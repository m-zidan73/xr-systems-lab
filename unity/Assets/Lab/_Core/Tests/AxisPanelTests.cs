using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lab.Core.Tests
{
    public class AxisFormatTests
    {
        [TestCase(3f, "mm", "+3.0 mm")]
        [TestCase(-2.54f, AxisFormat.Degrees, "−2.5°")]
        [TestCase(0.04f, "mm", "0.0 mm")]
        [TestCase(-0.04f, "mm", "0.0 mm")]
        [TestCase(12.25f, "mm", "+12.3 mm")]
        public void Readout_IsSignedWithOneDecimal(float value, string unit, string expected) =>
            Assert.AreEqual(expected, AxisFormat.Readout(value, unit));

        [TestCase(-4f, true)]
        [TestCase(8f, true)]
        [TestCase(7.9f, false)]
        [TestCase(0f, false)]
        public void AtLimit_OnlyAtEitherEnd(float value, bool expected) =>
            Assert.AreEqual(expected, AxisFormat.AtLimit(value, -4f, 8f));
    }

    public class AxisPanelTests
    {
        GameObject _root;
        FakeAxes _axes;
        AxisPanel _panel;

        [SetUp]
        public void Build()
        {
            _root = new GameObject("Panel Test");
            _axes = new FakeAxes();
            var panelObject = new GameObject("Panel");
            panelObject.transform.SetParent(_root.transform);
            _panel = panelObject.AddComponent<AxisPanel>();

            var wiring = new SerializedObject(_panel);
            var button = new GameObject("Reset", typeof(RectTransform), typeof(Button));
            button.transform.SetParent(panelObject.transform);
            wiring.FindProperty("resetButton").objectReferenceValue = button.GetComponent<Button>();
            var rows = wiring.FindProperty("rows");
            rows.arraySize = 2;
            for (var i = 0; i < 2; i++)
            {
                var row = rows.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("slider").objectReferenceValue = Child<Slider>(panelObject, $"Slider {i}");
                row.FindPropertyRelative("label").objectReferenceValue = Child<Text>(panelObject, $"Label {i}");
                row.FindPropertyRelative("value").objectReferenceValue = Child<Text>(panelObject, $"Value {i}");
                row.FindPropertyRelative("background").objectReferenceValue = Child<Image>(panelObject, $"Row {i}");
            }
            wiring.ApplyModifiedPropertiesWithoutUndo();
            _panel.Bind(_axes);
        }

        [TearDown]
        public void Destroy() => Object.DestroyImmediate(_root);

        static T Child<T>(GameObject parent, string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform);
            return go.AddComponent<T>();
        }

        [Test]
        public void Bind_SlidersEndAtTheAxisLimitsAndShowLabels()
        {
            for (var i = 0; i < 2; i++)
            {
                var row = _panel.RowAt(i);
                Assert.AreEqual(FakeAxes.Mins[i], row.slider.minValue);
                Assert.AreEqual(FakeAxes.Maxes[i], row.slider.maxValue);
                Assert.IsFalse(row.slider.wholeNumbers);
                Assert.AreEqual(FakeAxes.Labels[i], row.label.text);
            }
            Assert.AreEqual("0.0 mm", _panel.RowAt(0).value.text);
        }

        [Test]
        public void MovingASlider_SetsItsAxisAndPicksItsRow()
        {
            _panel.RowAt(1).slider.value = 12.5f;
            Assert.AreEqual(12.5f, _axes.GetAxis(1));
            Assert.AreEqual(1, _panel.Selected);
            Assert.AreEqual("+12.5°", _panel.RowAt(1).value.text);
        }

        [Test]
        public void ChangingTheTarget_MovesSliderAndReadout()
        {
            _axes.SetAxis(0, -3f);
            Assert.AreEqual(-3f, _panel.RowAt(0).slider.value);
            Assert.AreEqual("−3.0 mm", _panel.RowAt(0).value.text);
        }

        [Test]
        public void Nudge_MovesThePickedAxisByAStepOrAFineStep()
        {
            _panel.Select(0);
            _panel.Nudge(+1, fine: false);
            Assert.AreEqual(_panel.Step, _axes.GetAxis(0), 1e-5f);
            _panel.Nudge(-1, fine: true);
            Assert.AreEqual(_panel.Step - _panel.FineStep, _axes.GetAxis(0), 1e-5f);
        }

        [Test]
        public void Nudge_StopsAtTheLimit()
        {
            _panel.Select(0);
            for (var i = 0; i < 20; i++) _panel.Nudge(+1, fine: false);
            Assert.AreEqual(FakeAxes.Maxes[0], _axes.GetAxis(0));
            Assert.AreNotEqual(_panel.RowAt(1).value.color, _panel.RowAt(0).value.color, "The readout at its limit stands out.");
        }

        [Test]
        public void Select_WrapsAround()
        {
            _panel.Select(0);
            _panel.Select(_panel.Selected - 1);
            Assert.AreEqual(1, _panel.Selected);
            _panel.Select(_panel.Selected + 1);
            Assert.AreEqual(0, _panel.Selected);
        }

        [Test]
        public void ResetButton_ResetsTheTarget()
        {
            _axes.SetAxis(1, 10f);
            _panel.ResetButton.onClick.Invoke();
            Assert.AreEqual(1, _axes.ResetCount);
            Assert.AreEqual(0f, _panel.RowAt(1).slider.value);
        }

        [Test]
        public void Sliders_NeverTakeArrowKeyFocusMoves()
        {
            for (var i = 0; i < 2; i++)
                Assert.AreEqual(Navigation.Mode.None, _panel.RowAt(i).slider.navigation.mode);
        }
    }
}
