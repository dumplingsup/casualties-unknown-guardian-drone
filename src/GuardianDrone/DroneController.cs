using System.Collections.Generic;
using UnityEngine;

namespace GuardianDrone
{
    public sealed class DroneController : MonoBehaviour
    {
        public const float HealReach = 2.35f;
        public const float OwnerClearance = 3.1f;
        public Body Owner { get; private set; }
        public string Mode { get; private set; } = "patrol";
        public int Shots { get; private set; }
        public int Recoveries { get; private set; }
        public int FriendlyHits { get; private set; }
        public int EnemyHits { get; private set; }
        public float HealingSeconds { get; private set; }
        public float RepairAmount { get; private set; }
        public string TargetId => target ? target.id : null;
        public bool Healing => visual && visual.Healing;
        public bool Piloting { get; private set; }
        public bool LaneBlocked { get; private set; }
        public bool StowRequested { get; private set; }
        public bool Stowed { get; private set; }
        public bool HasTreatableWound => WorstWound(out _) != null;
        public float BodyBleed => TotalBleed();
        public SupportPriority Priority => Owner ? DroneSave.For(Owner).Priority : SupportPriority.Auto;
        public bool ThreatOnScreen { get; private set; }
        public bool CombatHold { get; private set; }
        public bool TreatmentHeld => CombatHold && HasTreatableWound;
        DroneVisual visual;
        BuildingEntity target;
        Vector2 goal, fireGoal;
        bool haveGoal, healLatch, healSoundPlayed;
        float scanAt, shootAt, routeAt, stuckTime, phase, calmUntil, blockedTellAt, offscreenTellAt, fireHoldUntil, healLogAt, redeployAt, threatAt, lastThreatAt, lastTargetAt;
        Vector2 velocity, previousPosition, previousGoal;
        List<Vector2> route = new List<Vector2>();
        static readonly Collider2D[] Nearby = new Collider2D[512];
        static readonly RaycastHit2D[] RayHits = new RaycastHit2D[128];
        static readonly int HitMask = LayerMask.GetMask("Ground", "Body", "Limb", "Item", "Descriptor");
        readonly HashSet<int> seen = new HashSet<int>();
        readonly List<Vector2> spots = new List<Vector2>();

