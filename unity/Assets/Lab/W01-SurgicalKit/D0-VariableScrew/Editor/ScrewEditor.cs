using UnityEditor;
using UnityEngine;

namespace Lab.W01.VariableScrew.EditorTools
{
    /// <summary>
    /// The length slider, made correct.
    ///
    /// It slides over the INDEX into ScrewSpec.availableLengthsMm rather than over the
    /// millimetre value, so it snaps to exactly n positions by construction and can never
    /// rest on a length the system does not ship. The label shows the real value, and the
    /// swatch shows the tray colour, so the inspector reads like the physical tray.
    /// </summary>
    [CustomEditor(typeof(Screw))]
    public class ScrewEditor : Editor
    {
        SerializedProperty specProp;
        SerializedProperty lengthProp;
        SerializedProperty colliderProp;
        SerializedProperty tipProp;

        void OnEnable()
        {
            specProp     = serializedObject.FindProperty("spec");
            lengthProp   = serializedObject.FindProperty("lengthMm");
            colliderProp = serializedObject.FindProperty("shaftCollider");
            tipProp      = serializedObject.FindProperty("tipPoint");
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
                        "Snaps to the lengths this implant system actually ships. " +
                        "Real screw sets come in discrete lengths, so a continuous slider " +
                        "would let a trainee pick a screw that does not exist."),
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
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Physics follows the shader by hand", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(colliderProp, new GUIContent("Shaft Collider"));
            EditorGUILayout.PropertyField(tipProp, new GUIContent("Tip Point"));

            if (colliderProp.objectReferenceValue == null || tipProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Vertex displacement is a rendering effect. Without a collider and a tip " +
                    "point to resize, physics and any depth check still use the base mesh length.",
                    MessageType.Warning);
            }

            // Applying triggers OnValidate on the Screw, which pushes the new values
            // into the MaterialPropertyBlock and resizes the collider.
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Colour swatch plus diameter, so the inspector mirrors the physical tray.</summary>
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
