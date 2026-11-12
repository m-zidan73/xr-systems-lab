using Lab.W01.ColourCodedHoles;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.StepGuidance
{
    /// <summary>
    /// Puts a pulsing ring around the hole and the screw the current step uses. The rings are separate
    /// objects, so they never fight the hover highlight or the catalog colours on the parts themselves.
    /// Each marker is a flat ring mesh with an outer radius of 1, scaled to fit what it rings: X across,
    /// Z along.
    /// </summary>
    public class StepHighlighter : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] StepGuide guide;
        [SerializeField] Renderer holeMarker;
        [SerializeField] Renderer screwMarker;
        [SerializeField] Color colour = new Color(1f, 0.86f, 0.3f, 0.95f);
        [SerializeField] float pulsesPerSecond = 1.2f;
        [SerializeField, Range(0f, 1f)] float faintest = 0.35f;
        [Tooltip("Metres from the edge of the hole to the outside of its ring, past the hole's own colour ring.")]
        [SerializeField] float holeMargin = 0.0045f;
        [Tooltip("Metres from the screw's head and ends to the outside of its ring.")]
        [SerializeField] float screwMargin = 0.0015f;
        [Tooltip("Metres the hole's ring floats above the hole, clear of its colour ring.")]
        [SerializeField] float holeLift = 0.0008f;

        StepGuide _connected;
        MaterialPropertyBlock _block;

        public Hole TargetHole { get; private set; }
        public Screw TargetScrew { get; private set; }

        void OnEnable() => Connect(guide);
        void OnDisable() => Disconnect();

        public void Connect(StepGuide to)
        {
            Disconnect();
            _connected = to;
            if (_connected != null)
            {
                _connected.StepChanged += OnStepChanged;
                _connected.Restarted += Show;
            }
            Show();
        }

        public void Disconnect()
        {
            if (_connected != null)
            {
                _connected.StepChanged -= OnStepChanged;
                _connected.Restarted -= Show;
            }
            _connected = null;
        }

        void OnStepChanged(int from, int to) => Show();

        void Show()
        {
            var step = _connected != null ? _connected.Current : null;
            TargetHole = step != null ? step.hole : null;
            TargetScrew = step != null ? step.screw : null;
            if (holeMarker != null) holeMarker.enabled = TargetHole != null;
            if (screwMarker != null) screwMarker.enabled = TargetScrew != null;

            if (TargetHole != null && holeMarker != null)
            {
                var hole = TargetHole.transform;
                var radius = TargetHole.DiameterMm * 0.0005f + holeMargin;
                holeMarker.transform.SetPositionAndRotation(hole.position + hole.up * holeLift, hole.rotation);
                holeMarker.transform.localScale = new Vector3(radius, 1f, radius);
            }
            Follow();
        }

        /// <summary>
        /// Keeps the screw's ring on the screw while it is carried. The ring is an oval along the screw as it
        /// lies, so it doesn't reach round its neighbours in the tray, and closes to a circle round the head
        /// when the screw is held upright.
        /// </summary>
        public void Follow()
        {
            if (TargetScrew == null || screwMarker == null) return;
            var head = TargetScrew.transform.position;
            var tip = TargetScrew.TipPosition;
            var headRadius = TargetScrew.Spec != null
                ? ScrewUnits.MmWorld(ScrewUnits.HeadDiameterMm(TargetScrew.Spec.diameterMm)) * 0.5f
                : 0f;
            var across = headRadius + screwMargin;
            var flat = Vector3.ProjectOnPlane(tip - head, Vector3.up);
            var along = Mathf.Max(across, flat.magnitude * 0.5f + screwMargin);
            var rotation = flat.sqrMagnitude > 1e-10f ? Quaternion.LookRotation(flat, Vector3.up) : Quaternion.identity;
            screwMarker.transform.SetPositionAndRotation((head + tip) * 0.5f, rotation);
            screwMarker.transform.localScale = new Vector3(across, 1f, along);
        }

        void LateUpdate()
        {
            Follow();
            var wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulsesPerSecond * 2f * Mathf.PI);
            var now = colour;
            now.a = colour.a * Mathf.Lerp(faintest, 1f, wave);
            Paint(holeMarker, now);
            Paint(screwMarker, now);
        }

        void Paint(Renderer marker, Color value)
        {
            if (marker == null || !marker.enabled) return;
            _block ??= new MaterialPropertyBlock();
            marker.GetPropertyBlock(_block);
            _block.SetColor(ColorId, value);
            marker.SetPropertyBlock(_block);
        }
    }
}
