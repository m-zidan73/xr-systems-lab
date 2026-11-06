using System;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.ColourCodedHoles
{
    /// <summary>
    /// A plate hole for one screw diameter. It takes its colour from the implant catalog and only
    /// accepts a screw of its own diameter. Colour only shows the match: manufacturers colour-code
    /// differently, so the rule compares sizes, never colours.
    /// </summary>
    [ExecuteAlways]
    public class Hole : MonoBehaviour
    {
        public static readonly Color MissingColour = new Color(0.35f, 0.35f, 0.38f);
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] float diameterMm = 3.5f;
        [SerializeField] ImplantCatalog catalog;
        [Tooltip("Where a seated screw's head goes. Its forward axis points into the hole.")]
        [SerializeField] Transform seat;
        [SerializeField] Renderer ring;

        MaterialPropertyBlock _block;

        public event Action<Hole, Screw> Accepted;
        public event Action<Hole, Screw, AcceptResult> Refused;
        public event Action<Hole, Screw> Vacated;

        public float DiameterMm => diameterMm;
        public ImplantCatalog Catalog => catalog;
        public Renderer Ring => ring;
        public Transform Seat => seat != null ? seat : transform;
        public Screw Occupant { get; private set; }

        /// <summary>The catalog's screw for this diameter, or null when the catalog doesn't ship one.</summary>
        public ScrewSpec ExpectedSpec => catalog != null ? catalog.ForHole(diameterMm) : null;

        void OnEnable() => ApplyColour();
        void OnValidate() => ApplyColour();

        public void SetCatalog(ImplantCatalog newCatalog)
        {
            catalog = newCatalog;
            ApplyColour();
        }

        /// <summary>What TryAccept would answer, without changing anything.</summary>
        public AcceptResult Check(Screw screw)
        {
            if (screw == null) return AcceptResult.NoScrew;
            if (Occupant != null && Occupant != screw) return AcceptResult.Occupied;
            if (ExpectedSpec == null) return AcceptResult.NotInCatalog;
            if (screw.Spec == null || !Mathf.Approximately(screw.Spec.diameterMm, diameterMm)) return AcceptResult.WrongSize;
            return AcceptResult.Accepted;
        }

        /// <summary>Seats the screw if it fits. Every input method (mouse today, VR later) calls this.</summary>
        public AcceptResult TryAccept(Screw screw)
        {
            var result = Check(screw);
            if (result == AcceptResult.Accepted)
            {
                Occupant = screw;
                screw.transform.SetPositionAndRotation(Seat.position, Seat.rotation);
                Accepted?.Invoke(this, screw);
            }
            else
            {
                Refused?.Invoke(this, screw, result);
            }
            return result;
        }

        public void Vacate()
        {
            if (Occupant == null) return;
            var leaving = Occupant;
            Occupant = null;
            Vacated?.Invoke(this, leaving);
        }

        void ApplyColour()
        {
            if (ring == null) return;
            _block ??= new MaterialPropertyBlock();
            ring.GetPropertyBlock(_block);
            var spec = ExpectedSpec;
            _block.SetColor(BaseColorId, spec != null ? spec.trayColor : MissingColour);
            ring.SetPropertyBlock(_block);
        }
    }
}
