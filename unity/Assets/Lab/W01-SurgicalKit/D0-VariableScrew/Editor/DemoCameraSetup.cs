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
        const float StartDistance = 0.12f;

        public static void Apply(Camera cam)
        {
            var orbit = cam.GetComponent<OrbitCamera>();
            if (orbit == null) orbit = cam.gameObject.AddComponent<OrbitCamera>();
            orbit.StartDistance = StartDistance;
            EditorUtility.SetDirty(orbit);
        }

        /// <summary>Changes only the camera, so the tuned materials and tray stay as they are.</summary>
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
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
