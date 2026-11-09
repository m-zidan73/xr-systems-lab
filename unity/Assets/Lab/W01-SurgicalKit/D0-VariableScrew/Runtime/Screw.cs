using UnityEngine;

namespace Lab.W01.VariableScrew
{
    /// <summary>
    /// Drives one screw instance. The mesh never changes: the shader stretches the shaft
    /// and the colour comes from the catalog, so every screw in the scene shares a single
    /// mesh and a single material.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public class Screw : MonoBehaviour
    {
        static readonly int StretchId   = Shader.PropertyToID("_Stretch");
        static readonly int HeadEndId   = Shader.PropertyToID("_HeadEnd");
        static readonly int ShaftLenId  = Shader.PropertyToID("_ShaftLength");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int TilingId    = Shader.PropertyToID("_ThreadTiling");
        static readonly int StartsId    = Shader.PropertyToID("_ThreadStarts");

        // Authored in metres. ScrewUnits decides how big the world is.
        const float MmToUnits = ScrewUnits.MillimetresToUnits;

        [SerializeField] ScrewSpec spec;
        [SerializeField] float lengthMm = 16f;

        [Header("Physics and logic have to follow the shader by hand")]
        [SerializeField] CapsuleCollider shaftCollider;
        [SerializeField] Transform tipPoint;

        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;

        public ScrewSpec Spec => spec;
        public float LengthMm => lengthMm;
        public Vector3 TipPosition => tipPoint != null ? tipPoint.position : transform.position;

        /// <summary>"Ø 3.5 mm × 26 mm", for the scene label and the recording.</summary>
        public string Label => spec == null
            ? "no spec"
            : $"Ø {spec.diameterMm:0.#} mm × {lengthMm:0.#} mm";

        void OnEnable()   => Apply();
        void OnValidate() => Apply();

        /// <summary>
        /// Called by the tray at runtime, or by the custom inspector in the editor.
        /// Always snaps to a length the chosen system actually ships.
        /// </summary>
        public void Set(ScrewSpec newSpec, float requestedLengthMm)
        {
            spec = newSpec;
            lengthMm = spec != null ? spec.NearestAvailable(requestedLengthMm) : requestedLengthMm;
            Apply();
        }

        void Apply()
        {
            if (spec == null) return;

            lengthMm = spec.NearestAvailable(lengthMm);

            // Carry world scale and diameter on the transform, so every shader value below
            // stays in the authored metre space and the maths never has to know. X and Y only:
            // Z carries length and pitch, which must not scale with diameter.
            float widthScale = ScrewUnits.WidthScale(spec.diameterMm);
            transform.localScale = new Vector3(widthScale, widthScale, 1f) * ScrewUnits.WorldScale;

            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (block == null) block = new MaterialPropertyBlock();

            float stretchMm = lengthMm - spec.baseMeshLengthMm;

            meshRenderer.GetPropertyBlock(block);
            block.SetFloat(StretchId,   stretchMm * MmToUnits);
            block.SetFloat(HeadEndId,   spec.headEndMm * MmToUnits);
            block.SetFloat(ShaftLenId,  spec.ShaftLengthMm * MmToUnits);
            block.SetColor(BaseColorId, spec.trayColor);
            // Pitch is per screw, not per material, so a 2.2 mm and a 7.0 mm screw show
            // visibly different thread spacing while sharing one material.
            block.SetFloat(TilingId,    spec.ThreadRepeatsAtBase);
            block.SetFloat(StartsId,    Mathf.Max(1, spec.threadStarts));
            meshRenderer.SetPropertyBlock(block);

            // The shader moved pixels, not physics. Move the collider and the tip marker too,
            // or every grab and every protrusion check still uses the base length.
            if (shaftCollider != null)
            {
                shaftCollider.direction = 2;                                   // Z axis
                shaftCollider.height    = lengthMm * MmToUnits;
                shaftCollider.center    = new Vector3(0f, 0f, lengthMm * MmToUnits * 0.5f);
                // Mesh space: the transform already carries the width.
                shaftCollider.radius    = ScrewUnits.MeshShaftRadius;
            }

            if (tipPoint != null)
                tipPoint.localPosition = new Vector3(0f, 0f, lengthMm * MmToUnits);
        }
    }
}
