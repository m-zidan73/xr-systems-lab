using System;
using Lab.W01.ColourCodedHoles;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.HoleTools
{
    public enum DrillResult { Drilled, AlreadyDrilled, NotInCatalog }

    public enum GaugeResult { Measured, NotDrilled, NoBone }

    /// <summary>
    /// What has been done to one plate hole before its screw goes in: drilled, then measured. The drill
    /// bit comes from the catalog, so there is nothing to choose. The depth is measured through the bone
    /// under the hole, so every hole reads differently and the reading follows the plate. It reads no
    /// input: the mouse tools today, and a VR hand later, call <see cref="Drill"/> and <see cref="Measure"/>.
    /// </summary>
    [RequireComponent(typeof(Hole))]
    public class HoleSite : MonoBehaviour
    {
        const float MovedMetres = 0.0001f;
        const float MovedDegrees = 0.1f;

        [Tooltip("The bone under the plate. Only this collider is measured.")]
        [SerializeField] Collider bone;
        [Tooltip("Metres past the hole to look for the far side. Longer than the bone is thick.")]
        [SerializeField, Min(0.001f)] float reachMetres = 0.08f;
        [Tooltip("Shown once the hole is drilled.")]
        [SerializeField] GameObject bore;

        Hole _hole;
        Vector3 _measuredAt;
        Quaternion _measuredFacing;

        public event Action<HoleSite> Drilled;
        public event Action<HoleSite> Measured;
        /// <summary>Raised when the plate moved after measuring, so the reading no longer holds.</summary>
        public event Action<HoleSite> ReadingLost;
        public event Action<HoleSite> Cleared;

        public Hole Hole => _hole != null ? _hole : _hole = GetComponent<Hole>();
        public Collider Bone => bone;
        public bool IsDrilled { get; private set; }
        public float DrillMm { get; private set; }
        public bool IsMeasured { get; private set; }
        public float DepthMm { get; private set; }
        /// <summary>Where the hole's axis leaves the bone, from the last measurement.</summary>
        public Vector3 FarPoint { get; private set; }

        /// <summary>"Ø 3.5" until measured, then "Ø 3.5 · 19.6 mm".</summary>
        public string Label => ToolMessages.HoleLabel(Hole.DiameterMm, IsMeasured, DepthMm);

        public void Load(Collider measuredBone, float reach, GameObject boreVisual)
        {
            bone = measuredBone;
            reachMetres = reach;
            bore = boreVisual;
            ShowBore();
        }

        void OnEnable() => ShowBore();

        /// <summary>The bit this hole needs, from the catalog's screw for its diameter. Zero when the catalog has none.</summary>
        public float BitMm
        {
            get
            {
                var spec = Hole.ExpectedSpec;
                return spec != null ? spec.threadHoleDrillMm : 0f;
            }
        }

        public DrillResult Drill()
        {
            if (IsDrilled) return DrillResult.AlreadyDrilled;
            var bit = BitMm;
            if (bit <= 0f) return DrillResult.NotInCatalog;
            IsDrilled = true;
            DrillMm = bit;
            ShowBore();
            Drilled?.Invoke(this);
            return DrillResult.Drilled;
        }

        /// <summary>Takes a reading. Measuring again replaces the last one.</summary>
        public GaugeResult Measure()
        {
            if (!IsDrilled) return GaugeResult.NotDrilled;
            if (!TryProbe(out var depth, out var far)) return GaugeResult.NoBone;
            IsMeasured = true;
            DepthMm = depth;
            FarPoint = far;
            var seat = Hole.Seat;
            _measuredAt = seat.position;
            _measuredFacing = seat.rotation;
            Measured?.Invoke(this);
            return GaugeResult.Measured;
        }

        /// <summary>The depth under the hole right now, without recording it.</summary>
        public bool TryProbe(out float depthMm, out Vector3 farPoint)
        {
            var seat = Hole.Seat;
            return BoneDepth.TryMeasure(bone, seat.position, seat.forward, reachMetres, out depthMm, out farPoint);
        }

        /// <summary>Back to an untouched hole, for a restart.</summary>
        public void Clear()
        {
            IsDrilled = false;
            DrillMm = 0f;
            IsMeasured = false;
            DepthMm = 0f;
            ShowBore();
            Cleared?.Invoke(this);
        }

        void Update() => CheckReading();

        /// <summary>Drops the reading if the hole has moved since it was taken. Returns true if it did.</summary>
        public bool CheckReading()
        {
            if (!IsMeasured) return false;
            var seat = Hole.Seat;
            if (Vector3.Distance(seat.position, _measuredAt) <= MovedMetres &&
                Quaternion.Angle(seat.rotation, _measuredFacing) <= MovedDegrees) return false;
            IsMeasured = false;
            DepthMm = 0f;
            ReadingLost?.Invoke(this);
            return true;
        }

        void ShowBore()
        {
            if (bore != null) bore.SetActive(IsDrilled);
        }
    }
}
