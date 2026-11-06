using System.Collections.Generic;
using Lab.Core;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.ColourCodedHoles
{
    /// <summary>Puts what the holes and the catalog switcher report on the demo's status line.</summary>
    public class HoleFeedback : MonoBehaviour
    {
        [SerializeField] Hole[] holes = new Hole[0];
        [SerializeField] CatalogSwitcher switcher;
        [SerializeField] StatusLine status;

        readonly List<Hole> _connectedHoles = new List<Hole>();
        CatalogSwitcher _connectedSwitcher;
        StatusLine _connectedStatus;

        void OnEnable() => Connect(holes, switcher, status);
        void OnDisable() => Disconnect();

        public void Connect(IEnumerable<Hole> toHoles, CatalogSwitcher toSwitcher, StatusLine toStatus)
        {
            Disconnect();
            _connectedStatus = toStatus;
            foreach (var hole in toHoles)
            {
                if (hole == null) continue;
                hole.Accepted += OnAccepted;
                hole.Refused += OnRefused;
                _connectedHoles.Add(hole);
            }
            _connectedSwitcher = toSwitcher;
            if (_connectedSwitcher != null) _connectedSwitcher.Switched += OnSwitched;
        }

        public void Disconnect()
        {
            foreach (var hole in _connectedHoles)
            {
                if (hole == null) continue;
                hole.Accepted -= OnAccepted;
                hole.Refused -= OnRefused;
            }
            _connectedHoles.Clear();
            if (_connectedSwitcher != null) _connectedSwitcher.Switched -= OnSwitched;
            _connectedSwitcher = null;
        }

        void OnAccepted(Hole hole, Screw screw) => Show(HoleMessages.Describe(hole, screw, AcceptResult.Accepted));
        void OnRefused(Hole hole, Screw screw, AcceptResult result) => Show(HoleMessages.Describe(hole, screw, result));
        void OnSwitched(ImplantCatalog catalog) => Show(HoleMessages.Switched(catalog));

        void Show(string message)
        {
            if (_connectedStatus != null) _connectedStatus.Show(message);
        }
    }
}
