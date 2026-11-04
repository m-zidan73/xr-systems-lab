using UnityEditor;
using UnityEngine;

namespace Lab.Core.EditorTools
{
    /// <summary>The lighting every demo scene starts from, built for inspecting small parts.</summary>
    public static class LabLighting
    {
        public static void Apply()
        {
            // A skybox-derived ambient probe is unreliable in a scene built from script, and
            // a near-metal surface with nothing to reflect renders close to black. Explicit
            // trilight ambient removes that dependency entirely.
            RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.44f, 0.47f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.33f, 0.36f);
            RenderSettings.ambientGroundColor  = new Color(0.17f, 0.17f, 0.19f);
            RenderSettings.ambientIntensity    = 1f;
            DynamicGI.UpdateEnvironment();

            // Shadows are off deliberately. URP's shadow bias is expressed in shadowmap
            // texels, and with the default 50 m shadow distance one texel is far larger than
            // a 20 mm screw, so the screws shadow themselves and go dark. Turning them off is
            // the right call for a floating tray; if you ever want them, drop the URP asset's
            // shadow distance to about 1 m first.
            EnsureLight("Key Light",  new Vector3( 42f, -38f, 0f), 2.1f, new Color(1.00f, 0.98f, 0.94f));
            EnsureLight("Fill Light", new Vector3( 18f, 145f, 0f), 0.9f, new Color(0.78f, 0.84f, 1.00f));
            EnsureLight("Rim Light",  new Vector3(-12f, 205f, 0f), 1.3f, new Color(1.00f, 0.94f, 0.82f));
        }

        static void EnsureLight(string name, Vector3 euler, float intensity, Color color)
        {
            var existing = GameObject.Find(name);
            var go = existing != null ? existing : new GameObject(name);

            var light = go.GetComponent<Light>();
            if (light == null) light = go.AddComponent<Light>();

            light.type      = LightType.Directional;
            light.intensity = intensity;
            light.color     = color;
            light.shadows   = LightShadows.None;
            go.transform.rotation = Quaternion.Euler(euler);

            EditorUtility.SetDirty(go);
        }
    }
}
