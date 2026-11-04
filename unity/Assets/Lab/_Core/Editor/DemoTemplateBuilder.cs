using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lab.Core.EditorTools
{
    /// <summary>Builds the scene every demo starts from: lighting, a floor and an orbit camera.</summary>
    public static class DemoTemplateBuilder
    {
        const string ScenePath = "Assets/Lab/_Core/DemoTemplate.unity";
        const float FloorSizeMetres = 2f;
        const float UnityPlaneSizeMetres = 10f;

        [MenuItem("Lab/Build Demo Template")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            LabLighting.Apply();

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = Vector3.one * (FloorSizeMetres / UnityPlaneSizeMetres);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.24f, 0.26f, 0.30f);
            camera.fieldOfView = 38f;
            camera.farClipPlane = 20f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 0.25f, -0.6f);
            cameraObject.transform.LookAt(Vector3.zero);
            cameraObject.AddComponent<OrbitCamera>().StartDistance = 0.65f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }
    }
}
