using Lab.Core;
using UnityEngine;

namespace Lab.W01.HoleTools
{
    public enum ToolKind { Drill, Gauge }

    /// <summary>
    /// A drill or a depth gauge that is carried to a hole. When it is used it stays in the hole for a
    /// moment, pointing along the hole, and then goes back to where it was picked up.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class HoleTool : MonoBehaviour
    {
        [SerializeField] ToolKind kind;
        [Tooltip("Seconds the tool stays in the hole after it is used.")]
        [SerializeField, Min(0f)] float workSeconds = 0.7f;
        [Tooltip("Turns per second while a drill is working. A gauge doesn't turn.")]
        [SerializeField] float spinTurnsPerSecond = 6f;

        Grabbable _grabbable;
        float _remaining;

        public ToolKind Kind => kind;
        public bool IsWorking => _remaining > 0f;

        Grabbable Grabbable => _grabbable != null ? _grabbable : _grabbable = GetComponent<Grabbable>();

        public void Load(ToolKind toolKind, float seconds)
        {
            kind = toolKind;
            workSeconds = seconds;
        }

        /// <summary>Puts the tool's tip in the hole, pointing along it, and starts the wait before it goes home.</summary>
        public void WorkAt(Transform seat)
        {
            transform.SetPositionAndRotation(seat.position, seat.rotation);
            _remaining = workSeconds;
            if (_remaining <= 0f) Finish();
        }

        /// <summary>Sends the tool home now.</summary>
        public void Finish()
        {
            _remaining = 0f;
            Grabbable.ReturnHome();
        }

        void Update()
        {
            if (_remaining <= 0f) return;
            // Picked up again mid-job: the hand has it now.
            if (Grabbable.IsHeld) { _remaining = 0f; return; }
            if (kind == ToolKind.Drill) transform.Rotate(Vector3.forward, spinTurnsPerSecond * 360f * Time.deltaTime, Space.Self);
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) Finish();
        }
    }
}
