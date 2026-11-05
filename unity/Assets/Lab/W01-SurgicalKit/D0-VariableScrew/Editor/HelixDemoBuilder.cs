using Lab.Core.EditorTools;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lab.W01.VariableScrew.EditorTools
{
    /// <summary>
    /// Builds the geometry-thread demo into its own scene, so it sits beside the
    /// normal-mapped one rather than replacing it. Having both is the point: one is free
    /// and lies about the silhouette, the other is correct and costs vertices.
    ///
    /// Menu: Implants > Build Helix Demo Scene
    /// </summary>
    public static class HelixDemoBuilder
    {
        const string Root       = ScrewPaths.Root;
        const string RibbonFbx  = ScrewPaths.RibbonFbxPath;
        const string RibbonMesh = ScrewPaths.RibbonMeshPath;
        const string ShaftMesh  = ScrewPaths.MeshPath;
        const string ShaftMat   = ScrewPaths.ShaftMaterialPath;
        const string ThreadMat  = ScrewPaths.ThreadMaterialPath;
        const string PrefabPath = ScrewPaths.HelixPrefabPath;
        const string ScenePath  = ScrewPaths.HelixScenePath;

        [MenuItem("Implants/Build Helix Demo Scene")]
        public static void BuildHelixDemo()
        {
            if (!OverwriteGuard.Allow(ScenePath)) return;

            var catalog = FindCatalog();
            if (catalog == null || catalog.screws == null || catalog.screws.Length == 0)
            {
                Debug.LogError("No implant catalog. Run Implants > Create Example Catalogs first.");
                return;
            }

            var shaft = AssetDatabase.LoadAssetAtPath<Mesh>(ShaftMesh);
            if (shaft == null)
            {
                Debug.LogError($"No shaft mesh at {ShaftMesh}. Run Implants > Build Demo Scene " +
                               $"first, which normalises the screw mesh out of the FBX.");
                return;
            }

            var ribbon = NormaliseRibbon();
            if (ribbon == null) return;

            var shaftMaterial  = BuildMaterial(ShaftMat,  "ScrewSystem/Screw",      plainShaft: true);
            var threadMaterial = BuildMaterial(ThreadMat, "ScrewSystem/ScrewHelix", plainShaft: false);
            if (shaftMaterial == null || threadMaterial == null) return;

            var prefab = BuildPrefab(shaft, ribbon, shaftMaterial, threadMaterial, catalog.screws[0]);
            BuildScene(prefab, catalog);
        }

        // ------------------------------------------------------------ ribbon encoding
        /// <summary>
        /// The ribbon is an encoded mesh, not a shape, so the FBX axis conversion has to land
        /// on the exact channels the shader reads. Rather than trust it, detect the axes and
        /// swizzle: s is the axis spanning 0 to 1, axial is the symmetric one, depth is what
        /// is left. Then the shader can never be silently fed the wrong channel.
        /// </summary>
        static Mesh NormaliseRibbon()
        {
            var source = AssetDatabase.LoadAllAssetsAtPath(RibbonFbx).OfType<Mesh>().FirstOrDefault();
            if (source == null)
            {
                Debug.LogError($"No mesh at {RibbonFbx}. Export Thread_Ribbon from Blender first.");
                return null;
            }

            var vertices = source.vertices;
            var normals  = source.normals;

            int sAxis = -1, axialAxis = -1, depthAxis = -1;
            float sSign = 1f, depthSign = 1f;

            for (int a = 0; a < 3; a++)
            {
                float min = vertices.Min(v => v[a]);
                float max = vertices.Max(v => v[a]);

                // s runs 0 to 1, or 0 to -1 if the exporter flipped handedness on this axis.
                if (Mathf.Abs(min) < 0.02f && Mathf.Abs(max - 1f) < 0.02f)      { sAxis = a; sSign =  1f; }
                else if (Mathf.Abs(max) < 0.02f && Mathf.Abs(min + 1f) < 0.02f) { sAxis = a; sSign = -1f; }
                else if (Mathf.Abs(min + max) < 0.05f)                          { axialAxis = a; }
                else { depthAxis = a; depthSign = Mathf.Abs(max) > Mathf.Abs(min) ? 1f : -1f; }
            }

            if (sAxis < 0 || axialAxis < 0 || depthAxis < 0)
            {
                Debug.LogError("Could not identify the ribbon's encoded axes. Expected one axis " +
                               "spanning 0 to 1 (s), one symmetric about zero (axial), and one " +
                               "asymmetric (depth). Re-export from Blender.");
                return null;
            }

            var outVerts   = new Vector3[vertices.Length];
            var outNormals = new Vector3[normals.Length];
            for (int i = 0; i < vertices.Length; i++)
                outVerts[i] = new Vector3(depthSign * vertices[i][depthAxis],
                                          vertices[i][axialAxis],
                                          sSign * vertices[i][sAxis]);
            for (int i = 0; i < normals.Length; i++)
                outNormals[i] = new Vector3(depthSign * normals[i][depthAxis],
                                            normals[i][axialAxis],
                                            sSign * normals[i][sAxis]).normalized;

            var mesh = new Mesh { name = "Thread_Ribbon_Encoded", indexFormat = source.indexFormat };
            mesh.SetVertices(outVerts.ToList());
            if (outNormals.Length == outVerts.Length) mesh.SetNormals(outNormals.ToList());
            mesh.subMeshCount = source.subMeshCount;
            for (int s = 0; s < source.subMeshCount; s++)
                mesh.SetTriangles(source.GetTriangles(s), s);

            // The bounds are in encoded space and mean nothing once the shader wraps it, so
            // set something generous or the renderer culls the screw when it should not.
            mesh.bounds = new Bounds(new Vector3(0f, 0f, 0.05f), new Vector3(0.03f, 0.03f, 0.12f));

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(RibbonMesh);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, RibbonMesh);

            AssetDatabase.SaveAssets();

            Debug.Log($"Ribbon encoded: depth axis {depthAxis}, axial {axialAxis}, s {sAxis}. " +
                      $"{mesh.vertexCount} verts, {mesh.triangles.Length / 3} tris.");
            return mesh;
        }

        // ----------------------------------------------------------------- materials
        static Material BuildMaterial(string path, string shaderId, bool plainShaft)
        {
            var shader = Shader.Find(shaderId);
            if (shader == null)
            {
                Debug.LogError($"Shader '{shaderId}' not found. Let it compile, then run again.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.enableInstancing = true;
            material.SetFloat("_Metallic", 0.30f);
            material.SetFloat("_Smoothness", 0.62f);

            // The shaft carries no painted thread here: the thread is real geometry now.
            if (plainShaft) material.SetFloat("_NormalStrength", 0f);

            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------- prefab
        static GameObject BuildPrefab(Mesh shaftMesh, Mesh ribbonMesh,
                                      Material shaftMaterial, Material threadMaterial,
                                      ScrewSpec defaultSpec)
        {
            var root = new GameObject("ScrewHelix");

            var shaft = new GameObject("Shaft");
            shaft.transform.SetParent(root.transform, false);
            shaft.AddComponent<MeshFilter>().sharedMesh = shaftMesh;
            shaft.AddComponent<MeshRenderer>().sharedMaterial = shaftMaterial;

            var thread = new GameObject("Thread");
            thread.transform.SetParent(root.transform, false);
            thread.AddComponent<MeshFilter>().sharedMesh = ribbonMesh;
            var threadRenderer = thread.AddComponent<MeshRenderer>();
            threadRenderer.sharedMaterial = threadMaterial;

            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 2;

            var tip = new GameObject("TipPoint");
            tip.transform.SetParent(root.transform, false);

            var screw = root.AddComponent<HelixScrew>();

            var so = new SerializedObject(screw);
            so.FindProperty("spec").objectReferenceValue           = defaultSpec;
            so.FindProperty("lengthMm").floatValue                 = defaultSpec.availableLengthsMm[0];
            so.FindProperty("shaftRenderer").objectReferenceValue  = shaft.GetComponent<MeshRenderer>();
            so.FindProperty("threadRenderer").objectReferenceValue = threadRenderer;
            so.FindProperty("shaftCollider").objectReferenceValue  = collider;
            so.FindProperty("tipPoint").objectReferenceValue       = tip.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // -------------------------------------------------------------------- scene
        static void BuildScene(GameObject prefab, ImplantCatalog catalog)
        {
            if (prefab == null) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LabLighting.Apply();

            var specs = catalog.screws.Where(s => s != null).ToArray();
            float spacing = ScrewUnits.MmWorld(8f);
            float top = (specs.Length - 1) * spacing * 0.5f;

            var tray = new GameObject("Tray").transform;

            for (int i = 0; i < specs.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = $"Screw {specs[i].diameterMm:0.0} mm";
                instance.transform.SetParent(tray, false);
                instance.transform.SetPositionAndRotation(
                    new Vector3(ScrewUnits.MmWorld(-20f), top - i * spacing, 0f),
                    Quaternion.Euler(0f, 90f, 0f));

                var lengths = specs[i].availableLengthsMm;
                instance.GetComponent<HelixScrew>().Set(specs[i], lengths[lengths.Length / 2]);
            }

            var hero = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            hero.name = "Hero Screw";
            hero.transform.SetPositionAndRotation(
                new Vector3(ScrewUnits.MmWorld(-20f), top + spacing * 1.9f, ScrewUnits.MmWorld(-14f)),
                Quaternion.Euler(0f, 90f, 0f));
            var heroSpec = specs[specs.Length / 2];
            hero.GetComponent<HelixScrew>().Set(heroSpec, heroSpec.availableLengthsMm[0]);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane   = ScrewUnits.NearClip;
            cam.farClipPlane    = 5f * ScrewUnits.WorldScale;
            cam.fieldOfView     = 38f;
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.24f, 0.26f, 0.30f);
            camGo.transform.SetPositionAndRotation(
                new Vector3(0f, ScrewUnits.MmWorld(4f), ScrewUnits.MmWorld(-130f)), Quaternion.identity);
            DemoCameraSetup.Apply(cam);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Selection.activeGameObject = hero;
            EditorGUIUtility.PingObject(hero);

            Debug.Log($"Helix demo built at {ScenePath}. Hero screw selected. " +
                      $"About {prefab.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount} " +
                      $"verts of shaft plus 3,205 of thread, per screw, at any length.");
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
