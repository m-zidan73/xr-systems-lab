using System.Collections.Generic;
using UnityEngine;

namespace Lab.Core
{
    /// <summary>The drop targets that are active right now. Targets register themselves while enabled.</summary>
    public static class DropTargets
    {
        static readonly List<IDropTarget> All = new List<IDropTarget>();

        public static IReadOnlyList<IDropTarget> Active => All;

        public static void Register(IDropTarget target)
        {
            if (target != null && !All.Contains(target)) All.Add(target);
        }

        public static void Unregister(IDropTarget target) => All.Remove(target);
    }

    /// <summary>Picks the drop target nearest the cursor on screen, which is where a person aims a drop.</summary>
    public static class DropTargetPicker
    {
        /// <summary>
        /// Index of the screen point nearest the cursor and within maxPixels, or -1.
        /// Points with z at or below zero are behind the camera and never picked.
        /// </summary>
        public static int Closest(IReadOnlyList<Vector3> screenPoints, Vector2 cursor, float maxPixels)
        {
            var best = -1;
            var bestDistance = maxPixels;
            for (var i = 0; i < screenPoints.Count; i++)
            {
                var point = screenPoints[i];
                if (point.z <= 0f) continue;
                var distance = Vector2.Distance(cursor, new Vector2(point.x, point.y));
                if (distance > bestDistance) continue;
                if (best >= 0 && distance == bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            return best;
        }
    }
}
