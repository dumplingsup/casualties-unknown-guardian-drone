using System.Collections.Generic;
using UnityEngine;

namespace GuardianDrone
{
    // Swept, finite-speed rounds. Friendly bodies are deliberately NOT filtered:
    // choosing an enemy target does not make the line of fire safe for bystanders.
    public sealed class DroneProjectile : MonoBehaviour
    {
        static readonly List<DroneProjectile> Active = new List<DroneProjectile>();
        static readonly RaycastHit2D[] Hits = new RaycastHit2D[128];
        static readonly int Mask = LayerMask.GetMask("Ground", "Body", "Limb", "Item");
        DroneController source;
        Vector2 velocity;
        float damage, age;
        LineRenderer line;
        public const float Speed = 55f;
        public const float SpreadDegrees = 1.25f;
        public const float Radius = 0.055f;
        public static int Count => Active.Count;
        public Vector2 Velocity => velocity;
        public static DroneProjectile Newest => Active.Count == 0 ? null : Active[Active.Count - 1];
        public static bool CorridorClear(Vector2 muzzle, Vector2 targetPoint, Body owner, BuildingEntity target)
        {
            var delta = targetPoint - muzzle;
            float distance = delta.magnitude;
            if (distance < 0.05f) return false;
            var forward = delta / distance;
            float spread = SpreadDegrees * Mathf.Deg2Rad;
            for (int i = -1; i <= 1; i++)
            {
                float angle = i * spread;
                var dir = new Vector2(forward.x * Mathf.Cos(angle) - forward.y * Mathf.Sin(angle),
                    forward.x * Mathf.Sin(angle) + forward.y * Mathf.Cos(angle));
                if (!RayClear(muzzle, dir, distance, owner, target)) return false;
            }
            return true;
        }
        static bool RayClear(Vector2 origin, Vector2 dir, float distance, Body owner, BuildingEntity target)
        {
            int count = Physics2D.CircleCastNonAlloc(origin, Radius, dir, Hits, distance, Mask);
            if (count == Hits.Length) return false;
            float closest = float.MaxValue; int best = -1;
            for (int i = 0; i < count; i++)
            {
                var c = Hits[i].collider; if (!c || c.isTrigger) continue;
                if (Hits[i].distance < closest) { best = i; closest = Hits[i].distance; }
            }
            if (best < 0) return true;
            var hit = Hits[best];
            var building = hit.collider.GetComponentInParent<BuildingEntity>();
            if (target && building == target) return true;
            var limb = hit.collider.GetComponentInParent<Limb>();
            var body = limb ? limb.body : hit.collider.GetComponentInParent<Body>();
            if (body) return false;
            return false;
        }
        public static void Fire(DroneController source, Vector2 origin, Vector2 target, float damage)
        {
            var go=new GameObject("Guardian pulse");go.transform.position=origin;
            var shot=go.AddComponent<DroneProjectile>();shot.source=source;shot.damage=damage;
            var direction=(target-origin).normalized;
            float spread=Random.Range(-SpreadDegrees,SpreadDegrees)*Mathf.Deg2Rad;
            shot.velocity=new Vector2(direction.x*Mathf.Cos(spread)-direction.y*Mathf.Sin(spread), direction.x*Mathf.Sin(spread)+direction.y*Mathf.Cos(spread))*Speed;
            shot.line=go.AddComponent<LineRenderer>();shot.line.sharedMaterial=DroneArt.LineMaterial;
            shot.line.positionCount=2;shot.line.useWorldSpace=true;
            shot.line.startWidth=0.22f;shot.line.endWidth=0.08f;
            shot.line.startColor=new Color(1,0.85f,0.35f);shot.line.endColor=new Color(1,0.35f,0.08f,0.2f);shot.line.sortingOrder=80;
            shot.line.SetPosition(0,origin);shot.line.SetPosition(1,origin-direction*0.55f);
            Active.Add(shot);
        }
        public static void StepAll(float dt)
        {
            if (!Plugin.Ready || PauseHandler.paused || Time.timeScale<=0 || SettingsMenu.instance) return;
            for (int i=Active.Count-1;i>=0;i--)
            {
                var shot=Active[i];
                if (!shot || !shot.source || !shot.Step(dt)) { if(shot) Destroy(shot.gameObject);Active.RemoveAt(i); }
            }
        }
        bool Step(float dt)
        {
            age+=dt;if(age>1.2f)return false;
            Vector2 pos=transform.position;
            int count=Physics2D.CircleCastNonAlloc(pos,Radius,velocity.normalized,Hits,velocity.magnitude*dt,Mask);
            float closest=float.MaxValue;int best=-1;
            for(int i=0;i<count;i++)
            {
                var c=Hits[i].collider;if(!c || c.isTrigger)continue;
                if(Hits[i].distance<closest){best=i;closest=Hits[i].distance;}
            }
            if(count==Hits.Length)return false;
            if(best>=0){Impact(Hits[best]);return false;}
            pos+=velocity*dt;transform.position=pos;
            line.SetPosition(0,pos);line.SetPosition(1,pos-velocity.normalized*1.8f);
            return true;
        }
        void Impact(RaycastHit2D hit)
        {
            var limb=hit.collider.GetComponentInParent<Limb>();
            var body=limb ? limb.body : hit.collider.GetComponentInParent<Body>();
            if(body && body.alive)
            {
                if(!limb)limb=body.GetClosestLimb(hit.point);
                if(!limb)return;
                float armor=Mathf.Max(0.1f,limb.GetArmorReduction());
                limb.skinHealth-=damage*0.6f/armor;limb.muscleHealth-=damage*0.35f/armor;
                limb.bleedAmount+=damage*0.04f/armor;limb.pain+=damage*0.3f/armor;
                body.adrenaline=Mathf.Max(body.adrenaline,30f);body.DoGoreSound();
                source.RecordImpact(true);
                Plugin.Log("PROJECTILE_BODY_HIT owner="+(body==source.Owner)+" limb="+limb.shortName+" damage="+damage);
                WorldGeneration.CreateDamageNumber(hit.point,Mathf.RoundToInt(damage/armor));
                return;
            }
            var building=hit.collider.GetComponentInParent<BuildingEntity>();
            if(building && !building.cantHit && !building.indestructible)
            {
                bool hostile=DroneController.IsEnemy(building);
                building.health-=damage;
                if(building && building.animal)building.SendMessage("AnimalHit",damage,SendMessageOptions.DontRequireReceiver);
                source.RecordImpact(!hostile);
                WorldGeneration.CreateDamageNumber(hit.point,Mathf.RoundToInt(damage));
            }
            // Terrain absorbs the round; it is not mined or destroyed.
        }
        public static void Clear(){foreach(var s in Active)if(s)Destroy(s.gameObject);Active.Clear();}
    }
}
