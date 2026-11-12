using Lab.Core;
using UnityEngine;

namespace Lab.W01.AdjustablePlate
{
    /// <summary>
    /// Shows what the adjuster reports: limits reached on the status line, and a banner across the view
    /// while the plate is in place, so finishing the step can't be missed.
    /// </summary>
    public class PlateFeedback : MonoBehaviour
    {
        [SerializeField] PlateAdjuster adjuster;
        [SerializeField] StatusLine status;
        [SerializeField] Banner banner;

        PlateAdjuster _connected;
        StatusLine _connectedStatus;
        Banner _connectedBanner;

        void OnEnable() => Connect(adjuster, status, banner);
        void OnDisable() => Disconnect();

        public void Connect(PlateAdjuster to, StatusLine toStatus, Banner toBanner)
        {
            Disconnect();
            _connected = to;
            _connectedStatus = toStatus;
            _connectedBanner = toBanner;
            if (_connected == null) return;
            _connected.AxisChanged += OnAxisChanged;
            _connected.LimitReached += ShowLimit;
            _connected.PlacedChanged += OnPlacedChanged;
        }

        public void Disconnect()
        {
            if (_connected != null)
            {
                _connected.AxisChanged -= OnAxisChanged;
                _connected.LimitReached -= ShowLimit;
                _connected.PlacedChanged -= OnPlacedChanged;
            }
            _connected = null;
        }

        void OnAxisChanged(PlateAxis axis)
        {
            if (_connected.AtLimit(axis)) ShowLimit(axis);
        }

        void ShowLimit(PlateAxis axis) =>
            Show(PlateMessages.AtLimit(_connected.Settings(axis).label, _connected.Get(axis), axis));

        void OnPlacedChanged(bool placed)
        {
            Show(placed
                ? PlateMessages.Placed(_connected.PositionToleranceMm, _connected.RotationToleranceDeg)
                : PlateMessages.LeftPlace);
            if (_connectedBanner == null) return;
            if (placed) _connectedBanner.Show(PlateMessages.PlacedTitle, PlateMessages.PlacedNext);
            else _connectedBanner.Hide();
        }

        void Show(string message)
        {
            if (_connectedStatus != null) _connectedStatus.Show(message);
        }
    }

    public static class PlateMessages
    {
        public const string PlacedTitle = "Plate in place";
        public const string PlacedNext = "This step is done. In the full procedure, screws go into the plate's holes next.";
        public const string LeftPlace = "Out of place again. Line the plate up with the outline.";

        public static string Placed(float toleranceMm, float toleranceDeg) =>
            $"In place: every move is within {toleranceMm:0.#} mm and every turn within {toleranceDeg:0.#}{AxisFormat.Degrees} of the target.";

        public static string AtLimit(string label, float value, PlateAxis axis) =>
            $"{label} stops at its limit, {AxisFormat.Readout(value, axis.Unit())}.";
    }
}
