using System.Collections.Generic;
using Lab.Core;
using UnityEngine;

namespace Lab.W01.HoleTools
{
    /// <summary>
    /// The plate's holes as a group: says on the status line what each tool did, and puts every hole
    /// back to untouched for a restart.
    /// </summary>
    public class HoleSites : MonoBehaviour
    {
        [SerializeField] HoleSiteTarget[] targets = new HoleSiteTarget[0];
        [SerializeField] StatusLine status;

        readonly List<HoleSiteTarget> _connected = new List<HoleSiteTarget>();
        StatusLine _status;

        public IReadOnlyList<HoleSiteTarget> Targets => targets;

        void OnEnable() => Connect(targets, status);
        void OnDisable() => Disconnect();

        public void Connect(IEnumerable<HoleSiteTarget> to, StatusLine toStatus)
        {
            Disconnect();
            _status = toStatus;
            if (to == null) return;
            foreach (var target in to)
            {
                if (target == null) continue;
                target.DrillUsed += OnDrillUsed;
                target.GaugeUsed += OnGaugeUsed;
                target.Site.ReadingLost += OnReadingLost;
                _connected.Add(target);
            }
            targets = _connected.ToArray();
        }

        public void Disconnect()
        {
            foreach (var target in _connected)
            {
                if (target == null) continue;
                target.DrillUsed -= OnDrillUsed;
                target.GaugeUsed -= OnGaugeUsed;
                target.Site.ReadingLost -= OnReadingLost;
            }
            _connected.Clear();
        }

        /// <summary>Every hole untouched again.</summary>
        public void ClearAll()
        {
            foreach (var target in _connected)
                if (target != null) target.Site.Clear();
            Show(ToolMessages.Cleared);
        }

        void OnDrillUsed(HoleSite site, DrillResult result)
        {
            var hole = site.Hole;
            var maker = hole.Catalog != null ? hole.Catalog.manufacturer : "The catalog";
            switch (result)
            {
                case DrillResult.Drilled: Show(ToolMessages.Drilled(hole.DiameterMm, site.DrillMm, maker)); break;
                case DrillResult.AlreadyDrilled: Show(ToolMessages.AlreadyDrilled(hole.DiameterMm)); break;
                default: Show(ToolMessages.NotInCatalog(hole.DiameterMm, maker)); break;
            }
        }

        void OnGaugeUsed(HoleSite site, GaugeResult result)
        {
            switch (result)
            {
                case GaugeResult.Measured: Show(ToolMessages.Measured(site.Hole.DiameterMm, site.DepthMm)); break;
                case GaugeResult.NotDrilled: Show(ToolMessages.MeasureFirst); break;
                default: Show(ToolMessages.NoBone); break;
            }
        }

        void OnReadingLost(HoleSite site) => Show(ToolMessages.ReadingLost);

        void Show(string message)
        {
            if (_status != null) _status.Show(message);
        }
    }
}
