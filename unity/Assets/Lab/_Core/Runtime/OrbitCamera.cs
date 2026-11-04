using UnityEngine;
using UnityEngine.InputSystem;

namespace Lab.Core
{
    /// <summary>
    /// Orbits a point in front of the camera for inspecting small parts: hold the right
    /// mouse button to rotate, scroll to zoom. The left button stays free for interaction.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OrbitCamera : MonoBehaviour
    {
        [SerializeField] float startDistance = 0.6f;
        [SerializeField] float minDistance = 0.02f;
        [SerializeField] float maxDistance = 3f;
        [SerializeField] float sensitivity = 0.2f;
        [SerializeField] float nearClip = 0.001f;

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

        /// <summary>
        /// Set while running because Unity raises a saved near clip below 0.01 when a scene
        /// loads, and at 0.01 a small part clips away as the camera zooms in close.
        /// </summary>
        public void ApplyNearClip()
        {
            GetComponent<Camera>().nearClipPlane = nearClip;
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

            if (mouse.rightButton.isPressed)
                (_yaw, _pitch) = OrbitMath.Rotate(_yaw, _pitch, mouse.delta.ReadValue(), sensitivity);
            _distance = OrbitMath.Zoom(_distance, mouse.scroll.ReadValue().y, minDistance, maxDistance);

            transform.SetPositionAndRotation(
                OrbitMath.Position(_target, _yaw, _pitch, _distance),
                Quaternion.Euler(_pitch, _yaw, 0f));
        }
    }
}
