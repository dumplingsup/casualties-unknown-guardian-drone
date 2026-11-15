using System.Collections.Generic;
using UnityEngine;

namespace GuardianDrone
{
    public sealed class DroneController : MonoBehaviour
    {
        public Body Owner { get; private set; }
        public string Mode { get; private set; } = "follow";
        public int Shots { get; private set; }
        public int Recoveries { get; private set; }
        public int FriendlyHits { get; private set; }
        public int EnemyHits { get; private set; }
        public float HealingSeconds { get; private set; }
        public float RepairAmount { get; private set; }
        public string TargetId => target ? target.id : null;
        public bool Healing => visual && visual.Healing;
        public bool Piloting { get; private set; }
        public bool HasTreatableWound => WorstWound(out _) != null;
        DroneVisual visual;
        BuildingEntity target;
        float scanAt, shootAt, routeAt, stuckTime, priorityUntil, phase, recoveryUntil;
        Vector2 velocity, previousPosition, previousGoal;
        List<Vector2> route = new List<Vector2>();
        static readonly Collider2D[] Nearby = new Collider2D[256];
        static readonly RaycastHit2D[] RayHits = new RaycastHit2D[128];
        static readonly int HitMask = LayerMask.GetMask("Ground", "Body", "Limb", "Item", "Descriptor");
        readonly HashSet<int> seen = new HashSet<int>();

