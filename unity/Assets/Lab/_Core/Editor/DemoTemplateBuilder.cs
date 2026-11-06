using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lab.Core.EditorTools
{
    /// <summary>Builds the scene every demo starts from: lighting, a floor and an orbit camera.</summary>
    public static class DemoTemplateBuilder
    {
        public const string ScenePath = "Assets/Lab/_Core/DemoTemplate.unity";
        const string FloorMaterialPath = "Assets/Lab/_Core/Floor.mat";
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
            floor.GetComponent<Renderer>().sharedMaterial = FloorMaterial();

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.24f, 0.26f, 0.30f);
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.01f;   // The lowest a scene can save. OrbitCamera goes smaller at runtime.
            camera.farClipPlane = 20f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 0.25f, -0.6f);
            cameraObject.transform.LookAt(Vector3.zero);
            cameraObject.AddComponent<OrbitCamera>().StartDistance = 0.65f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        // Dark and matte, so small parts stand out and the key light leaves no glare across the view.
        static Material FloorMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, FloorMaterialPath);
            }
            material.SetColor("_BaseColor", new Color(0.2f, 0.21f, 0.23f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
