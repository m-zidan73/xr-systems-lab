using System;
using System.Collections.Generic;
using System.Globalization;
using Lab.Core;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.SizingStation
{
    /// <summary>
    /// Chooses a size for one screw from a catalog: a diameter the catalog has, and a length that
    /// diameter ships in. It only ever lands on real sizes. Panels, keys and later VR controls call
    /// its methods, so it never reads input. It also reads as two axes, for any control that drives
    /// an <see cref="IAxisAdjustable"/>.
    /// </summary>
    public class ScrewSizer : MonoBehaviour, IAxisAdjustable
    {
        public const int DiameterAxis = 0;
        public const int LengthAxis = 1;

        static readonly ScrewSpec[] NoSpecs = new ScrewSpec[0];
        static readonly float[] NoLengths = new float[0];

        [SerializeField] ImplantCatalog[] catalogs = new ImplantCatalog[0];
        [Tooltip("The screw being sized. The station swaps this when it starts a new screw.")]
        [SerializeField] Screw screw;
        [SerializeField] float startDiameterMm = 3.5f;
        [SerializeField] float startLengthMm = 20f;

        int _catalog;
        int _diameter;
        float _lengthMm;
        bool _ready;

        /// <summary>Raised after the size, the catalog or the screw being sized changes.</summary>
        public event Action Changed;
        public event Action<ImplantCatalog> CatalogChanged;

        public Screw Screw => screw;

        public ImplantCatalog Catalog
        {
            get
            {
                Ready();
                return _catalog >= 0 && _catalog < catalogs.Length ? catalogs[_catalog] : null;
            }
        }

        /// <summary>The diameters the current catalog has, in its own order.</summary>
        public IReadOnlyList<ScrewSpec> Specs
        {
            get
            {
                var catalog = Catalog;
                return catalog != null && catalog.screws != null ? catalog.screws : NoSpecs;
            }
        }

        public ScrewSpec Spec
        {
            get
            {
                var specs = Specs;
                return _diameter >= 0 && _diameter < specs.Count ? specs[_diameter] : null;
            }
        }

        /// <summary>The lengths the chosen diameter ships in.</summary>
        public IReadOnlyList<float> Lengths
        {
            get
            {
                var spec = Spec;
                return spec != null && spec.availableLengthsMm != null ? spec.availableLengthsMm : NoLengths;
            }
        }

        public int DiameterIndex { get { Ready(); return _diameter; } }
        public float LengthMm { get { Ready(); return _lengthMm; } }
        public int LengthIndex => Spec != null ? Spec.NearestIndex(LengthMm) : 0;

        /// <summary>"Ø 3.5 mm × 22 mm".</summary>
        public string Label
        {
            get
            {
                var spec = Spec;
                return spec == null
                    ? "no size"
                    : string.Format(CultureInfo.InvariantCulture, "Ø {0:0.#} mm × {1:0.#} mm", spec.diameterMm, LengthMm);
            }
        }

        void OnEnable() => Ready();

        /// <summary>Sets the catalogs and the screw in code, then starts from the start size.</summary>
        public void Load(IEnumerable<ImplantCatalog> from, Screw toSize)
        {
            catalogs = from != null ? new List<ImplantCatalog>(from).ToArray() : new ImplantCatalog[0];
            screw = toSize;
            _catalog = 0;
            _ready = false;
            Ready();
            Changed?.Invoke();
        }

        void Ready()
        {
            if (_ready) return;
            _ready = true;
            ToStartSize();
            Push(false);
        }

        void ToStartSize()
        {
            _diameter = NearestDiameter(startDiameterMm);
            var spec = Spec;
            _lengthMm = spec != null ? spec.NearestAvailable(startLengthMm) : startLengthMm;
        }

        /// <summary>Picks a diameter by its place in the catalog. The length moves to the nearest one that diameter ships in.</summary>
        public void SelectDiameter(int index)
        {
            Ready();
            var specs = Specs;
            if (specs.Count == 0) return;
            _diameter = Mathf.Clamp(index, 0, specs.Count - 1);
            var spec = Spec;
            if (spec != null) _lengthMm = spec.NearestAvailable(_lengthMm);
            Push(true);
            Changed?.Invoke();
        }

        /// <summary>Picks a length by its place in the chosen diameter's list.</summary>
        public void SelectLength(int index)
        {
            Ready();
            var lengths = Lengths;
            if (lengths.Count == 0) return;
            SetLengthMm(lengths[Mathf.Clamp(index, 0, lengths.Count - 1)]);
        }

        /// <summary>Asks for a length in millimetres and gets the nearest one that ships.</summary>
        public void SetLengthMm(float requestedMm)
        {
            Ready();
            var spec = Spec;
            if (spec == null) return;
            _lengthMm = spec.NearestAvailable(requestedMm);
            Push(true);
            Changed?.Invoke();
        }

        /// <summary>One diameter up (+1) or down (-1). Stops at the ends.</summary>
        public void StepDiameter(int by) => SelectDiameter(DiameterIndex + by);

        /// <summary>One shipped length up (+1) or down (-1). Stops at the ends.</summary>
        public void StepLength(int by) => SelectLength(LengthIndex + by);

        /// <summary>Moves to the next catalog and keeps the size as close as that catalog allows.</summary>
        public void NextCatalog()
        {
            Ready();
            if (catalogs.Length == 0) return;
            var diameterMm = Spec != null ? Spec.diameterMm : startDiameterMm;
            _catalog = (_catalog + 1) % catalogs.Length;
            _diameter = NearestDiameter(diameterMm);
            var spec = Spec;
            if (spec != null) _lengthMm = spec.NearestAvailable(_lengthMm);
            Push(true);
            CatalogChanged?.Invoke(Catalog);
            Changed?.Invoke();
        }

        /// <summary>Points the sizer at another screw and gives it the current size.</summary>
        public void Attach(Screw next)
        {
            Ready();
            screw = next;
            Push(false);
            Changed?.Invoke();
        }

        int NearestDiameter(float diameterMm)
        {
            var specs = Specs;
            var best = 0;
            var bestDelta = float.MaxValue;
            for (var i = 0; i < specs.Count; i++)
            {
                if (specs[i] == null) continue;
                var delta = Mathf.Abs(specs[i].diameterMm - diameterMm);
                if (delta >= bestDelta) continue;
                best = i;
                bestDelta = delta;
            }
            return best;
        }

        void Push(bool smooth)
        {
            var spec = Spec;
            if (screw == null || spec == null) return;
            if (smooth) screw.SetSmooth(spec, _lengthMm);
            else screw.Set(spec, _lengthMm);
        }

        // The same choice, read as two axes.

        public int AxisCount => 2;
        public string AxisLabel(int axis) => axis == DiameterAxis ? "Diameter" : "Length";
        public string AxisUnit(int axis) => "mm";

        public float AxisMin(int axis)
        {
            if (axis == LengthAxis) return Lengths.Count > 0 ? Lengths[0] : 0f;
            var min = float.MaxValue;
            foreach (var spec in Specs)
                if (spec != null) min = Mathf.Min(min, spec.diameterMm);
            return min == float.MaxValue ? 0f : min;
        }

        public float AxisMax(int axis)
        {
            if (axis == LengthAxis) return Lengths.Count > 0 ? Lengths[Lengths.Count - 1] : 0f;
            var max = 0f;
            foreach (var spec in Specs)
                if (spec != null) max = Mathf.Max(max, spec.diameterMm);
            return max;
        }

        public float GetAxis(int axis)
        {
            if (axis == LengthAxis) return LengthMm;
            return Spec != null ? Spec.diameterMm : 0f;
        }

        public float SetAxis(int axis, float value)
        {
            if (axis == LengthAxis) SetLengthMm(value);
            else SelectDiameter(NearestDiameter(value));
            return GetAxis(axis);
        }

        public void ResetAxes()
        {
            Ready();
            ToStartSize();
            Push(true);
            Changed?.Invoke();
        }
    }
}
