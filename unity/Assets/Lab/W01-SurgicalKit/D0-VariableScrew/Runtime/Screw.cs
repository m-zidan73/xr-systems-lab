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

        // How quickly the drawn length catches up with the chosen one, per second.
        const float GlideSharpness = 14f;
        const float GlideArrivedMm = 0.02f;

        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;
        float shownLengthMm;
        bool gliding;

        public ScrewSpec Spec => spec;
        public float LengthMm => lengthMm;
        /// <summary>The length the shader draws right now. It equals LengthMm except during a glide.</summary>
        public float ShownLengthMm => gliding ? shownLengthMm : lengthMm;
        public bool IsGliding => gliding;
        public Vector3 TipPosition => tipPoint != null ? tipPoint.position : transform.position;

        /// <summary>"Ø 3.5 mm × 26 mm", for the scene label and the recording.</summary>
        public string Label => spec == null
            ? "no spec"
            : $"Ø {spec.diameterMm:0.#} mm × {lengthMm:0.#} mm";

        void OnEnable()   { gliding = false; Apply(); }
        void OnValidate() { gliding = false; Apply(); }

        void Update()
        {
            if (Application.isPlaying) Tick(Time.deltaTime);
        }

        /// <summary>
        /// Called by the tray at runtime, or by the custom inspector in the editor.
        /// Always snaps to a length the chosen system actually ships.
        /// </summary>
        public void Set(ScrewSpec newSpec, float requestedLengthMm)
        {
            spec = newSpec;
            lengthMm = spec != null ? spec.NearestAvailable(requestedLengthMm) : requestedLengthMm;
            gliding = false;
            Apply();
        }

        /// <summary>
        /// The same as Set, but the drawn shaft slides to the new length over a few frames. The
        /// chosen length, the collider and the tip are right at once, so nothing that measures
        /// the screw ever sees a size the catalog doesn't ship.
        /// </summary>
        public void SetSmooth(ScrewSpec newSpec, float requestedLengthMm)
        {
            float from = ShownLengthMm;
            bool hadSpec = spec != null;
            spec = newSpec;
            lengthMm = spec != null ? spec.NearestAvailable(requestedLengthMm) : requestedLengthMm;
            shownLengthMm = from;
            gliding = hadSpec && spec != null && Mathf.Abs(from - lengthMm) > GlideArrivedMm;
            Apply();
        }

        /// <summary>Moves a glide on by one step of time. Update calls it in Play mode.</summary>
        public void Tick(float deltaTime)
        {
            if (!gliding) return;
            shownLengthMm = Mathf.Lerp(shownLengthMm, lengthMm, 1f - Mathf.Exp(-GlideSharpness * deltaTime));
            if (Mathf.Abs(shownLengthMm - lengthMm) <= GlideArrivedMm) gliding = false;
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

            // Only the picture glides. Everything below the property block uses the chosen length.
            float stretchMm = ShownLengthMm - spec.baseMeshLengthMm;

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
