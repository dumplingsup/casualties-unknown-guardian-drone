using System;
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
    [BepInPlugin(Guid, "Guardian Drone", "0.2.0")]
    [BepInDependency("net.cucorelib", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "local.cu.guardiandrone";
        public const string ControllerId = "guardian_drone_controller";
        public static Plugin Instance;
        public static DroneController Drone;
        public static ConfigEntry<KeyCode> DeployKey, GuardKey, HealKey;
        public static ConfigEntry<float> Damage, ShotInterval, BleedRepair, TissueRepair, PatrolRadius, Volume;
        Harmony harmony;
        Body lastBody;
        WorldGeneration lastWorld;
        float grantAt, controllerCheckAt, missingSince, toldDeployAt, hudErrorAt;
        bool hasController, initialGrantDone, replacementIssued, toldLeft, userToggled;
        int inventoryControllers, worldControllers;
        GUIStyle hudStyle;
        static FieldInfo mechField;
        static PropertyInfo pilotProperty;

        void Awake()
        {
            Instance = this;
            DeployKey = Config.Bind("Controls", "DeployRecall", KeyCode.F6, "Deploy / recall; requires the free controller in your inventory.");
            GuardKey = Config.Bind("Controls", "GuardCeasefire", KeyCode.F7, "Toggle automatic attacks; automatic healing remains enabled.");
            HealKey = Config.Bind("Controls", "HealNow", KeyCode.F8, "Prioritize supported external wounds.");
            Damage = Config.Bind("Support", "ShotDamage", 25f, new ConfigDescription("Pulse damage. No energy or ammo cost.", new AcceptableValueRange<float>(1f, 200f)));
            ShotInterval = Config.Bind("Support", "ShotInterval", 0.45f, new ConfigDescription("Seconds between pulses.", new AcceptableValueRange<float>(0.15f, 5f)));
            BleedRepair = Config.Bind("Support", "BleedingRepairPerSecond", 5f, new ConfigDescription("External bleeding reduction per second on one limb.", new AcceptableValueRange<float>(0.1f, 50f)));
            TissueRepair = Config.Bind("Support", "TissueRepairPerSecond", 5f, new ConfigDescription("Skin and muscle recovery per second on one limb.", new AcceptableValueRange<float>(0.1f, 50f)));
            PatrolRadius = Config.Bind("Support", "PatrolRadius", 8f, new ConfigDescription("Cruising distance from the owner. Moves closer to treat wounds.", new AcceptableValueRange<float>(4f, 14f)));
            Volume = Config.Bind("Support", "Volume", 0.35f, new ConfigDescription("Rotor, shot, heal and deploy volume. Zero mutes the drone.", new AcceptableValueRange<float>(0f, 1f)));
            ItemRegistry.Register(ControllerId, new CustomItemInfo
            {
                fullName = "Guardian drone controller",
                description = "Free, indestructible patrol drone. Use or F6: deploy/recall. F7: guard/ceasefire. F8: prioritize treatment. Holds fire when you or a bystander is in the shot lane, then moves to a clear flank. Rounds already in flight can still hit you. Patrols about 6-9 tiles away in the open and closer beside you in a low cave. Waits instead of entering a wall when no open cell exists. Repairs external bleeding, skin and muscle. No battery, ammo or medicine. Does not restore blood, organs, bones or missing limbs. Single-player.",
                category = "tool", weight = 0.05f, value = 0, usable = true,
                usableWithLMB = false, destroyAtZeroCondition = false, decayMinutes = 0,
                SpawnFrequency = 0, rec = new Recognition(0),
                useAction = (body, item) => UseController(body)
            }, DroneArt.Get("controller"));
            SaveRegistry.RegisterBodyProvider(DroneSave.Key, new DroneSave());
            harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Log("Guardian 0.2.0 registered; lane check, reachable repair, status icon, single player.");
        }

        public static bool Ready => PlayerCamera.main && PlayerCamera.main.body && WorldGeneration.world &&
            WorldGeneration.world.worldExists && !WorldGeneration.world.generatingWorld && !WorldGeneration.world.cutsceneActive;
        public static bool InputReady()
        {
            if (!Ready || PauseHandler.paused || Time.timeScale <= 0 || SettingsMenu.instance || !Application.isFocused) return false;
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
                if (Drone) Drone.Suspend();
                return;
            }
            var owner = PlayerCamera.main.body;
            if (owner != lastBody || WorldGeneration.world != lastWorld)
            {
                ClearDrone(); lastBody = owner; lastWorld = WorldGeneration.world;
                grantAt = Time.unscaledTime + 0.75f; controllerCheckAt = 0;
                initialGrantDone = false; replacementIssued = false; toldLeft = false; userToggled = false; missingSince = -1;
            }
            if (Time.unscaledTime >= controllerCheckAt)
            {
                CountControllers(owner);
                hasController = inventoryControllers > 0;
                controllerCheckAt = Time.unscaledTime + 0.4f;
                if (hasController) toldLeft = false;
                else if (worldControllers > 0 && !toldLeft)
                {
                    toldLeft = true;
                    Tell("The controller left your inventory. Guardian recalled.");
                }
            }
            if (!initialGrantDone && Time.unscaledTime >= grantAt && controllerCheckAt > 0)
            {
                if (inventoryControllers + worldControllers == 0)
                {
                    if (missingSince < 0) missingSince = Time.unscaledTime;
                    else if (Time.unscaledTime - missingSince >= 0.8f)
                    {
                        GrantController(owner, "CONTROLLER_GRANTED");
                        initialGrantDone = true;
                        missingSince = -1;
                    }
                }
                else { initialGrantDone = true; missingSince = -1; }
            }
            else if (initialGrantDone)
            {
                if (inventoryControllers + worldControllers == 0)
                {
                    if (missingSince < 0) missingSince = Time.unscaledTime;
                    else if (!replacementIssued && Time.unscaledTime - missingSince >= 3f)
                    {
                        replacementIssued = true;
                        GrantController(owner, "CONTROLLER_REPLACED");
                        Tell("A free replacement controller was issued.");
                        missingSince = -1;
                    }
                }
                else missingSince = -1;
            }
            var state = DroneSave.For(owner);
            if (!owner.alive || !hasController || state.Unsupported || !state.Deployed) ClearDrone();
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
                if (Input.GetKeyDown(HealKey.Value)) HealNow();
            }
            if (Drone) Drone.Step(Mathf.Min(Time.deltaTime, 0.1f));
            DroneProjectile.StepAll(Mathf.Min(Time.deltaTime, 0.1f));
        }

        public static bool HasController(Body owner) => owner && owner.GetComponentsInChildren<Item>(true).Any(x => x.id == ControllerId);
        void CountControllers(Body owner)
        {
            inventoryControllers = 0;
            worldControllers = 0;
            var items = UnityEngine.Object.FindObjectsOfType<Item>();
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (!item || item.id != ControllerId) continue;
                if (item.GetComponentInParent<Body>() == owner) inventoryControllers++;
                else worldControllers++;
            }
        }
        static void GrantController(Body owner, string reason)
        {
            var obj = Utils.Create(ControllerId, owner.transform.position, 0);
            if (!obj || !obj.TryGetComponent<Item>(out var item)) return;
            var slot = owner.FirstEmptySlot();
            if (slot.HasValue) owner.PickUpItem(item, slot.Value, true);
            else Tell("Inventory full. Your drone controller is at your feet.");
            Log(reason + " body=" + owner.GetInstanceID());
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
            string posture = state.Guard ? Tr("Escort fire") : Tr("Ceasefire");
            string activity = !hasController ? Tr("Controller missing")
                : !state.Deployed ? Tr("Recalled")
                : Drone && Drone.Stowed ? Tr("Waiting for space")
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
        public static void HealNow()
        {
            if (!InputReady()) return;
            if (!Drone) { Tell("Deploy your guardian first."); return; }
            if (!Drone.HasTreatableWound) { Tell("No treatable external wounds. Blood loss and internal conditions need other treatment."); return; }
            Drone.PrioritizeHealing();
            Tell("Guardian: medical support requested.");
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
