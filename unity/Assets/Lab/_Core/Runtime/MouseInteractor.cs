using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lab.Core
{
    /// <summary>
    /// Mouse controls for handling parts: hover to highlight, hold the left button to pick up and
    /// drag, scroll while holding to turn the part, let go near a drop target to place it. A refused
    /// or missed drop sends the part home. Systems never see the mouse; they only answer TryDrop.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MouseInteractor : MonoBehaviour
    {
        [Tooltip("How close to a drop target, in screen pixels, a drop has to land.")]
        [SerializeField] float snapPixels = 48f;
        [SerializeField] float turnStepDeg = 15f;
        [SerializeField] float maxReachMetres = 5f;

        readonly List<Vector3> _screenPoints = new List<Vector3>();
        Camera _camera;
        OrbitCamera _orbit;
        Grabbable _hovered;
        Grabbable _held;
        Plane _dragPlane;
        Vector3 _grabOffset;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _orbit = GetComponent<OrbitCamera>();
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var cursor = mouse.position.ReadValue();
            var ray = _camera.ScreenPointToRay(cursor);

            if (_held == null)
            {
                Hover(ray);
                if (_hovered != null && mouse.leftButton.wasPressedThisFrame) Grab(ray);
                return;
            }

            if (_dragPlane.Raycast(ray, out var enter)) _held.MoveTo(ray.GetPoint(enter) + _grabOffset);
            var scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f) _held.RotateHeld(Mathf.Sign(scroll) * turnStepDeg);
            if (mouse.leftButton.wasReleasedThisFrame) Release(cursor);
        }

        void Hover(Ray ray)
        {
            Grabbable found = null;
            if (Physics.Raycast(ray, out var hit, maxReachMetres, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                found = hit.collider.GetComponentInParent<Grabbable>();
            if (found == _hovered) return;
            if (_hovered != null) _hovered.SetHighlighted(false);
            _hovered = found;
            if (_hovered != null) _hovered.SetHighlighted(true);
        }

        void Grab(Ray ray)
        {
            _held = _hovered;
            _held.PickUp();
            // Drag on a plane facing the camera through the part, so it stays at the depth it was picked from.
            _dragPlane = new Plane(-_camera.transform.forward, _held.transform.position);
            _grabOffset = _dragPlane.Raycast(ray, out var enter) ? _held.transform.position - ray.GetPoint(enter) : Vector3.zero;
            if (_orbit != null) _orbit.ZoomEnabled = false;
        }

        void Release(Vector2 cursor)
        {
            var targets = DropTargets.Active;
            _screenPoints.Clear();
            foreach (var target in targets) _screenPoints.Add(_camera.WorldToScreenPoint(target.DropPoint));
            var index = DropTargetPicker.Closest(_screenPoints, cursor, snapPixels);

            var item = _held;
            _held = null;
            var accepted = index >= 0 && targets[index].TryDrop(item);
            item.Drop(accepted);
            if (_orbit != null) _orbit.ZoomEnabled = true;
        }
    }
}
