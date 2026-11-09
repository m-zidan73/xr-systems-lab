using System.Linq;
using Lab.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lab.W01.VariableScrew.EditorTools
{
    /// <summary>Gives the demo scenes the lab's orbit camera, sized for a tray of screws.</summary>
    public static class DemoCameraSetup
    {
        // Far enough back to show all ten screws at their real widths, including the hero above the tray.
        const float StartDistance = 0.17f;
        static readonly Vector3 StartPosition = new Vector3(0f, 0.008f, -StartDistance);

        public static void Apply(Camera cam)
        {
            var orbit = cam.GetComponent<OrbitCamera>();
            if (orbit == null) orbit = cam.gameObject.AddComponent<OrbitCamera>();
            cam.transform.position = StartPosition;
            orbit.StartDistance = StartDistance;
            EditorUtility.SetDirty(orbit);
            EditorUtility.SetDirty(cam.transform);
        }

        /// <summary>Changes only the camera and the heights in the stack, so tuned materials and sizes stay as they are.</summary>
        [MenuItem("Implants/Add Orbit Camera To Demo Scenes")]
        public static void UpgradeScenes()
        {
            foreach (var path in new[] { ScrewPaths.ScenePath, ScrewPaths.HelixScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var camera = scene.GetRootGameObjects()
                    .Select(root => root.GetComponent<Camera>())
                    .First(found => found != null && found.name == "Main Camera");
                Apply(camera);
                var roots = scene.GetRootGameObjects();
                TrayLayout.ApplyToStack(
                    roots.First(root => root.name == "Tray").transform,
                    roots.First(root => root.name == "Hero Screw").transform);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
