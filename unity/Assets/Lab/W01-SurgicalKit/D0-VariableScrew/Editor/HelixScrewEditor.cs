using UnityEditor;
using UnityEngine;

namespace Lab.W01.VariableScrew.EditorTools
{
    /// <summary>
    /// The same snapping length slider as <see cref="ScrewEditor"/>, for the geometry-thread
    /// variant. Slides over the INDEX into ScrewSpec.availableLengthsMm rather than over
    /// millimetres, so it snaps to exactly n positions and can never rest on a length the
    /// system does not ship.
    ///
    /// Adds a turn count, because with a real helix the tessellation depends on how many
    /// turns the current length needs.
    /// </summary>
    [CustomEditor(typeof(HelixScrew))]
    public class HelixScrewEditor : Editor
    {
        SerializedProperty specProp;
        SerializedProperty lengthProp;
        SerializedProperty shaftRendererProp;
        SerializedProperty threadRendererProp;
        SerializedProperty colliderProp;
        SerializedProperty tipProp;

        void OnEnable()
        {
            specProp           = serializedObject.FindProperty("spec");
            lengthProp         = serializedObject.FindProperty("lengthMm");
            shaftRendererProp  = serializedObject.FindProperty("shaftRenderer");
            threadRendererProp = serializedObject.FindProperty("threadRenderer");
            colliderProp       = serializedObject.FindProperty("shaftCollider");
            tipProp            = serializedObject.FindProperty("tipPoint");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(specProp, new GUIContent("Screw Spec"));

            var spec = specProp.objectReferenceValue as ScrewSpec;
            bool usable = spec != null && spec.availableLengthsMm != null && spec.availableLengthsMm.Length > 0;

            EditorGUILayout.Space(4);

            if (!usable)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Screw Spec that lists at least one available length.",
                    MessageType.Info);
            }
            else
            {
                float[] lengths = spec.availableLengthsMm;
                int current = spec.NearestIndex(lengthProp.floatValue);

                DrawTrayRow(spec, lengths[current]);

                EditorGUI.BeginChangeCheck();
                int chosen = EditorGUILayout.IntSlider(
                    new GUIContent("Length",
                        "Snaps to the lengths this implant system actually ships. The helix is " +
                        "re-evaluated at the new length rather than stretched, so the pitch " +
                        "stays constant and the screw simply gains turns."),
                    current, 0, lengths.Length - 1);

                if (EditorGUI.EndChangeCheck())
                    lengthProp.floatValue = lengths[chosen];

                EditorGUILayout.LabelField(
                    " ",
                    $"{lengths[chosen]:0.#} mm     step {chosen + 1} of {lengths.Length}",
                    EditorStyles.miniLabel);

                if (lengths.Length > 1)
                {
                    EditorGUILayout.LabelField(
                        " ",
                        $"range {lengths[0]:0.#} to {lengths[lengths.Length - 1]:0.#} mm",
                        EditorStyles.miniLabel);
                }

                float run   = Mathf.Max(lengths[chosen] - spec.headEndMm - spec.tipLengthMm, 0f);
                float turns = run / Mathf.Max(spec.threadPitchMm, 0.05f);
                EditorGUILayout.LabelField(
                    " ",
                    $"{turns:0.#} turns     ribbon is authored for 640 segments",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Renderers", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(shaftRendererProp,  new GUIContent("Shaft Renderer"));
            EditorGUILayout.PropertyField(threadRendererProp, new GUIContent("Thread Renderer"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Physics follows the shaders by hand", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(colliderProp, new GUIContent("Shaft Collider"));
            EditorGUILayout.PropertyField(tipProp,     new GUIContent("Tip Point"));

            if (threadRendererProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "No thread renderer assigned, so the length still drives the shaft but the " +
                    "helix will not follow it.",
                    MessageType.Warning);
            }

            // Applying triggers OnValidate on the HelixScrew, which pushes the new values into
            // both property blocks and resizes the collider.
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Colour swatch plus diameter, pitch and length, mirroring the tray.</summary>
        static void DrawTrayRow(ScrewSpec spec, float lengthMm)
        {
            Rect row = EditorGUILayout.GetControlRect(false, 20f);

            Rect swatch = new Rect(row.x, row.y + 2f, 16f, 16f);
            EditorGUI.DrawRect(swatch, spec.trayColor);
            EditorGUI.DrawRect(new Rect(swatch.x, swatch.y, swatch.width, 1f), Color.black * 0.25f);

            Rect label = new Rect(row.x + 24f, row.y, row.width - 24f, row.height);
            EditorGUI.LabelField(label,
                $"Ø {spec.diameterMm:0.#} mm × {lengthMm:0.#} mm" +
                $"   ·   {spec.threadPitchMm:0.00} mm pitch" +
                (spec.locking ? "   ·   locking" : "   ·   non-locking"),
                EditorStyles.boldLabel);
        }
    }
}
