using Lab.Core;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.ColourCodedHoles
{
    /// <summary>Lets the lab's mouse controls put screws in a hole. The hole's own rule decides.</summary>
    [RequireComponent(typeof(Hole))]
    public class HoleDropTarget : MonoBehaviour, IDropTarget
    {
        Hole _hole;

        Hole Hole => _hole != null ? _hole : _hole = GetComponent<Hole>();

        public Vector3 DropPoint => Hole.Seat.position;

        void OnEnable() => DropTargets.Register(this);
        void OnDisable() => DropTargets.Unregister(this);

        public bool TryDrop(Grabbable item)
        {
            var screw = item != null ? item.GetComponent<Screw>() : null;
            if (Hole.TryAccept(screw) != AcceptResult.Accepted) return false;
            item.PickedUp += OnOccupantPickedUp;
            return true;
        }

        void OnOccupantPickedUp(Grabbable item)
        {
            item.PickedUp -= OnOccupantPickedUp;
            // The hole may have been emptied and refilled since, for example by a catalog switch.
            if (Hole.Occupant == item.GetComponent<Screw>()) Hole.Vacate();
        }
    }
}
