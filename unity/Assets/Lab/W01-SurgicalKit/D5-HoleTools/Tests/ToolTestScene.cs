using System.Collections.Generic;
using Lab.Core;
using Lab.W01.ColourCodedHoles;
using Lab.W01.ColourCodedHoles.Tests;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.HoleTools.Tests
{
    /// <summary>
    /// A hole looking straight down at a slab of bone, with a drill and a gauge. The hole sits 3 mm above
    /// a slab 12 mm thick, so the depth from the hole to the far side is 15 mm.
    /// </summary>
    public sealed class ToolTestScene
    {
        public const float GapMm = 3f;
        public const float SlabMm = 12f;
        public const float DepthMm = GapMm + SlabMm;

        readonly HoleTestKit _kit = new HoleTestKit();
        readonly List<Object> _made = new List<Object>();

        public readonly ScrewSpec Spec35;
        public readonly ImplantCatalog Catalog;
        public readonly BoxCollider Bone;
        public readonly Hole Hole;
        public readonly HoleSite Site;
        public readonly HoleSiteTarget Target;
        public readonly GameObject Bore;
        public readonly HoleTool Drill;
        public readonly HoleTool Gauge;

        public ToolTestScene()
        {
            Spec35 = _kit.Spec(3.5f, Color.blue);
            Spec35.threadHoleDrillMm = 2.5f;
            Catalog = _kit.Catalog("Test Maker", Spec35);

            var slab = Track(new GameObject("Bone"));
            slab.transform.position = new Vector3(0f, -(GapMm + SlabMm * 0.5f) * 0.001f, 0f);
            Bone = slab.AddComponent<BoxCollider>();
            Bone.size = new Vector3(0.05f, SlabMm * 0.001f, 0.05f);

            Hole = _kit.Hole(3.5f, Catalog, Vector3.zero);
            // The kit's seat sits 2.5 mm above the hole. Here the hole itself is the seat.
            Hole.Seat.localPosition = Vector3.zero;
            Bore = Track(new GameObject("Bore"));
            Bore.transform.SetParent(Hole.transform, false);
            Site = Hole.gameObject.AddComponent<HoleSite>();
            Site.Load(Bone, 0.08f, Bore);
            Target = Hole.gameObject.AddComponent<HoleSiteTarget>();

            Drill = Tool("Drill", ToolKind.Drill);
            Gauge = Tool("Gauge", ToolKind.Gauge);
            Physics.SyncTransforms();
        }

        public T Track<T>(T thing) where T : Object
        {
            _made.Add(thing);
            return thing;
        }

        HoleTool Tool(string name, ToolKind kind)
        {
            var go = Track(new GameObject(name));
            go.transform.position = new Vector3(0.1f, 0f, 0f);
            go.AddComponent<Grabbable>().SetHome();
            var tool = go.AddComponent<HoleTool>();
            tool.Load(kind, 0.5f);
            return tool;
        }

        public Screw Screw(ScrewSpec spec) => _kit.Screw(spec, new Vector3(0.2f, 0f, 0f));
        public Grabbable Grabbable(Screw screw) => _kit.Grabbable(screw);
        public ScrewSpec OtherSpec(float diameterMm) => _kit.Spec(diameterMm, Color.red);

        /// <summary>Moves the hole, as moving the plate would, and tells physics.</summary>
        public void MoveHole(Vector3 by)
        {
            Hole.transform.position += by;
            Physics.SyncTransforms();
        }

        public void Dispose()
        {
            foreach (var thing in _made)
                if (thing != null) Object.DestroyImmediate(thing);
            _made.Clear();
            _kit.Dispose();
        }
    }
}
