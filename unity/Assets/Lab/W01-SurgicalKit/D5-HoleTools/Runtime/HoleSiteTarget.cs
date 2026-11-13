using System;
using Lab.Core;
using Lab.W01.ColourCodedHoles;
using UnityEngine;

namespace Lab.W01.HoleTools
{
    /// <summary>
    /// Lets the lab's mouse controls use a tool on a hole as well as put a screw in it. A hole offers one
    /// place to drop things, so this takes the place of day 1's drop target: tools are handled here and
    /// screws are passed on to day 1's rule unchanged.
    /// </summary>
    [RequireComponent(typeof(HoleSite))]
    public class HoleSiteTarget : HoleDropTarget
    {
        HoleSite _site;

        /// <summary>Raised when a drill was used on the hole, with what came of it.</summary>
        public event Action<HoleSite, DrillResult> DrillUsed;
        /// <summary>Raised when a gauge was used on the hole, with what came of it.</summary>
        public event Action<HoleSite, GaugeResult> GaugeUsed;

        public HoleSite Site => _site != null ? _site : _site = GetComponent<HoleSite>();

        public override bool TryDrop(Grabbable item)
        {
            var tool = item != null ? item.GetComponent<HoleTool>() : null;
            if (tool == null) return base.TryDrop(item);
            return Use(tool);
        }

        /// <summary>Uses the tool on this hole. Returns true when the tool did its job and stays in the hole for a moment.</summary>
        public bool Use(HoleTool tool)
        {
            bool worked;
            if (tool.Kind == ToolKind.Drill)
            {
                var result = Site.Drill();
                worked = result == DrillResult.Drilled;
                DrillUsed?.Invoke(Site, result);
            }
            else
            {
                var result = Site.Measure();
                worked = result == GaugeResult.Measured;
                GaugeUsed?.Invoke(Site, result);
            }
            if (worked) tool.WorkAt(Site.Hole.Seat);
            return worked;
        }
    }
}
