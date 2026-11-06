using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Lab.Core
{
    /// <summary>Calls whatever is wired to it when a key is pressed, so demos get shortcuts without systems reading the keyboard.</summary>
    public class KeyAction : MonoBehaviour
    {
        [SerializeField] Key key = Key.Space;
        [SerializeField] UnityEvent onPressed = new UnityEvent();

        public Key Key => key;
        public UnityEvent OnPressed => onPressed;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[key].wasPressedThisFrame) onPressed.Invoke();
        }
    }
}
