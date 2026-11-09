using System.Collections.Generic;
using System.Linq;
using Lab.Core.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lab.W01.VariableScrew.EditorTools
{
    /// <summary>
    /// Builds the whole demo from the imported FBX in one click: normalises the mesh axis,
    /// creates the material, assembles the prefab, and lays out the scene.
    ///
    /// Menu: Implants > Build Demo Scene
    /// </summary>
    public static class DemoBuilder
    {
        const string Root      = ScrewPaths.Root;
        const string FbxPath   = ScrewPaths.FbxPath;
        const string MeshPath  = ScrewPaths.MeshPath;
        const string MatPath   = ScrewPaths.MaterialPath;
        const string PrefabPath= ScrewPaths.PrefabPath;
        const string ScenePath = ScrewPaths.ScenePath;
        const string ShaderId  = "ScrewSystem/Screw";

        [MenuItem("Implants/Build Demo Scene")]
        public static void BuildDemo()
        {
            if (!OverwriteGuard.Allow(ScenePath)) return;

            var catalog = FindCatalog();
            if (catalog == null)
            {
                CatalogBuilder.CreateExampleCatalogs();
                catalog = FindCatalog();
            }
            if (catalog == null || catalog.screws == null || catalog.screws.Length == 0)
            {
                Debug.LogError("No implant catalog found. Run Implants > Create Example Catalogs first.");
                return;
            }

            var mesh = NormaliseMesh();
            if (mesh == null) return;

            var material = BuildMaterial();
            var prefab   = BuildPrefab(mesh, material, catalog.screws[0]);

            BuildScene(prefab, catalog);
        }

        // ---------------------------------------------------------------- mesh axis
        /// <summary>
        /// Guarantees the mesh runs along +Z with the head at z = 0, whatever the FBX
        /// exporter did with axes. This removes the entire class of Blender to Unity
        /// orientation bugs, which the shader would otherwise fail on silently.
        /// </summary>
        static Mesh NormaliseMesh()
        {
            var source = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Mesh>().FirstOrDefault();
            if (source == null)
            {
                Debug.LogError($"No mesh found at {FbxPath}. Export the screw from Blender first.");
                return null;
            }

            Vector3 size = source.bounds.size;
            Quaternion rotation = Quaternion.identity;

            if (size.y > size.x && size.y > size.z)       rotation = Quaternion.AngleAxis( 90f, Vector3.right); // +Y to +Z
            else if (size.x > size.y && size.x > size.z)  rotation = Quaternion.AngleAxis(-90f, Vector3.up);    // +X to +Z

            var vertices = source.vertices;
            var normals  = source.normals;
            var tangents = source.tangents;

            for (int i = 0; i < vertices.Length; i++) vertices[i] = rotation * vertices[i];
            for (int i = 0; i < normals.Length;  i++) normals[i]  = rotation * normals[i];
            for (int i = 0; i < tangents.Length; i++)
            {
                Vector3 t = rotation * new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
            }

            // The head is the fat end. If it ended up at the far end, spin the mesh around.
            if (HeadIsAtFarEnd(vertices))
            {
                Quaternion flip = Quaternion.AngleAxis(180f, Vector3.right);
                for (int i = 0; i < vertices.Length; i++) vertices[i] = flip * vertices[i];
                for (int i = 0; i < normals.Length;  i++) normals[i]  = flip * normals[i];
                for (int i = 0; i < tangents.Length; i++)
                {
                    Vector3 t = flip * new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                    tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
                }
            }

            float minZ = vertices.Min(v => v.z);
            for (int i = 0; i < vertices.Length; i++) vertices[i] -= new Vector3(0f, 0f, minZ);

            var mesh = new Mesh { name = "Screw_Base_Normalized", indexFormat = source.indexFormat };
            mesh.SetVertices(vertices);
            if (normals.Length  == vertices.Length) mesh.SetNormals(normals);
            if (tangents.Length == vertices.Length) mesh.SetTangents(tangents);
            mesh.SetUVs(0, source.uv.ToList());
            mesh.SetColors(source.colors.ToList());
            mesh.subMeshCount = source.subMeshCount;
            for (int s = 0; s < source.subMeshCount; s++)
                mesh.SetTriangles(source.GetTriangles(s), s);
            mesh.RecalculateBounds();

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, MeshPath);

            AssetDatabase.SaveAssets();

            if (source.colors == null || source.colors.Length == 0)
            {
                Debug.LogWarning("The imported mesh has no vertex colours, so the shaft mask is " +
                                 "missing and the thread will appear on the head. Check the FBX " +
                                 "importer has vertex colours enabled.");
            }

            Debug.Log($"Normalised mesh: length {mesh.bounds.size.z * 1000f:0.#} mm along Z, " +
                      $"{mesh.vertexCount} verts, colours {(source.colors.Length > 0 ? "present" : "MISSING")}.");
            return mesh;
        }

        /// <summary>Compare the radius at each end. The head is wider than the tip.</summary>
        static bool HeadIsAtFarEnd(Vector3[] vertices)
        {
            float minZ = vertices.Min(v => v.z);
            float maxZ = vertices.Max(v => v.z);
            float band = (maxZ - minZ) * 0.1f;

            float nearR = vertices.Where(v => v.z <= minZ + band)
                                  .Select(v => new Vector2(v.x, v.y).magnitude)
                                  .DefaultIfEmpty(0f).Max();
            float farR  = vertices.Where(v => v.z >= maxZ - band)
                                  .Select(v => new Vector2(v.x, v.y).magnitude)
                                  .DefaultIfEmpty(0f).Max();
            return farR > nearR;
        }

        // ----------------------------------------------------------------- material
        /// <summary>Rebuilds the material and reassigns the thread map. Use after replacing
        /// the generated normal map with your own bake.</summary>
        [MenuItem("Implants/Refresh Screw Material")]
        public static void RefreshMaterial()
        {
            if (BuildMaterial() != null) Debug.Log("Screw material refreshed.");
        }

        static Material BuildMaterial()
        {
            var shader = Shader.Find(ShaderId);
            if (shader == null)
            {
                Debug.LogError($"Shader '{ShaderId}' not found. Let it compile, then run this again.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Screw" };
                AssetDatabase.CreateAsset(material, MatPath);
            }
            material.shader = shader;
            material.enableInstancing = true;
            material.SetFloat("_Metallic", 0.30f);
            material.SetFloat("_Smoothness", 0.62f);

            var thread = AssetDatabase.LoadAssetAtPath<Texture2D>(ScrewPaths.ThreadNormalPath);
            if (thread != null)
            {
                material.SetTexture("_ThreadNormal", thread);
                material.SetFloat("_NormalStrength", 1f);
            }
            else
            {
                Debug.LogWarning("No Thread_Normal.png in Art/Textures, so the shaft " +
                                 "will render smooth. The shader is fine; there is just no map.");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------- prefab
        static GameObject BuildPrefab(Mesh mesh, Material material, ScrewSpec defaultSpec)
        {
            var go = new GameObject("Screw");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;

            var collider = go.AddComponent<CapsuleCollider>();
            collider.direction = 2;

            var tip = new GameObject("TipPoint");
            tip.transform.SetParent(go.transform, false);

            var screw = go.AddComponent<Screw>();

            var so = new SerializedObject(screw);
            so.FindProperty("spec").objectReferenceValue           = defaultSpec;
            so.FindProperty("shaftCollider").objectReferenceValue  = collider;
            so.FindProperty("tipPoint").objectReferenceValue       = tip.transform;
            so.FindProperty("lengthMm").floatValue                 = defaultSpec.availableLengthsMm[0];
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // -------------------------------------------------------------------- scene
        static void BuildScene(GameObject prefab, ImplantCatalog catalog)
        {
            if (prefab == null) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            LabLighting.Apply();

            var specs = catalog.screws.Where(s => s != null).ToArray();
            float spacing = ScrewUnits.MmWorld(7f);                  // 7 mm between screws
            float top = (specs.Length - 1) * spacing * 0.5f;

            var row = new GameObject("Tray").transform;

            for (int i = 0; i < specs.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = $"Screw {specs[i].diameterMm:0.0} mm";
                instance.transform.SetParent(row, false);

                // Local +Z becomes world +X so the screws lie across the view.
                instance.transform.SetPositionAndRotation(
                    new Vector3(ScrewUnits.MmWorld(-20f), top - i * spacing, 0f),
                    Quaternion.Euler(0f, 90f, 0f));

                var lengths = specs[i].availableLengthsMm;
                instance.GetComponent<Screw>().Set(specs[i], lengths[lengths.Length / 2]);
            }

            var hero = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            hero.name = "Hero Screw";
            hero.transform.SetPositionAndRotation(
                new Vector3(ScrewUnits.MmWorld(-20f), top + spacing * 1.8f, ScrewUnits.MmWorld(-12f)),
                Quaternion.Euler(0f, 90f, 0f));
            hero.GetComponent<Screw>().Set(specs[specs.Length / 2],
                                           specs[specs.Length / 2].availableLengthsMm[0]);
            TrayLayout.ApplyToStack(row, hero.transform);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = ScrewUnits.NearClip;                 // a 20 mm screw needs this
            cam.farClipPlane  = 5f * ScrewUnits.WorldScale;
            cam.fieldOfView   = 38f;
            cam.backgroundColor = new Color(0.24f, 0.26f, 0.30f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.SetPositionAndRotation(
                new Vector3(0f, ScrewUnits.MmWorld(4f), ScrewUnits.MmWorld(-120f)), Quaternion.identity);
            DemoCameraSetup.Apply(cam);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Selection.activeGameObject = hero;
            EditorGUIUtility.PingObject(hero);

            Debug.Log($"Demo scene built at {ScenePath}. The Hero Screw is selected: drag its " +
                      $"Length slider in the inspector.");
        }

        // ----------------------------------------------------------------- lighting
        /// <summary>
        /// Applies lighting that works at this scale. Run it on an existing scene without
        /// rebuilding anything.
        /// </summary>
        [MenuItem("Implants/Fix Scene Lighting")]
        public static void FixSceneLighting()
        {
            LabLighting.Apply();

            var material = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (material != null)
            {
                material.SetFloat("_Metallic", 0.30f);
                material.SetFloat("_Smoothness", 0.62f);
                EditorUtility.SetDirty(material);
            }

            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.24f, 0.26f, 0.30f);
                camera.nearClipPlane = ScrewUnits.NearClip;
                EditorUtility.SetDirty(camera);
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("Lighting fixed: shadows off, three lights, brighter ambient, metallic " +
                      "dropped to 0.30. Save the scene to keep it.");
        }

        static ImplantCatalog FindCatalog()
        {
            var guid = AssetDatabase.FindAssets("t:ImplantCatalog", new[] { Root }).FirstOrDefault();
            return guid == null
                ? null
                : AssetDatabase.LoadAssetAtPath<ImplantCatalog>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
