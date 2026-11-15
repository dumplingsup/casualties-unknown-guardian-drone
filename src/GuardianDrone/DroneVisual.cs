using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GuardianDrone
{
    public static class DroneArt
    {
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        static readonly List<Texture2D> Textures = new List<Texture2D>();
        public static Material SpriteMaterial, LineMaterial;
        public static Sprite Get(string key)
        {
            if (Sprites.TryGetValue(key, out var cached)) return cached;
            var asm = typeof(DroneArt).Assembly;
            string name = asm.GetManifestResourceNames().Single(n => n.EndsWith("." + key + ".png"));
            byte[] bytes;
            using (var s = asm.GetManifestResourceStream(name)) using (var m = new MemoryStream()) { s.CopyTo(m); bytes = m.ToArray(); }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            ImageConversion.LoadImage(texture, bytes); Textures.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, 16f);
            Sprites[key] = sprite; return sprite;
        }
        public static void Materials()
        {
            if (!SpriteMaterial) SpriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"));
            if (!LineMaterial)
            {
                var native = Resources.Load<GameObject>("Special/TurretLine");
                var renderer = native ? native.GetComponent<LineRenderer>() : null;
                LineMaterial = renderer && renderer.sharedMaterial ? new Material(renderer.sharedMaterial) : new Material(Shader.Find("Sprites/Default"));
                if (LineMaterial.HasProperty("_Color")) LineMaterial.SetColor("_Color", Color.white);
                if (LineMaterial.HasProperty("_BaseColor")) LineMaterial.SetColor("_BaseColor", Color.white);
            }
        }
        public static void Release()
        {
            foreach (var s in Sprites.Values) Object.Destroy(s);
            foreach (var t in Textures) Object.Destroy(t);
            Sprites.Clear(); Textures.Clear(); Object.Destroy(SpriteMaterial); Object.Destroy(LineMaterial);
        }
    }
    public sealed class DroneVisual : MonoBehaviour
    {
        public bool Healing { get; private set; }
        public Vector2 Muzzle => gun.TransformPoint(new Vector3(0.65f,0,0));
        Transform hull, gun;
        SpriteRenderer shell, eye, rotor;
        LineRenderer beam, pulse;
        float pulseUntil;
        Vector2 healTarget;
        int sorting;
        string layer;
        SpriteRenderer[] parts;
        Light2D lamp;
        public void Build(Body owner)
        {
            DroneArt.Materials();
            var source = owner.limbs[1].GetComponent<SpriteRenderer>();
            sorting = source ? source.sortingOrder + 15 : 35;
            layer = source ? source.sortingLayerName : "Default";
            hull = new GameObject("hull").transform; hull.SetParent(transform, false);
            shell = Part("hull", "hull", hull, Vector2.zero, 0);
            eye = Part("eye", "eye", hull, Vector2.zero, 1);
            rotor = Part("rotor", "rotor0", hull, Vector2.zero, 1);
            gun = new GameObject("tool").transform; gun.SetParent(hull, false); gun.localPosition = new Vector2(0.1f, -0.78f);
            Part("tool", "tool", gun, Vector2.zero, 2);
            beam = MakeLine("nanite-guide", new Color(0.25f, 1f, 0.68f, 0.8f), 0.08f);
            pulse = MakeLine("escort-pulse", new Color(1f, 0.65f, 0.2f, 1), 0.065f);
            parts = hull.GetComponentsInChildren<SpriteRenderer>();
            lamp = gameObject.AddComponent<Light2D>(); lamp.lightType = Light2D.LightType.Point;
            lamp.color = new Color(0.75f, 0.95f, 0.88f); lamp.intensity = 0.7f;
            lamp.pointLightOuterRadius = 4.5f; lamp.pointLightInnerRadius = 0.3f;
            lamp.pointLightOuterAngle = lamp.pointLightInnerAngle = 360; lamp.falloffIntensity = 0.5f;
        }
        SpriteRenderer Part(string name, string art, Transform parent, Vector2 pos, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = DroneArt.Get(art); sr.sharedMaterial = DroneArt.SpriteMaterial;
            sr.sortingLayerName = layer; sr.sortingOrder = sorting + order; return sr;
        }
        LineRenderer MakeLine(string name, Color color, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = DroneArt.LineMaterial;
            line.useWorldSpace = true; line.positionCount = 2; line.startWidth = width; line.endWidth = width * 0.4f;
            line.startColor = color; line.endColor = new Color(color.r, color.g, color.b, 0.35f);
            line.sortingLayerName = layer; line.sortingOrder = sorting + 3; line.enabled = false; return line;
        }
        public void SetHealing(bool active, Vector2 point) { Healing = active; healTarget = point; }
        public void Shot(Vector2 point)
        {
            pulseUntil = Time.time + 0.075f; pulse.enabled = true;
            Vector2 muzzle=Muzzle;
            pulse.SetPosition(0,muzzle);pulse.SetPosition(1,muzzle+(point-muzzle).normalized*0.4f);
        }
        public void Animate(Vector2 velocity, string mode, Vector2 aim, float phase)
        {
            hull.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(-velocity.x * 0.8f, -12, 12));
            eye.color = Healing ? new Color(0.3f, 1f, 0.65f) : mode == "guard" ? new Color(1f, 0.5f, 0.2f) : new Color(0.5f, 0.9f, 1f);
            rotor.sprite = DroneArt.Get(((int)(phase * 14) & 1) == 0 ? "rotor0" : "rotor1");
            var d = aim - (Vector2)gun.position;
            gun.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            beam.enabled = Healing;
            if (Healing)
            {
                beam.SetPosition(0, gun.position); beam.SetPosition(1, healTarget);
                beam.startWidth = 0.07f + Mathf.Sin(phase * 20) * 0.02f;
            }
            if (Time.time >= pulseUntil) pulse.enabled = false;
        }
        public void Hide() { lamp.enabled = false; beam.enabled = pulse.enabled = false; Healing = false; foreach (var sr in parts) sr.enabled = false; }
        public void Show() { lamp.enabled = true; foreach (var sr in parts) sr.enabled = true; }
    }
}
