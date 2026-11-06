using System;
using UnityEngine;

namespace Lab.Core
{
    /// <summary>
    /// A part the mouse (and later a VR hand) can pick up, move and put down. It remembers where
    /// it came from and goes back there when a drop is refused or misses.
    /// </summary>
    public class Grabbable : MonoBehaviour
    {
        static readonly int HighlightId = Shader.PropertyToID("_Highlight");
        const float ReturnSharpness = 18f;
        const float ArrivedMetres = 0.0002f;

        [SerializeField] bool animateReturn = true;

        Vector3 _homePosition;
        Quaternion _homeRotation;
        bool _hasHome;
        bool _returning;
        MaterialPropertyBlock _block;

        public event Action<Grabbable> PickedUp;
        public event Action<Grabbable, bool> Dropped;

        public bool IsHeld { get; private set; }

        void Awake() => SetHome();

        /// <summary>Remembers the current pose as the place to return to.</summary>
        public void SetHome()
        {
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
            _hasHome = true;
        }

        public void PickUp()
        {
            if (!_hasHome) SetHome();
            _returning = false;
            IsHeld = true;
            PickedUp?.Invoke(this);
        }

        public void MoveTo(Vector3 position) => transform.position = position;

        public void RotateHeld(float degrees) => transform.Rotate(Vector3.up, degrees, Space.World);

        /// <summary>Lets go. An accepted drop stays where the target put it; anything else goes home.</summary>
        public void Drop(bool accepted)
        {
            IsHeld = false;
            if (!accepted) ReturnHome();
            Dropped?.Invoke(this, accepted);
        }

        public void ReturnHome(bool instant = false)
        {
            // Outside Play, Awake hasn't run, so no home is known yet. Staying put beats jumping to the origin.
            if (!_hasHome) SetHome();
            if (instant || !animateReturn || !Application.isPlaying)
            {
                transform.SetPositionAndRotation(_homePosition, _homeRotation);
                _returning = false;
                return;
            }
            _returning = true;
        }

        void Update()
        {
            if (!_returning) return;
            var t = 1f - Mathf.Exp(-ReturnSharpness * Time.deltaTime);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, _homePosition, t),
                Quaternion.Slerp(transform.rotation, _homeRotation, t));
            if (Vector3.Distance(transform.position, _homePosition) < ArrivedMetres) ReturnHome(true);
        }

        /// <summary>
        /// Writes _Highlight into each renderer's property block. Shaders that declare it brighten;
        /// other values already in the block, such as a screw's colour, are kept.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            _block ??= new MaterialPropertyBlock();
            foreach (var target in GetComponentsInChildren<Renderer>())
            {
                target.GetPropertyBlock(_block);
                _block.SetFloat(HighlightId, highlighted ? 1f : 0f);
                target.SetPropertyBlock(_block);
            }
        }
    }
}