        public void Initialize(Body owner)
        {
            Owner = owner;
            visual = gameObject.AddComponent<DroneVisual>(); visual.Build(owner);
            previousPosition = transform.position;
            DroneAudio.Deploy(transform.position);
        }
        public void Suspend() { if (visual) visual.Hide(); }
        public void Step(float dt)
        {
            if (!Owner || !Owner.alive || !Plugin.Ready || PauseHandler.paused || Time.timeScale <= 0 || SettingsMenu.instance)
            { Suspend(); return; }
            visual.Show();
            var anchor = Plugin.Anchor(Owner, out bool pilot); Piloting = pilot;
            if (Stowed)
            {
                visual.Hide();
                target = null;
                velocity = Vector2.zero;
                Mode = "stowed";
                LaneBlocked = false;
                CombatHold = false;
                ThreatOnScreen = false;
                if (Time.time >= redeployAt)
                {
                    redeployAt = Time.time + 0.5f;
                    if (TryPatrol(anchor, out var spot, out _, false) && DroneNavigation.Free(spot))
                    {
                        transform.position = spot;
                        previousPosition = spot;
                        Stowed = false;
                        StowRequested = false;
                        stuckTime = 0;
                        visual.Show();
                        Plugin.Tell("Open space found.");
                    }
                }
                return;
            }
            phase += dt;
            var wound = WorstWound(out float severity);
            float bleed = TotalBleed();
            bool guarding = DroneSave.For(Owner).Guard;
            var priority = DroneSave.For(Owner).Priority;
            if (Time.time >= scanAt)
            {
                scanAt = Time.time + 0.3f;
                target = guarding && !pilot ? SelectTarget(anchor) : null;
            }
            bool engageNow = target != null;
            if (engageNow) lastTargetAt = Time.time;
            bool engageHold = engageNow || Time.time - lastTargetAt < 0.75f;

            if (Time.time >= threatAt)
            {
                threatAt = Time.time + (priority == SupportPriority.Defense || healLatch ? 0.15f : 0.3f);
                bool threatFound = guarding && !pilot && FindScreenThreat(anchor);
                ThreatOnScreen = threatFound;
                if (threatFound) lastThreatAt = Time.time;
            }
            bool screenThreat = ThreatOnScreen || Time.time - lastThreatAt < 1f;

            bool forced = priority == SupportPriority.Medical;
            float critical = Plugin.CriticalBleed == null ? 10f : Plugin.CriticalBleed.Value;
            bool criticalBleed = bleed >= critical;
            CombatHold = guarding && !pilot && !forced && Owner.conscious &&
                (priority == SupportPriority.Defense
                    ? screenThreat
                    : priority == SupportPriority.Auto && engageHold && !criticalBleed);

            if (CombatHold) healLatch = false;
            else
            {
                if ((wound && bleed >= 1f) || !Owner.conscious || forced) healLatch = true;
                if (healLatch && Owner.conscious && !forced && bleed < 0.15f) healLatch = false;
            }
            if (target) calmUntil = Time.time + 1.5f;
            bool light = !healLatch && !pilot && !CombatHold && wound && severity >= 18f && Time.time >= calmUntil;
            bool treat = wound && !pilot && !CombatHold && (healLatch || light);
            Vector2 aim = treat ? (Vector2)wound.transform.position : anchor;
            bool onScreen = true;
            bool holdingFire = !treat && target && Time.time < fireHoldUntil && DroneNavigation.Free(fireGoal);
            bool found = holdingFire || (treat ? TryHealSpot(aim, out goal) : TryPatrol(anchor, out goal, out onScreen));
            if (holdingFire) { goal = fireGoal; onScreen = OnScreen(Camera.main, goal); }
            haveGoal = found;
            if (!treat && found && !onScreen) Mode = "offscreen";
            if (haveGoal) Move(goal, dt);
            else { velocity = Vector2.zero; stuckTime += dt; Mode = "no-cell"; }
            if (stuckTime > 2.5f)
            {
                Stowed = true;
                StowRequested = true;
                stuckTime = 0;
                Recoveries++;
                target = null;
                visual.Hide();
                Mode = "stowed";
                redeployAt = Time.time + 0.5f;
                Plugin.Tell("Waiting for open space.");
                return;
            }
            if (!treat && !onScreen && Time.time >= offscreenTellAt && haveGoal)
            { offscreenTellAt = Time.time + 8f; Plugin.Tell("Guardian is outside the camera."); }
            Vector2 look = treat ? aim : target ? (Vector2)target.transform.position : aim;
            visual.Aim(look);
            bool healed = false;
            if (treat && Vector2.Distance(visual.Muzzle, aim) <= HealReach && Visible(visual.Muzzle, aim, null, Owner))
            {
                float amount = Repair(wound, dt);
                if (amount > 0)
                {
                    HealingSeconds += dt; RepairAmount += amount; healed = true; Mode = "healing";
                    if (!healSoundPlayed) { DroneAudio.Heal(visual.Muzzle); healSoundPlayed = true; }
                    if (Time.time >= healLogAt)
                    {
                        healLogAt = Time.time + 0.5f;
                        Plugin.Log("HEAL_TICK amount=" + amount.ToString("0.00") + " bleed=" + TotalBleed().ToString("0.00") + " dist=" + Vector2.Distance(visual.Muzzle, aim).ToString("0.00"));
                    }
                }
                else Mode = "approach-medical";
            }
            else if (treat)
            {
                Mode = "approach-medical";
                healSoundPlayed = false;
                if (Time.time >= healLogAt)
                {
                    healLogAt = Time.time + 0.5f;
                    Plugin.Log("HEAL_APPROACH dist=" + Vector2.Distance(visual.Muzzle, aim).ToString("0.00"));
                }
            }
            else healSoundPlayed = false;
            visual.SetHealing(healed, aim);
            LaneBlocked = false;
            if (!treat && guarding && target && Time.time >= shootAt)
                ConsiderFire(anchor, look);
            if (!treat && Time.time < fireHoldUntil) Mode = "reposition";
            else if (!treat && haveGoal && Mode != "blocked" && Mode != "offscreen")
                Mode = goal.y < anchor.y + 2.6f ? "escort" : "patrol";
            if (CombatHold && !target) Mode = "defending";
            visual.Animate(velocity, Mode, look, phase);
        }
        public void RecordImpact(bool friendly) { if (friendly) FriendlyHits++; else EnemyHits++; }
        void ConsiderFire(Vector2 anchor, Vector2 look)
        {
            if (!IsEnemy(target) || !Engaging(target, Owner) || Vector2.Distance(anchor, target.transform.position) > 18f)
            { target = null; return; }
            if (DroneProjectile.CorridorClear(visual.Muzzle, look, Owner, target))
            {
                LaneBlocked = false;
                fireHoldUntil = 0;
                DroneProjectile.Fire(this, visual.Muzzle, look, Plugin.Damage.Value);
                visual.Shot(look); Shots++; shootAt = Time.time + Plugin.ShotInterval.Value;
                return;
            }
            LaneBlocked = true;
            if (TryFiringSpot(anchor, look, out var spot))
            { fireGoal = spot; fireHoldUntil = Time.time + 1.4f; goal = spot; haveGoal = true; Mode = "reposition"; }
            else
            {
                Mode = "blocked";
                if (Time.time >= blockedTellAt)
                { blockedTellAt = Time.time + 4f; Plugin.Tell("Shot blocked."); }
            }
        }
        void Move(Vector2 nextGoal, float dt)
        {
            Vector2 pos = transform.position;
            float distance = Vector2.Distance(pos, nextGoal);
            if (Time.time >= routeAt || Vector2.Distance(nextGoal, previousGoal) > 1.5f)
            { routeAt = Time.time + 0.65f; previousGoal = nextGoal; route = DroneNavigation.Route(pos, nextGoal); }
            Vector2 next = pos;
            if (DroneNavigation.Clear(pos, nextGoal)) next = nextGoal;
            else
            {
                while (route.Count > 0 && Vector2.Distance(pos, route[0]) < 0.35f) route.RemoveAt(0);
                if (route.Count > 0) next = route[0];
                else { stuckTime += dt; velocity = Vector2.zero; previousPosition = pos; return; }
            }
            Vector2 desired = Vector2.ClampMagnitude((next - pos) * 5f, Mathf.Min(18f, 6f + distance));
            velocity = Vector2.MoveTowards(velocity, desired, 50f * dt);
            var step = pos + velocity * dt;
            if (DroneNavigation.Clear(pos, step)) transform.position = step;
            else velocity = Vector2.zero;
            if (distance > 1.2f && Vector2.Distance(previousPosition, transform.position) < dt * 0.25f) stuckTime += dt;
            else stuckTime = 0;
            previousPosition = transform.position;
        }
        bool TryPatrol(Vector2 anchor, out Vector2 result, out bool onScreen, bool requirePath = true)
        {
            spots.Clear();
            float cruise = Plugin.PatrolRadius.Value;
            for (int i = 0; i < 7; i++)
            {
                float angle = (28f + i * 21f) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                spots.Add(anchor + dir * cruise);
                spots.Add(anchor + dir * (cruise * 0.72f));
            }
            spots.Add(anchor + new Vector2(-5.2f, 1.5f));
            spots.Add(anchor + new Vector2(5.2f, 1.5f));
            spots.Add(anchor + new Vector2(-4.2f, 2.4f));
            spots.Add(anchor + new Vector2(4.2f, 2.4f));
            return Pick(anchor, spots, OwnerClearance, false, requirePath, out result, out onScreen);
        }
        bool TryHealSpot(Vector2 wound, out Vector2 result)
        {
            spots.Clear();
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = ring == 0 ? 1.55f : 2.05f;
                for (int i = 0; i < 10; i++)
                {
                    float angle = i * Mathf.PI / 5f;
                    var spot = wound + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (Visible(spot, wound, null, Owner)) spots.Add(spot);
                }
            }
            return Pick((Vector2)Owner.transform.position, spots, 1.25f, true, true, out result, out _);
        }
        bool TryFiringSpot(Vector2 anchor, Vector2 targetPoint, out Vector2 result)
        {
            spots.Clear();
            var toTarget = targetPoint - anchor;
            if (toTarget.sqrMagnitude < 0.01f) toTarget = Vector2.right;
            toTarget.Normalize();
            var flank = new Vector2(-toTarget.y, toTarget.x);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                spots.Add(targetPoint - dir * 6.5f);
                spots.Add(anchor + flank * (4.5f + (i % 2)) + Vector2.up * 2.2f);
            }
            spots.Add(targetPoint + flank * 5f);
            spots.Add(targetPoint - flank * 5f);
            spots.Add(targetPoint + Vector2.up * 5f);
            float best = float.MaxValue; result = anchor; bool found = false;
            foreach (var spot in spots)
            {
                if (Vector2.Distance(spot, anchor) < OwnerClearance || !DroneNavigation.Free(spot)) continue;
                if (!DroneNavigation.Clear(transform.position, spot) && DroneNavigation.Route(transform.position, spot).Count == 0) continue;
                if (!DroneProjectile.CorridorClear(spot, targetPoint, Owner, target)) continue;
                float score = Vector2.Distance(transform.position, spot);
                if (score < best) { best = score; result = spot; found = true; }
            }
            return found;
        }
        bool Pick(Vector2 anchor, List<Vector2> options, float clearance, bool medical, bool requirePath, out Vector2 result, out bool onScreen)
        {
            float cruise = Plugin.PatrolRadius.Value;
            float best = float.MaxValue; result = anchor; onScreen = false; bool found = false;
            var camera = Camera.main;
            foreach (var spot in options)
            {
                if (Vector2.Distance(spot, anchor) < clearance) continue;
                if (!DroneNavigation.Free(spot)) continue;
                if (requirePath && !DroneNavigation.Clear(transform.position, spot) && DroneNavigation.Route(transform.position, spot).Count == 0) continue;
                bool visible = OnScreen(camera, spot);
                float score = medical
                    ? Vector2.Distance(transform.position, spot)
                    : Vector2.Distance(transform.position, spot) * 0.08f + Mathf.Abs(Vector2.Distance(spot, anchor) - cruise) * 0.35f - Mathf.Max(0f, spot.y - anchor.y) * 0.2f;
                if (!visible) score += medical ? 6f : 40f;
                if (score < best) { best = score; result = spot; onScreen = visible; found = true; }
            }
            return found;
        }
        static bool OnScreen(Camera camera, Vector2 point)
        {
            if (!camera || !camera.orthographic) return true;
            var delta = point - (Vector2)camera.transform.position;
            float halfH = Mathf.Max(1f, camera.orthographicSize - 1.3f);
            float halfW = Mathf.Max(1f, halfH * camera.aspect - 0.4f);
            return Mathf.Abs(delta.x) <= halfW && Mathf.Abs(delta.y) <= halfH;
        }
        static bool ScreenVisible(Camera camera, Vector2 point, float margin)
        {
            if (!camera || !camera.orthographic) return true;
            var delta = point - (Vector2)camera.transform.position;
            float halfH = Mathf.Max(1f, camera.orthographicSize + margin);
            float halfW = Mathf.Max(1f, halfH * camera.aspect + margin);
            return Mathf.Abs(delta.x) <= halfW && Mathf.Abs(delta.y) <= halfH;
        }
        float TotalBleed()
        {
            float sum = 0;
            if (!Owner) return 0;
            foreach (var limb in Owner.limbs)
                if (limb && !limb.dismembered && !limb.prostheticInfo.hasProsthetic) sum += Mathf.Max(0, limb.bleedAmount);
            return sum;
        }
        Limb WorstWound(out float severity)
        {
            severity = 0; Limb result = null;
            if (!Owner || !Owner.alive) return null;
            foreach (var limb in Owner.limbs)
            {
                if (!limb || limb.dismembered || limb.prostheticInfo.hasProsthetic) continue;
                float score = Mathf.Max(0, limb.bleedAmount) * 100;
                if (!limb.hasShrapnel) score += Mathf.Max(0, 100 - limb.skinHealth) + Mathf.Max(0, 100 - limb.muscleHealth);
                if (score > severity + 0.05f) { result = limb; severity = score; }
            }
            return severity >= 18f || (result && result.bleedAmount >= 0.2f) ? result : null;
        }
        static float Repair(Limb limb, float dt)
        {
            float before = limb.bleedAmount, skin = limb.skinHealth, muscle = limb.muscleHealth;
            limb.bleedAmount = Mathf.MoveTowards(Mathf.Max(0, before), 0, Plugin.BleedRepair.Value * dt);
            if (limb.bleedAmount < 0.1f && !limb.hasShrapnel)
            {
                limb.skinHealth = Mathf.MoveTowards(limb.skinHealth, 100, Plugin.TissueRepair.Value * dt);
                limb.muscleHealth = Mathf.MoveTowards(limb.muscleHealth, 100, Plugin.TissueRepair.Value * dt);
            }
            return Mathf.Max(0, before - limb.bleedAmount) + Mathf.Max(0, limb.skinHealth - skin) + Mathf.Max(0, limb.muscleHealth - muscle);
        }
        public static bool IsEnemy(BuildingEntity b)
        {
            // Scripted hostiles (guards, turrets, crystal enemies) handle their own
            // death. Their BuildingEntity is marked indestructible only to bypass
            // the generic destruction path, so that flag must not make them immune
            // to target selection.
            if (!b || b.cantHit || b.health <= 0 || !Listed(b)) return false;
            return true;
        }
        static bool Listed(BuildingEntity b)
        {
            var trader = b.GetComponent<TraderScript>();
            if (trader && trader.hostile) return true;
            return b.GetComponent<SpiderHandler>() || b.GetComponent<CaveTicks>() ||
                b.GetComponent<LaserDroneScript>() || b.GetComponent<GuardScript>() || b.GetComponent<TurretScript>() ||
                b.GetComponent<CeilingTurret>() || b.GetComponent<CrystalEnemy>();
        }
        public static bool Engaging(BuildingEntity b, Body owner)
        {
            if (!owner || !IsEnemy(b)) return false;
            float distance = Vector2.Distance(owner.transform.position, b.transform.position);
            if (b.GetComponent<SpiderHandler>() || b.GetComponent<CaveTicks>()) return distance <= 10f;
            if (b.GetComponent<CrystalEnemy>()) return distance <= 16f;
            if (b.GetComponent<LaserDroneScript>()) return distance <= 14f;
            if (b.GetComponent<GuardScript>()) return distance <= 9f;
            var trader = b.GetComponent<TraderScript>();
            if (trader && trader.hostile) return distance <= 12f && CanSeeThreat(b.transform.position, owner.transform.position, null);
            if (b.GetComponent<TurretScript>() || b.GetComponent<CeilingTurret>())
                return distance <= 18f && CanSeeThreat(b.transform.position, owner.transform.position, null);
            return false;
        }
        BuildingEntity SelectTarget(Vector2 anchor)
        {
            seen.Clear(); BuildingEntity best = null; float bestDistance = 18f;
            int count = Physics2D.OverlapCircleNonAlloc(anchor, 18f, Nearby, LayerMask.GetMask("Ground", "Body", "Limb", "Descriptor"));
            for (int i = 0; i < count; i++)
            {
                var b = Nearby[i] ? Nearby[i].GetComponentInParent<BuildingEntity>() : null;
                if (!b || !seen.Add(b.GetInstanceID()) || !Engaging(b, Owner)) continue;
                float dist = Vector2.Distance(anchor, b.transform.position);
                if (dist < bestDistance && (CanSeeThreat(transform.position, b.transform.position, b) ||
                    CanSeeThreat(anchor, b.transform.position, b)))
                { best = b; bestDistance = dist; }
            }
            return best;
        }
        bool FindScreenThreat(Vector2 anchor)
        {
            var camera = Camera.main;
            float halfH = camera && camera.orthographic ? camera.orthographicSize + 1.5f : 10f;
            float halfW = camera && camera.orthographic ? halfH * camera.aspect + 1.5f : 18f;
            float radius = Mathf.Min(30f, Mathf.Sqrt(halfH * halfH + halfW * halfW) + 1.5f);
            seen.Clear(); BuildingEntity found = null; float best = float.MaxValue;
            int count = Physics2D.OverlapCircleNonAlloc(anchor, radius, Nearby, LayerMask.GetMask("Ground", "Body", "Limb", "Descriptor"));
            for (int i = 0; i < count; i++)
            {
                var b = Nearby[i] ? Nearby[i].GetComponentInParent<BuildingEntity>() : null;
                if (!b || !seen.Add(b.GetInstanceID()) || !IsEnemy(b)) continue;
                if (!ScreenVisible(camera, b.transform.position, 1.5f)) continue;
                if (!CanSeeThreat(Owner.transform.position, b.transform.position, b) &&
                    !CanSeeThreat(transform.position, b.transform.position, b)) continue;
                float dist = Vector2.Distance(anchor, b.transform.position);
                if (dist < best) { best = dist; found = b; }
            }
            return found != null;
        }
        public static bool CanSeeThreat(Vector2 from, Vector2 to, BuildingEntity target)
        {
            var d = to - from; if (d.sqrMagnitude < 0.001f) return true;
            int count = Physics2D.RaycastNonAlloc(from, d.normalized, RayHits, d.magnitude, LayerMask.GetMask("Ground"));
            if (count == RayHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var c = RayHits[i].collider; if (!c || c.isTrigger) continue;
                var entity = c.GetComponentInParent<BuildingEntity>();
                if (entity && (entity == target || entity.animal)) continue;
                return false;
            }
            return true;
        }
        public static bool Visible(Vector2 from, Vector2 to, BuildingEntity allowedBuilding, Body allowedBody)
        {
            var d = to - from; if (d.sqrMagnitude < 0.001f) return true;
            int count = Physics2D.RaycastNonAlloc(from, d.normalized, RayHits, d.magnitude + 0.05f, HitMask);
            if (count == RayHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var c = RayHits[i].collider; if (!c || c.isTrigger) continue;
                var building = c.GetComponentInParent<BuildingEntity>();
                var limb = c.GetComponentInParent<Limb>();
                var body = limb ? limb.body : c.GetComponentInParent<Body>();
                if (allowedBuilding && building == allowedBuilding) continue;
                if (allowedBody && body == allowedBody) continue;
                return false;
            }
            return true;
        }
    }
}
