using UnityEngine;

namespace Lab.Core
{
    /// <summary>A place a Grabbable can be put down. The target decides whether to take it.</summary>
    public interface IDropTarget
    {
        Vector3 DropPoint { get; }

        /// <summary>Returns true and places the item if it is accepted; leaves it alone otherwise.</summary>
        bool TryDrop(Grabbable item);
    }
}