        public void Initialize(Body owner)
        {
            Owner = owner;
            visual = gameObject.AddComponent<DroneVisual>(); visual.Build(owner);
            previousPosition = transform.position;
        }
        public void PrioritizeHealing() { priorityUntil = Time.time + 12f; }
        public void Suspend() { if (visual) visual.Hide(); }
        public void Step(float dt)
        {
            if (!Owner || !Owner.alive || !Plugin.Ready || PauseHandler.paused || Time.timeScale <= 0 || SettingsMenu.instance)
            { Suspend(); return; }
            visual.Show();
            var anchor = Plugin.Anchor(Owner, out bool pilot); Piloting = pilot;
            phase += dt;
            var wound = WorstWound(out float severity);
            bool urgent = wound && !pilot && (wound.bleedAmount >= 1f || !Owner.conscious || Time.time < priorityUntil);
            bool canTreat = wound && !pilot;
            bool guarding = DroneSave.For(Owner).Guard;
            if (Time.time >= scanAt)
            { scanAt = Time.time + 0.3f; target = guarding ? SelectTarget(anchor) : null; }
            if (!guarding) target = null;
            bool treat = canTreat && (urgent || !target);
            Vector2 aim = treat ? (Vector2)wound.transform.position : anchor + Vector2.up * 1.4f;
            float side = Owner.transform.localScale.x >= 0 ? -1 : 1;
            float cruise = Plugin.PatrolRadius.Value;
            // A broad, slow ellipse makes the drone visibly patrol rather than stick to a shoulder.
            Vector2 patrolOffset = new Vector2(Mathf.Cos(phase * 0.24f) * cruise,
                cruise * 0.5f + Mathf.Abs(Mathf.Sin(phase * 0.24f)) * cruise * 0.25f);
            Vector2 preferred = treat ? aim + new Vector2(side * 2.3f, 1.1f) : anchor + patrolOffset;
            preferred.y += Mathf.Sin(phase * 2.2f) * 0.1f;
            Mode = treat ? "approach-medical" : target ? "guard" : pilot ? "mech-escort" : "follow";
            bool hasHover = DroneNavigation.FindHover(aim, preferred, out var goal);
            if (hasHover) Move(goal, anchor, dt);
            else { velocity = Vector2.zero; stuckTime += dt; }
            if (Time.time < recoveryUntil)
            {
                Mode = "redeploy"; visual.SetHealing(false, aim); visual.Animate(Vector2.zero, Mode, aim, phase); return;
            }
            bool healed = false;
            if (treat && Vector2.Distance(transform.position, aim) <= 3f && Visible(transform.position, aim, null, Owner))
            {
                float amount = Repair(wound, dt);
                if (amount > 0)
                { HealingSeconds += dt; RepairAmount += amount; healed = true; Mode = "healing"; }
            }
            visual.SetHealing(healed, aim);
            if (!healed && !urgent && guarding && target && Time.time >= shootAt)
            {
                if (IsEnemy(target) && Vector2.Distance(anchor, target.transform.position) <= 28 &&
                    CanSeeThreat(transform.position, target.transform.position, target))
                {
                    visual.Animate(velocity, Mode, target.transform.position, phase);
                    DroneProjectile.Fire(this, visual.Muzzle, target.transform.position, Plugin.Damage.Value);
                    visual.Shot(target.transform.position); Shots++; shootAt = Time.time + Plugin.ShotInterval.Value;
                }
                else target = null;
            }
            visual.Animate(velocity, Mode, target ? (Vector2)target.transform.position : aim, phase);
        }
        public void RecordImpact(bool friendly) { if (friendly) FriendlyHits++; else EnemyHits++; }
        void Move(Vector2 goal, Vector2 anchor, float dt)
        {
            Vector2 pos = transform.position;
            float distance = Vector2.Distance(pos, goal);
            if (distance > 35 || stuckTime > 2.5f)
            {
                // Explicit fold-and-redeploy fallback, with no attack/healing during recovery.
                if (DroneNavigation.FindHover(anchor, goal, out var recovery))
                { transform.position = recovery; Recoveries++; velocity = Vector2.zero; stuckTime = 0; route.Clear(); recoveryUntil = Time.time + 0.5f; shootAt = recoveryUntil; priorityUntil = 0; }
                previousPosition = transform.position;
                return;
            }
            if (Time.time >= routeAt || Vector2.Distance(goal, previousGoal) > 2)
            { routeAt = Time.time + 0.65f; previousGoal = goal; route = DroneNavigation.Route(pos, goal); }
            Vector2 next = pos;
            if (DroneNavigation.Clear(pos, goal)) next = goal;
            else
            {
                while (route.Count > 0 && Vector2.Distance(pos, route[0]) < 0.3f) route.RemoveAt(0);
                if (route.Count > 0) next = route[0];
            }
            Vector2 desired = Vector2.ClampMagnitude((next - pos) * 5f, Mathf.Min(22f, 7f + distance));
            velocity = Vector2.MoveTowards(velocity, desired, 60f * dt);
            var step = pos + velocity * dt;
            if (DroneNavigation.Clear(pos, step)) transform.position = step;
            else velocity = Vector2.zero;
            if (distance > 2 && Vector2.Distance(previousPosition, transform.position) < dt * 0.3f) stuckTime += dt;
            else stuckTime = 0;
            previousPosition = transform.position;
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
            return result;
        }
        static float Repair(Limb limb, float dt)
        {
            float before = limb.bleedAmount, skin = limb.skinHealth, muscle = limb.muscleHealth;
            limb.bleedAmount = Mathf.MoveTowards(Mathf.Max(0, before), 0, Plugin.BleedRepair.Value * dt);
            // Embedded fragments continue to require removal; do not heal past their cause.
            if (limb.bleedAmount < 0.1f && !limb.hasShrapnel)
            {
                limb.skinHealth = Mathf.MoveTowards(limb.skinHealth, 100, Plugin.TissueRepair.Value * dt);
                limb.muscleHealth = Mathf.MoveTowards(limb.muscleHealth, 100, Plugin.TissueRepair.Value * dt);
            }
            return Mathf.Max(0, before - limb.bleedAmount) + Mathf.Max(0, limb.skinHealth - skin) + Mathf.Max(0, limb.muscleHealth - muscle);
        }
        public static bool IsEnemy(BuildingEntity b)
        {
            if (!b || b.cantHit || b.indestructible || b.health <= 0) return false;
            // Explicit native threat components; neutral NPCs, traders, props and unknown mod entities are excluded.
            return b.GetComponent<SpiderHandler>() || b.GetComponent<CaveTicks>() || b.GetComponent<LaserDroneScript>();
        }
        BuildingEntity SelectTarget(Vector2 anchor)
        {
            seen.Clear(); BuildingEntity best = null; float bestDistance = 28f;
            int count = Physics2D.OverlapCircleNonAlloc(anchor, 28f, Nearby, LayerMask.GetMask("Ground", "Body", "Limb"));
            for (int i = 0; i < count; i++)
            {
                var b = Nearby[i] ? Nearby[i].GetComponentInParent<BuildingEntity>() : null;
                if (!b || !seen.Add(b.GetInstanceID()) || !IsEnemy(b)) continue;
                float dist = Vector2.Distance(anchor, b.transform.position);
                if (dist < bestDistance && (CanSeeThreat(transform.position, b.transform.position, b) ||
                    CanSeeThreat(anchor + Vector2.up * 1.4f, b.transform.position, b)))
                { best = b; bestDistance = dist; }
            }
            return best;
        }
        public static bool CanSeeThreat(Vector2 from, Vector2 to, BuildingEntity target)
        {
            var d = to-from;
            int count = Physics2D.RaycastNonAlloc(from,d.normalized,RayHits,d.magnitude,LayerMask.GetMask("Ground"));
            if (count == RayHits.Length) return false;
            for (int i=0; i<count; i++)
            {
                var c=RayHits[i].collider; if (!c || c.isTrigger) continue;
                var entity=c.GetComponentInParent<BuildingEntity>();
                if (entity && (entity==target || entity.animal)) continue;
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
                // Every other physical object blocks the ray, including player and friendly bodies.
                return false;
            }
            return true;
        }
    }
}
