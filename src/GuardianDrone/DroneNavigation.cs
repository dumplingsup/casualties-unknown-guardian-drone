using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuardianDrone
{
    // Bounded local search for a small flying body. Neither enemies nor the player
    // are solid navigation obstacles. No rigidbody means the drone cannot shove them.
    public static class DroneNavigation
    {
        public const float Radius = 0.7f;
        static readonly int Ground = LayerMask.GetMask("Ground");
        static readonly Collider2D[] Overlaps = new Collider2D[64];
        static readonly RaycastHit2D[] Hits = new RaycastHit2D[64];
        static bool Solid(Collider2D c)
        {
            if (!c || c.isTrigger) return false;
            var b = c.GetComponentInParent<BuildingEntity>();
            return !b || !b.animal;
        }
        public static bool Free(Vector2 pos)
        {
            int count = Physics2D.OverlapCircleNonAlloc(pos, Radius, Overlaps, Ground);
            if (count == Overlaps.Length) return false;
            for (int i = 0; i < count; i++) if (Solid(Overlaps[i])) return false;
            return true;
        }
        public static bool Clear(Vector2 from, Vector2 to)
        {
            var d = to - from; if (d.sqrMagnitude < 0.001f) return Free(to);
            int count = Physics2D.CircleCastNonAlloc(from, Radius, d.normalized, Hits, d.magnitude, Ground);
            if (count == Hits.Length) return false;
            for (int i = 0; i < count; i++) if (Solid(Hits[i].collider)) return false;
            return Free(to);
        }
        public static bool FindHover(Vector2 anchor, Vector2 preferred, out Vector2 result)
        {
            if (Free(preferred)) { result = preferred; return true; }
            for (int r = 1; r <= 3; r++) for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6;
                var p = anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (r * 1.1f);
                if (Free(p)) { result = p; return true; }
            }
            result = anchor; return false;
        }
        public static List<Vector2> Route(Vector2 from, Vector2 to)
        {
            var route = new List<Vector2>();
            if (Clear(from, to)) { route.Add(to); return route; }
            if (Vector2.Distance(from, to) > 28) return route;
            var start = Vector2Int.RoundToInt(from); var goal = Vector2Int.RoundToInt(to);
            var open = new List<Vector2Int> { start };
            var cost = new Dictionary<Vector2Int, float> { [start] = 0 };
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var closed = new HashSet<Vector2Int>();
            var free = new Dictionary<Vector2Int, bool>();
            for (int iteration = 0; open.Count > 0 && iteration < 420; iteration++)
            {
                int best = 0; float score = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    float f = cost[open[i]] + Vector2Int.Distance(open[i], goal);
                    if (f < score) { best = i; score = f; }
                }
                var current = open[best]; open.RemoveAt(best); closed.Add(current);
                if (Vector2.Distance(current, to) < 1.5f && Clear(current, to))
                {
                    route.Add(to);
                    while (current != start) { route.Add(current); current = previous[current]; }
                    route.Reverse();
                    if (route.Count > 0 && !Clear(from, route[0])) route.Clear();
                    return route;
                }
                for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    var next = current + new Vector2Int(x, y);
                    if (closed.Contains(next) || Math.Abs(next.x - start.x) > 18 || Math.Abs(next.y - start.y) > 18) continue;
                    if (!free.TryGetValue(next, out bool can)) { can = Free(next); free[next] = can; }
                    if (!can || !Clear(current, next)) continue;
                    float g = cost[current] + ((x != 0 && y != 0) ? 1.4142f : 1f);
                    if (cost.TryGetValue(next, out float old) && old <= g) continue;
                    cost[next] = g; previous[next] = current;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
            return route;
        }
    }
}
