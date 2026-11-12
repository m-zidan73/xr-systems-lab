using UnityEngine;

namespace Lab.W01.AdjustablePlate
{
    /// <summary>
    /// A see-through copy of the plate where it belongs, which turns green once the plate is there. It has
    /// to share the plate's parent, because it takes the adjuster's target pose as its own local pose.
    /// It colours every renderer under it, so it can sit on the root of a copy of the plate model.
    /// </summary>
    public class PlateTargetGhost : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] PlateAdjuster adjuster;
        [SerializeField] Color waitingColour = new Color(0.2f, 0.6f, 1f, 0.7f);
        [SerializeField] Color placedColour = new Color(0.25f, 1f, 0.45f, 0.75f);

        PlateAdjuster _connected;
        MaterialPropertyBlock _block;

        public Color CurrentColour { get; private set; }
        public Color WaitingColour => waitingColour;
        public Color PlacedColour => placedColour;

        void OnEnable() => Connect(adjuster);
        void OnDisable() => Disconnect();

        public void Connect(PlateAdjuster to)
        {
            Disconnect();
            _connected = to;
            if (_connected == null) return;
            _connected.PlacedChanged += Show;
            var (position, rotation) = _connected.TargetPose;
            transform.SetLocalPositionAndRotation(position, rotation);
            Show(_connected.IsPlaced);
        }

        public void Disconnect()
        {
            if (_connected != null) _connected.PlacedChanged -= Show;
            _connected = null;
        }

        void Show(bool placed)
        {
            CurrentColour = placed ? placedColour : waitingColour;
            _block ??= new MaterialPropertyBlock();
            foreach (var target in GetComponentsInChildren<Renderer>())
            {
                target.GetPropertyBlock(_block);
                _block.SetColor(ColorId, CurrentColour);
                target.SetPropertyBlock(_block);
            }
        }
    }
}
