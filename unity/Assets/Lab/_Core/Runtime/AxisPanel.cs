using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Lab.Core
{
    /// <summary>
    /// A row of slider, label and readout per axis of an <see cref="IAxisAdjustable"/>, plus Reset.
    /// Each slider ends at its axis limit. The keyboard works too: Up and Down pick a row, Left and
    /// Right nudge it by one step (Shift for a fine step), holding a key repeats.
    /// </summary>
    public class AxisPanel : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public Slider slider;
            public Text label;
            public Text value;
            public Image background;
        }

        static readonly Color RowColour = new Color(1f, 1f, 1f, 0f);
        static readonly Color SelectedRowColour = new Color(0.45f, 0.78f, 0.82f, 0.22f);
        static readonly Color ValueColour = new Color(0.55f, 0.85f, 0.9f);
        static readonly Color LimitColour = new Color(1f, 0.68f, 0.3f);
        const float RepeatDelay = 0.35f;
        const float RepeatInterval = 0.06f;

        [Tooltip("The component to adjust. It has to implement IAxisAdjustable.")]
        [SerializeField] MonoBehaviour target;
        [SerializeField] Row[] rows = new Row[0];
        [SerializeField] Button resetButton;
        [Tooltip("One press of Left or Right: millimetres for a move, degrees for a turn.")]
        [SerializeField, Min(0f)] float step = 1f;
        [Tooltip("The same with Shift held.")]
        [SerializeField, Min(0f)] float fineStep = 0.1f;

        IAxisAdjustable _target;
        int _selected;
        int _heldDirection;
        float _nextRepeat;

        public IAxisAdjustable Target => _target;
        public int Selected => _selected;
        public int RowCount => rows.Length;
        public Row RowAt(int index) => rows[index];
        public Button ResetButton => resetButton;
        public float Step => step;
        public float FineStep => fineStep;

        void Start() => Bind();

        void OnDestroy()
        {
            if (_target != null) _target.Changed -= Refresh;
        }

        /// <summary>Connects the sliders to the target set in the Inspector. Called on Start.</summary>
        public void Bind()
        {
            if (_target != null) return;
            if (target is IAxisAdjustable adjustable)
            {
                Bind(adjustable);
                return;
            }
            Debug.LogError($"{name}: the target has to implement IAxisAdjustable.", this);
            enabled = false;
        }

        /// <summary>Connects the sliders to a target given in code.</summary>
        public void Bind(IAxisAdjustable to)
        {
            if (_target != null || to == null) return;
            _target = to;
            if (rows.Length != _target.AxisCount)
                Debug.LogWarning($"{name}: {rows.Length} rows for {_target.AxisCount} axes.", this);

            for (var i = 0; i < rows.Length && i < _target.AxisCount; i++)
            {
                var axis = i;
                var slider = rows[i].slider;
                slider.wholeNumbers = false;
                slider.minValue = _target.AxisMin(axis);
                slider.maxValue = _target.AxisMax(axis);
                // The panel's own arrow keys do the nudging, so the slider never takes focus moves.
                slider.navigation = new Navigation { mode = Navigation.Mode.None };
                slider.SetValueWithoutNotify(_target.GetAxis(axis));
                slider.onValueChanged.AddListener(value =>
                {
                    _selected = axis;
                    _target.SetAxis(axis, value);
                });
                if (rows[i].label != null) rows[i].label.text = _target.AxisLabel(axis);
            }
            if (resetButton != null)
            {
                resetButton.navigation = new Navigation { mode = Navigation.Mode.None };
                resetButton.onClick.AddListener(ResetAll);
            }
            _target.Changed += Refresh;
            Refresh();
        }

        /// <summary>Moves the sliders and readouts to the target's current values.</summary>
        public void Refresh()
        {
            if (_target == null) return;
            for (var i = 0; i < rows.Length && i < _target.AxisCount; i++)
            {
                var value = _target.GetAxis(i);
                var row = rows[i];
                row.slider.SetValueWithoutNotify(value);
                if (row.value != null)
                {
                    row.value.text = AxisFormat.Readout(value, _target.AxisUnit(i));
                    row.value.color = AxisFormat.AtLimit(value, _target.AxisMin(i), _target.AxisMax(i)) ? LimitColour : ValueColour;
                }
                if (row.background != null) row.background.color = i == _selected ? SelectedRowColour : RowColour;
            }
        }

        /// <summary>Picks a row, wrapping from the last back to the first.</summary>
        public void Select(int row)
        {
            if (rows.Length == 0) return;
            _selected = ((row % rows.Length) + rows.Length) % rows.Length;
            Refresh();
        }

        /// <summary>Moves the picked axis one step left (-1) or right (+1).</summary>
        public void Nudge(int direction, bool fine)
        {
            if (_target == null || rows.Length == 0) return;
            var size = fine ? fineStep : step;
            _target.SetAxis(_selected, _target.GetAxis(_selected) + Math.Sign(direction) * size);
        }

        public void ResetAll() => _target?.ResetAxes();

        void Update()
        {
            DropUiFocus();
            var keyboard = Keyboard.current;
            if (keyboard == null || _target == null) return;

            if (keyboard.upArrowKey.wasPressedThisFrame) Select(_selected - 1);
            if (keyboard.downArrowKey.wasPressedThisFrame) Select(_selected + 1);

            var direction = (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
            var fine = keyboard.shiftKey.isPressed;
            if (direction == 0)
            {
                _heldDirection = 0;
                return;
            }
            if (direction != _heldDirection)
            {
                _heldDirection = direction;
                Nudge(direction, fine);
                _nextRepeat = Time.unscaledTime + RepeatDelay;
            }
            else if (Time.unscaledTime >= _nextRepeat)
            {
                Nudge(direction, fine);
                _nextRepeat = Time.unscaledTime + RepeatInterval;
            }
        }

        // A clicked slider or button keeps UI focus, and the UI's own arrow-key handling would then move
        // the slider by a tenth of its range. Taking focus back leaves the arrow keys to this panel.
        void DropUiFocus()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;
            var focused = eventSystem.currentSelectedGameObject;
            if (focused != null && focused.transform.IsChildOf(transform)) eventSystem.SetSelectedGameObject(null);
        }
    }
}
