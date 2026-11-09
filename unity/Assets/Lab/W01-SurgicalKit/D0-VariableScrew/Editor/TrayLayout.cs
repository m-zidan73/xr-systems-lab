using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lab.W01.VariableScrew.EditorTools
{
    /// <summary>Spaces a row of screws by the width of their heads, so wide screws never overlap.</summary>
    public static class TrayLayout
    {
        const float GapMm = 2f;
        const float HeroAboveMm = 14f;

        /// <summary>
        /// Centre of each screw along a row, in world units and in the order given, with the row
        /// centred on zero and neighbouring heads one gap apart.
        /// </summary>
        public static float[] Centres(IReadOnlyList<float> diametersMm)
        {
            var centresMm = new float[diametersMm.Count];
            var edgeMm = 0f;
            for (var i = 0; i < centresMm.Length; i++)
            {
                var widthMm = ScrewUnits.HeadDiameterMm(diametersMm[i]);
                centresMm[i] = edgeMm + widthMm * 0.5f;
                edgeMm += widthMm + GapMm;
            }
            var middleMm = (edgeMm - GapMm) * 0.5f;
            return centresMm.Select(centre => ScrewUnits.MmWorld(centre - middleMm)).ToArray();
        }

        /// <summary>
        /// Stacks a demo's screws from top to bottom and puts the hero above them. Only heights
        /// change, so a scene's materials, sizes and slider positions stay as they were tuned.
        /// </summary>
        public static void ApplyToStack(Transform tray, Transform hero)
        {
            var slots = tray.Cast<Transform>().ToArray();
            var centres = Centres(slots.Select(DiameterOf).ToArray());
            for (var i = 0; i < slots.Length; i++) SetHeight(slots[i], -centres[i]);
            if (hero != null && slots.Length > 0) SetHeight(hero, -centres[0] + ScrewUnits.MmWorld(HeroAboveMm));
        }

        static void SetHeight(Transform screw, float height)
        {
            var position = screw.position;
            position.y = height;
            screw.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(screw);
            EditorUtility.SetDirty(screw);
        }

        static float DiameterOf(Transform slot)
        {
            var screw = slot.GetComponent<Screw>();
            if (screw != null && screw.Spec != null) return screw.Spec.diameterMm;
            var helix = slot.GetComponent<HelixScrew>();
            if (helix != null && helix.Spec != null) return helix.Spec.diameterMm;
            return ScrewUnits.MeshNominalDiameterMm;
        }
    }
}
