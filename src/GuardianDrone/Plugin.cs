using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using CUCoreLib.Data;
using CUCoreLib.Registries;
using CUCoreLib.Saving;
using HarmonyLib;
using UnityEngine;

namespace GuardianDrone
{
    [BepInPlugin(Guid, "Guardian Drone", "0.3.0")]
    [BepInDependency("net.cucorelib", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "local.cu.guardiandrone";
        public const string ControllerId = "guardian_drone_controller";
        // A layer load or save restore can leave the old drone flying through a
        // half-built world. Keep the drone out until the new layer has been
        // ready for a moment, then redeploy if deployment was still requested.
        const float LoadSettleSeconds = 2.5f;
        const float InitialGrantDelay = 1.5f;
        const float ReplacementDelay = 3f;
        const float PostRestoreReplacementDelay = 12f;
        public static Plugin Instance;
        public static DroneController Drone;
        public static ConfigEntry<KeyCode> DeployKey, GuardKey, HealKey;
        public static ConfigEntry<float> Damage, ShotInterval, BleedRepair, TissueRepair, PatrolRadius, Volume, CriticalBleed;
        Harmony harmony;
        Body lastBody;
        WorldGeneration lastWorld;
        float controllerCheckAt, missingSince, toldDeployAt, hudErrorAt, transitionSettleUntil, nextGrantAt;
        bool hasController, toldLeft, userToggled, transition;
        int lastDepth = int.MinValue, lastRestoreSerial;
        Item grantedController;
        int inventoryControllers, worldControllers;
        GUIStyle hudStyle;
        static FieldInfo mechField;
        static PropertyInfo pilotProperty;

        void Awake()
        {
            Instance = this;
            DeployKey = Config.Bind("Controls", "DeployRecall", KeyCode.F6, "Deploy / recall; requires the free controller in your inventory.");
            GuardKey = Config.Bind("Controls", "GuardCeasefire", KeyCode.F7, "Toggle automatic attacks; automatic healing remains enabled.");
            HealKey = Config.Bind("Controls", "HealNow", KeyCode.F8, "Cycle support priority: auto / defense / medical.");
            Damage = Config.Bind("Support", "ShotDamage", 25f, new ConfigDescription("Pulse damage. No energy or ammo cost.", new AcceptableValueRange<float>(1f, 200f)));
            ShotInterval = Config.Bind("Support", "ShotInterval", 0.45f, new ConfigDescription("Seconds between pulses.", new AcceptableValueRange<float>(0.15f, 5f)));
            BleedRepair = Config.Bind("Support", "BleedingRepairPerSecond", 5f, new ConfigDescription("External bleeding reduction per second on one limb.", new AcceptableValueRange<float>(0.1f, 50f)));
            TissueRepair = Config.Bind("Support", "TissueRepairPerSecond", 5f, new ConfigDescription("Skin and muscle recovery per second on one limb.", new AcceptableValueRange<float>(0.1f, 50f)));
            PatrolRadius = Config.Bind("Support", "PatrolRadius", 8f, new ConfigDescription("Cruising distance from the owner. Moves closer to treat wounds.", new AcceptableValueRange<float>(4f, 14f)));
            Volume = Config.Bind("Support", "Volume", 0.35f, new ConfigDescription("Rotor, shot, heal and deploy volume. Zero mutes the drone.", new AcceptableValueRange<float>(0f, 1f)));
            CriticalBleed = Config.Bind("Support", "CriticalBleed", 10f, new ConfigDescription("In auto priority, total bleeding at or above this overrides an active fight so the drone can treat you.", new AcceptableValueRange<float>(1f, 40f)));
            ItemRegistry.Register(ControllerId, new CustomItemInfo
            {
                fullName = "Guardian drone controller",
                description = "Free, indestructible patrol drone. Use or F6: deploy/recall. F7: guard/ceasefire. F8: cycle auto / defense / medical priority. Auto fights an active threat and treats wounds when clear; defense keeps fighting while an enemy is on screen and pauses treatment; medical prioritizes treatment. Holds fire when you or a bystander is in the shot lane, then moves to a clear flank. Rounds already in flight can still hit you. Patrols about 6-9 tiles away in the open and closer beside you in a low cave. Waits instead of entering a wall when no open cell exists. Repairs external bleeding, skin and muscle. No battery, ammo or medicine. Does not restore blood, organs, bones or missing limbs. Single-player.",
                category = "tool", weight = 0.05f, value = 0, usable = true,
                usableWithLMB = false, destroyAtZeroCondition = false, decayMinutes = 0,
                SpawnFrequency = 0, rec = new Recognition(0),
                useAction = (body, item) => UseController(body)
            }, DroneArt.Get("controller"));
            SaveRegistry.RegisterBodyProvider(DroneSave.Key, new DroneSave());
            harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Log("Guardian 0.3.0 registered; priority cycle, screen threat hold, controller dedupe, lane check, single player.");
        }

        public static bool Ready => PlayerCamera.main && PlayerCamera.main.body && WorldGeneration.world &&
            WorldGeneration.world.worldExists && !WorldGeneration.world.generatingWorld && !WorldGeneration.world.cutsceneActive;
        public static bool Transitioning => Instance && Instance.transition;
        public static bool InputReady()
        {
            if (!Ready || Transitioning || PauseHandler.paused || Time.timeScale <= 0 || SettingsMenu.instance || !Application.isFocused) return false;
            if (!PlayerCamera.main.body.alive || !PlayerCamera.main.body.conscious) return false;
            if (ConsoleScript.instance && ConsoleScript.instance.active) return false;
            var p = PlayerCamera.main;
            return !p.craftingPanel.activeSelf && !p.tradeMenu.activeSelf && !p.woundView.activeSelf &&
                !p.itemCustomizer.activeSelf && !p.radialOpen && !p.dragItem &&
                !(MinigameBase.main && MinigameBase.main.currentMinigame) && !LiquidTransfer.instance;
        }

        public void Tick()
        {
            if (!Ready)
            {
                EnterTransition();
                if (Drone) Drone.Suspend();
                return;
            }
            var owner = PlayerCamera.main.body;
            var world = WorldGeneration.world;
            float now = Time.unscaledTime;
            if (DroneSave.RestoreSerial != lastRestoreSerial)
            {
                lastRestoreSerial = DroneSave.RestoreSerial;
                EnterTransition();
                return;
            }
            if (owner != lastBody || world != lastWorld)
            {
                ClearDrone(); lastBody = owner; lastWorld = world; lastDepth = world.biomeDepth;
                controllerCheckAt = 0; toldLeft = false; userToggled = false; missingSince = -1;
                grantedController = null; nextGrantAt = 0;
                EnterTransition();
                return;
            }
            if (world.biomeDepth != lastDepth)
            {
                lastDepth = world.biomeDepth;
                EnterTransition();
                return;
            }
            // The descent panel is the last place the old drone can still fly.
            // Recall it as soon as the panel appears, not only when the world
            // starts rebuilding under it.
            if (world.savePanel && world.savePanel.activeInHierarchy)
            {
                EnterTransition();
                return;
            }
            if (transition)
            {
                if (now < transitionSettleUntil) return;
                transition = false;
                controllerCheckAt = 0;
                missingSince = -1;
                hasController = false;
                Log("LEVEL_READY depth=" + world.biomeDepth);
            }
            if (now >= controllerCheckAt)
            {
                CountControllers(owner);
                hasController = inventoryControllers > 0;
                controllerCheckAt = now + 0.4f;
                var state = DroneSave.For(owner);
                bool owned = inventoryControllers + worldControllers > 0;
                if (owned) { state.HadController = true; missingSince = -1; }
                if (hasController) toldLeft = false;
                else if (worldControllers > 0 && !toldLeft)
                {
                    toldLeft = true;
                    Tell("The controller left your inventory. Guardian recalled.");
                }
                if (owned)
                {
                    RemoveDuplicateGrant();
                }
                else MaintainController(owner, state, now);
            }
            var stateNow = DroneSave.For(owner);
            if (!owner.alive || !hasController || stateNow.Unsupported || !stateNow.Deployed) ClearDrone();
            else if (!Drone)
            {
                var go = new GameObject("Guardian Drone");
                go.transform.position = Anchor(owner, out _) + Vector2.up * 2;
                Drone = go.AddComponent<DroneController>();
                Drone.Initialize(owner);
                Log("DEPLOY body=" + owner.GetInstanceID());
                if (!userToggled && Time.unscaledTime - toldDeployAt > 0.5f)
                    Tell("Guardian deployed.");
                userToggled = false;
            }
            if (InputReady())
            {
                if (Input.GetKeyDown(DeployKey.Value)) ToggleDeployment();
                if (Input.GetKeyDown(GuardKey.Value)) ToggleGuard();
                if (Input.GetKeyDown(HealKey.Value)) TogglePriority();
            }
            if (Drone) Drone.Step(Mathf.Min(Time.deltaTime, 0.1f));
            DroneProjectile.StepAll(Mathf.Min(Time.deltaTime, 0.1f));
        }

        void EnterTransition()
        {
            if (!transition) Log("TRANSITION begin depth=" + (WorldGeneration.world ? WorldGeneration.world.biomeDepth : -1));
            transition = true;
            transitionSettleUntil = Time.unscaledTime + LoadSettleSeconds;
            if (Drone || DroneProjectile.Count > 0)
            {
                ClearDrone();
                Log("TRANSITION recall");
            }
            hasController = false;
            missingSince = -1;
        }

        public static bool HasController(Body owner) => owner && owner.GetComponentsInChildren<Item>(true).Any(x => x.id == ControllerId);

        // Counts every live controller once: hotbar and worn items, contents of
        // container items, dropped items, and items in storage containers. Items
        // inside an inactive container are not in Item.allItems until Start runs
        // (common while a save mod is restoring the layer), so a scene sweep is
        // the fallback. The custom-item template lives in DontDestroyOnLoad and
        // is never a player-owned controller.
        readonly HashSet<int> countedControllers = new HashSet<int>();
        void CountControllers(Body owner)
        {
            inventoryControllers = 0;
            worldControllers = 0;
            countedControllers.Clear();
            var own = owner.GetComponentsInChildren<Item>(true);
            for (int i = 0; i < own.Length; i++)
            {
                var item = own[i];
                if (!item || item.id != ControllerId || !countedControllers.Add(item.GetInstanceID())) continue;
                inventoryControllers++;
            }
            var live = Item.allItems;
            for (int i = 0; i < live.Count; i++)
            {
                var item = live[i];
                if (!item || item.id != ControllerId || !countedControllers.Add(item.GetInstanceID())) continue;
                if (item.transform.IsChildOf(owner.transform)) inventoryControllers++;
                else worldControllers++;
            }
            if (inventoryControllers + worldControllers > 0) return;
            var scene = UnityEngine.Object.FindObjectsOfType<Item>(true);
            for (int i = 0; i < scene.Length; i++)
            {
                var item = scene[i];
                if (!item || item.id != ControllerId || !countedControllers.Add(item.GetInstanceID())) continue;
                if (!item.gameObject.scene.IsValid() || item.gameObject.scene.name == "DontDestroyOnLoad") continue;
                // QoL Unknown's in-depth save clones the custom item template
                // without activating it, so a restored controller sits inert and
                // cannot be picked up. Wake it instead of issuing a second one.
                if (!item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(true);
                    Log("CONTROLLER_ACTIVATED");
                }
                if (item.transform.IsChildOf(owner.transform)) inventoryControllers++;
                else worldControllers++;
            }
        }

        void MaintainController(Body owner, DroneState state, float now)
        {
            if (now < nextGrantAt) return;
            // After a save restore, let the save mod finish restoring the layer
            // (including storage containers) before handing out anything.
            if (DroneSave.RestoredWithin(now, PostRestoreReplacementDelay)) return;
            if (missingSince < 0) missingSince = now;
            float delay = state.HadController ? ReplacementDelay : InitialGrantDelay;
            if (now - missingSince < delay) return;
            // Last look immediately before handing out an item. This closes the
            // window where a storage container or a save restore finishes a few
            // milliseconds after the periodic scan.
            CountControllers(owner);
            hasController = inventoryControllers > 0;
            if (inventoryControllers + worldControllers > 0)
            {
                state.HadController = true;
                missingSince = -1;
                return;
            }
            bool replacement = state.HadController;
            var item = GrantController(owner, replacement ? "CONTROLLER_REPLACED" : "CONTROLLER_GRANTED");
            nextGrantAt = now + 20f;
            missingSince = -1;
            if (!item) return;
            grantedController = item;
            state.HadController = true;
            if (replacement) Tell("A free replacement controller was issued.");
        }

        // A save mod can restore the original controller after we already issued
        // a replacement. Keep only the item the player owned before, and remove
        // the one this session created. Existing duplicates are left alone.
        void RemoveDuplicateGrant()
        {
            if (!grantedController) { grantedController = null; return; }
            if (inventoryControllers + worldControllers <= 1) return;
            Log("CONTROLLER_DEDUPE");
            Destroy(grantedController.gameObject);
            grantedController = null;
            Tell("Duplicate controller removed.");
        }

        static Item GrantController(Body owner, string reason)
        {
            var obj = Utils.Create(ControllerId, owner.transform.position, 0);
            if (!obj || !obj.TryGetComponent<Item>(out var item)) return null;
            var slot = owner.FirstEmptySlot();
            if (slot.HasValue) owner.PickUpItem(item, slot.Value, true);
            else Tell("Inventory full. Your drone controller is at your feet.");
            Log(reason + " body=" + owner.GetInstanceID());
            return item;
        }
        void OnGUI()
        {
            try { DrawHud(); }
            catch (Exception e)
            {
                if (Time.unscaledTime < hudErrorAt) return;
                hudErrorAt = Time.unscaledTime + 5f;
                Log("HUD " + e.GetType().Name + ": " + e.Message);
            }
        }
        void DrawHud()
        {
            if (!Ready) return;
            var owner = PlayerCamera.main.body;
            var state = DroneSave.For(owner);
            if (state == null || state.Unsupported) return;
            if (hudStyle == null)
            {
                hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = false };
                hudStyle.normal.textColor = Color.white;
            }
            string posture = (state.Guard ? Tr("Escort fire") : Tr("Ceasefire")) + " · " + Tr(PriorityShort(state.Priority));
            string activity = Transitioning ? Tr("Level loading")
                : !hasController ? Tr("Controller missing")
                : !state.Deployed ? Tr("Recalled")
                : Drone && Drone.Stowed ? Tr("Waiting for space")
                : Drone && Drone.TreatmentHeld ? Tr("Holding treatment")
                : Drone ? Tr(ActivityEnglish(Drone.Mode))
                : Tr("Waiting");
            var icon = DroneArt.Get("controller");
            var box = new Rect(16, 16, 280, 52);
            var previous = GUI.color;
            GUI.color = new Color(0.05f, 0.07f, 0.08f, 0.78f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = state.Guard ? new Color(0.55f, 0.95f, 0.85f) : new Color(1f, 0.72f, 0.35f);
            if (icon && icon.texture) GUI.DrawTexture(new Rect(22, 22, 36, 36), icon.texture, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(66, 18, 220, 22), posture, hudStyle);
            GUI.Label(new Rect(66, 40, 220, 22), activity, hudStyle);
            GUI.color = previous;
        }
        static string ActivityEnglish(string mode)
        {
            switch (mode)
            {
                case "escort": return "Side escort";
                case "offscreen": return "Off screen";
                case "reposition": return "Moving to a clear shot";
                case "blocked": return "Shot blocked";
                case "no-cell": return "No open cell";
                case "approach-medical": return "Approaching a wound";
                case "healing": return "Repairing";
                case "stowed": return "Waiting for space";
                case "defending": return "Defending";
                default: return "Patrolling";
            }
        }
        public static string Tr(string english)
        {
            var strings = Locale.currentLang?.other;
            if (strings != null && strings.TryGetValue("guardian." + english, out var value) && !string.IsNullOrEmpty(value)) return value;
            return english;
        }
        public static void ToggleDeployment()
        {
            if (!InputReady()) return;
            UseController(PlayerCamera.main.body);
        }
        public static void UseController(Body body)
        {
            // Explicit item use is also valid from its inventory/radial action menu.
            // Keyboard shortcuts retain the stricter UI and focus guards above.
            if (!Ready || PauseHandler.paused || Time.timeScale <= 0 || !body || !body.alive || body != PlayerCamera.main.body) return;
            if (Transitioning) { Tell("Guardian is locked until the level finishes loading."); return; }
            if (!HasController(body)) { Tell("Keep the drone controller in your inventory."); return; }
            var state = DroneSave.For(body);
            if (state.Unsupported) { Tell("This drone save is from a newer version."); return; }
            state.Deployed = !state.Deployed;
            if (!state.Deployed) ClearDrone();
            if (Instance)
            {
                Instance.userToggled = true;
                Instance.toldDeployAt = Time.unscaledTime;
            }
            Tell(state.Deployed ? "Guardian deployed." : "Guardian recalled.");
            Log("TOGGLE deployed=" + state.Deployed);
        }
        public static void ToggleGuard()
        {
            if (!InputReady()) return;
            var s = DroneSave.For(PlayerCamera.main.body); if (s.Unsupported) return;
            s.Guard = !s.Guard;
            Tell(s.Guard ? "Guardian: escort fire enabled." : "Guardian: ceasefire. Medical support remains active.");
        }
        public static void TogglePriority()
        {
            if (!InputReady()) return;
            var s = DroneSave.For(PlayerCamera.main.body);
            if (s.Unsupported) return;
            s.Priority = (SupportPriority)(((int)s.Priority + 1) % 3);
            Tell(PrioritySentence(s.Priority));
            Log("PRIORITY " + s.Priority);
        }
        public static void HealNow()
        {
            if (!InputReady()) return;
            if (!Drone) { Tell("Deploy your guardian first."); return; }
            var s = DroneSave.For(PlayerCamera.main.body);
            if (s.Unsupported) return;
            s.Priority = SupportPriority.Medical;
            Tell(PrioritySentence(SupportPriority.Medical));
            Log("PRIORITY Medical");
        }
        static string PriorityShort(SupportPriority priority)
        {
            switch (priority)
            {
                case SupportPriority.Defense: return "Defense";
                case SupportPriority.Medical: return "Medical";
                default: return "Auto";
            }
        }
        static string PrioritySentence(SupportPriority priority)
        {
            switch (priority)
            {
                case SupportPriority.Defense: return "Guardian priority: Defense.";
                case SupportPriority.Medical: return "Guardian priority: Medical.";
                default: return "Guardian priority: Auto.";
            }
        }
        public static Vector2 Anchor(Body body, out bool piloting)
        {
            piloting = false;
            if (mechField == null && Chainloader.PluginInfos.TryGetValue("local.cu.wayfarermech", out var info))
                mechField = info.Instance.GetType().GetField("Mech", BindingFlags.Public | BindingFlags.Static);
            var mech = mechField?.GetValue(null) as Component;
            if (mech)
            {
                if (pilotProperty == null) pilotProperty = mech.GetType().GetProperty("Pilot");
                if (pilotProperty?.GetValue(mech, null) as Body == body)
                { piloting = true; return (Vector2)mech.transform.position + Vector2.up * 3.5f; }
            }
            return body.transform.position;
        }
        public static void Tell(string english)
        {
            var strings = Locale.currentLang?.other;
            if (strings != null && strings.TryGetValue("guardian." + english, out var value)) english = value;
            if (PlayerCamera.main) PlayerCamera.main.DoAlert(english);
        }
        public static void Log(string s) => Instance?.Logger.LogInfo(s);
        public static void ClearDrone() { DroneProjectile.Clear(); if (Drone) Destroy(Drone.gameObject); Drone = null; }
        void OnDestroy() { ClearDrone(); harmony?.UnpatchSelf(); DroneArt.Release(); Instance = null; }
    }
    [HarmonyPatch(typeof(PlayerCamera), "Update")]
    static class PlayerTick
    {
        static void Postfix() { if (Plugin.Instance) Plugin.Instance.Tick(); }
    }
}
