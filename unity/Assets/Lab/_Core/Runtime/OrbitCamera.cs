using UnityEngine;
using UnityEngine.InputSystem;

namespace Lab.Core
{
    /// <summary>
    /// Orbits a point in front of the camera for inspecting small parts: hold the right
    /// mouse button to rotate, the middle button or Shift + right to pan, scroll to zoom.
    /// The left button stays free for interaction.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OrbitCamera : MonoBehaviour
    {
        [SerializeField] float startDistance = 0.6f;
        [SerializeField] float minDistance = 0.02f;
        [SerializeField] float maxDistance = 3f;
        [SerializeField] float sensitivity = 0.2f;
        [SerializeField] float nearClip = 0.001f;

        Camera _camera;
        Vector3 _target;
        float _yaw;
        float _pitch;
        float _distance;

        public float StartDistance
        {
            get => startDistance;
            set => startDistance = value;
        }

        public float NearClip => nearClip;

        /// <summary>Turned off while something is held, so the scroll wheel turns the part instead.</summary>
        public bool ZoomEnabled { get; set; } = true;

        /// <summary>
        /// Set while running because Unity raises a saved near clip below 0.01 when a scene
        /// loads, and at 0.01 a small part clips away as the camera zooms in close.
        /// </summary>
        public void ApplyNearClip()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            _camera.nearClipPlane = nearClip;
        }

        void Awake()
        {
            ApplyNearClip();
            _distance = startDistance;
            _target = transform.position + transform.forward * startDistance;
            var euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = Mathf.DeltaAngle(0f, euler.x);
        }

        void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var mouseDelta = mouse.delta.ReadValue();
            var shiftHeld = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            if (mouse.middleButton.isPressed || (mouse.rightButton.isPressed && shiftHeld))
            {
                var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                _target = OrbitMath.Pan(_target, rotation, mouseDelta, _distance, _camera.fieldOfView, _camera.pixelHeight);
            }
            else if (mouse.rightButton.isPressed)
            {
                (_yaw, _pitch) = OrbitMath.Rotate(_yaw, _pitch, mouseDelta, sensitivity);
            }
            if (ZoomEnabled)
                _distance = OrbitMath.Zoom(_distance, mouse.scroll.ReadValue().y, minDistance, maxDistance);

            transform.SetPositionAndRotation(
                OrbitMath.Position(_target, _yaw, _pitch, _distance),
                Quaternion.Euler(_pitch, _yaw, 0f));
        }
    }
}
