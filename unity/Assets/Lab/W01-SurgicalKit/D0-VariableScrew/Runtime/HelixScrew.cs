using UnityEngine;

namespace Lab.W01.VariableScrew
{
    /// <summary>
    /// The geometry-thread variant. A plain shaft driven by the stretch shader, plus a
    /// separate ribbon child wrapped into a real helix by ScrewHelix.shader.
    ///
    /// The difference from <see cref="Screw"/> is the silhouette: this one actually has
    /// thread on its outline, which matters when a screw is seen up close, as it would be
    /// in VR. It costs about 3,200 vertices per screw, fixed whatever the length.
    /// </summary>
    [ExecuteAlways]
    public class HelixScrew : MonoBehaviour
    {
        // Shaft, using the existing stretch shader.
        static readonly int StretchId    = Shader.PropertyToID("_Stretch");
        static readonly int HeadEndId    = Shader.PropertyToID("_HeadEnd");
        static readonly int ShaftLenId   = Shader.PropertyToID("_ShaftLength");
        static readonly int CoreScaleId  = Shader.PropertyToID("_CoreScale");

        // Thread ribbon.
        static readonly int StartZId     = Shader.PropertyToID("_ThreadStartZ");
        static readonly int RunId        = Shader.PropertyToID("_ThreadRun");
        static readonly int PitchId      = Shader.PropertyToID("_Pitch");
        static readonly int CoreRadiusId = Shader.PropertyToID("_CoreRadius");
        static readonly int MajorRadiusId= Shader.PropertyToID("_MajorRadius");

        static readonly int BaseColorId  = Shader.PropertyToID("_BaseColor");

        const float MmToUnits = ScrewUnits.MillimetresToUnits;

        /// <summary>Shaft radius of the authored mesh, in metres. 3.5 mm nominal.</summary>
        public const float MeshShaftRadius = 0.00175f;
        const float MeshNominalDiameterMm  = 3.5f;

        [SerializeField] ScrewSpec spec;
        [SerializeField] float lengthMm = 16f;

        [Header("Renderers")]
        [SerializeField] MeshRenderer shaftRenderer;
        [SerializeField] MeshRenderer threadRenderer;

        [Header("Physics and logic follow the shaders by hand")]
        [SerializeField] CapsuleCollider shaftCollider;
        [SerializeField] Transform tipPoint;

        MaterialPropertyBlock shaftBlock;
        MaterialPropertyBlock threadBlock;

        public ScrewSpec Spec => spec;
        public float LengthMm => lengthMm;

        public string Label => spec == null
            ? "no spec"
            : $"Ø {spec.diameterMm:0.#} mm × {lengthMm:0.#} mm, {spec.threadPitchMm:0.00} mm pitch";

        void OnEnable()   => Apply();
        void OnValidate() => Apply();

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

            // Diameter is not modelled into the mesh, so scale it in. X and Y only: Z carries
            // length and pitch, which must not scale with diameter.
            float widthScale = spec.diameterMm / MeshNominalDiameterMm;
            transform.localScale = new Vector3(widthScale, widthScale, 1f) * ScrewUnits.WorldScale;

            // A screw is named by its MAJOR diameter, the crest of the thread, so the shaft
            // between threads must be thinner. The core is already in the data: you drill a
            // hole the size of the core, which is why a 3.5 mm screw takes a 2.5 mm bit.
            float coreRatio = Mathf.Clamp(
                spec.threadHoleDrillMm / Mathf.Max(spec.diameterMm, 0.01f), 0.2f, 0.95f);

            shaftBlock  ??= new MaterialPropertyBlock();
            threadBlock ??= new MaterialPropertyBlock();

            if (shaftRenderer != null)
            {
                shaftRenderer.GetPropertyBlock(shaftBlock);
                shaftBlock.SetFloat(StretchId,   (lengthMm - spec.baseMeshLengthMm) * MmToUnits);
                shaftBlock.SetFloat(HeadEndId,   spec.headEndMm * MmToUnits);
                shaftBlock.SetFloat(ShaftLenId,  spec.ShaftLengthMm * MmToUnits);
                shaftBlock.SetFloat(CoreScaleId, coreRatio);
                shaftBlock.SetColor(BaseColorId, spec.trayColor);
                shaftRenderer.SetPropertyBlock(shaftBlock);
            }

            if (threadRenderer != null)
            {
                // The thread runs from the underside of the head to the start of the tip.
                float runMm = Mathf.Max(lengthMm - spec.headEndMm - spec.tipLengthMm, 0.1f);

                threadRenderer.GetPropertyBlock(threadBlock);
                threadBlock.SetFloat(StartZId,   spec.headEndMm * MmToUnits);
                threadBlock.SetFloat(RunId,      runMm * MmToUnits);
                threadBlock.SetFloat(PitchId,    Mathf.Max(spec.threadPitchMm, 0.05f) * MmToUnits);
                // Object space. The transform still scales X and Y by nominal over 3.5, so the
                // crest at the mesh radius lands exactly on the nominal diameter.
                threadBlock.SetFloat(MajorRadiusId, MeshShaftRadius);
                threadBlock.SetFloat(CoreRadiusId,  MeshShaftRadius * coreRatio);
                threadBlock.SetColor(BaseColorId, spec.trayColor);
                threadRenderer.SetPropertyBlock(threadBlock);
            }

            if (shaftCollider != null)
            {
                shaftCollider.direction = 2;
                shaftCollider.height    = lengthMm * MmToUnits;
                shaftCollider.center    = new Vector3(0f, 0f, lengthMm * MmToUnits * 0.5f);
                shaftCollider.radius    = MeshShaftRadius;
            }

            if (tipPoint != null)
                tipPoint.localPosition = new Vector3(0f, 0f, lengthMm * MmToUnits);
        }

        /// <summary>Turns needed at the current length. Useful for sanity checking tessellation.</summary>
        public float TurnCount => spec == null
            ? 0f
            : Mathf.Max(lengthMm - spec.headEndMm - spec.tipLengthMm, 0f) /
              Mathf.Max(spec.threadPitchMm, 0.05f);
    }
}
