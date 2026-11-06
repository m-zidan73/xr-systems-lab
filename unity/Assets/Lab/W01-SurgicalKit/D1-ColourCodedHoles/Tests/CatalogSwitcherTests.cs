using Lab.Core;
using Lab.W01.VariableScrew;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lab.W01.ColourCodedHoles.Tests
{
    public class CatalogSwitcherTests
    {
        const float Tolerance = 1e-5f;

        HoleTestKit _kit;
        ImplantCatalog _catalogA;
        ImplantCatalog _catalogB;
        Hole _hole35;
        Hole _hole40;
        Screw[] _tray;
        CatalogSwitcher _switcher;

        [SetUp]
        public void SetUp()
        {
            _kit = new HoleTestKit();
            _catalogA = _kit.Catalog("A", _kit.Spec(2.7f, Color.grey), _kit.Spec(3.5f, Color.blue), _kit.Spec(4.0f, Color.green));
            _catalogB = _kit.Catalog("B", _kit.Spec(2.7f, Color.red), _kit.Spec(3.5f, Color.yellow));
            _hole35 = _kit.Hole(3.5f, null, new Vector3(0f, 0.05f, 0f));
            _hole40 = _kit.Hole(4.0f, null, new Vector3(0.02f, 0.05f, 0f));
            _tray = new Screw[3];
            for (var i = 0; i < _tray.Length; i++)
            {
                _tray[i] = _kit.Screw(null, new Vector3(i * 0.01f, 0f, -0.05f));
                _kit.Grabbable(_tray[i]);
            }

            var go = new GameObject("Switcher");
            _switcher = go.AddComponent<CatalogSwitcher>();
            var wiring = new SerializedObject(_switcher);
            SetArray(wiring.FindProperty("catalogs"), _catalogA, _catalogB);
            SetArray(wiring.FindProperty("holes"), _hole35, _hole40);
            SetArray(wiring.FindProperty("trayScrews"), _tray);
            wiring.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_switcher.gameObject);
            _kit.Dispose();
        }

        static void SetArray(SerializedProperty property, params Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        [Test]
        public void Show_ColoursTheHolesAndFillsTheTray()
        {
            _switcher.Show(0);
            Assert.AreSame(_catalogA, _switcher.Current);
            Assert.AreSame(_catalogA, _hole35.Catalog);
            Assert.AreSame(_catalogA, _hole40.Catalog);
            for (var i = 0; i < _tray.Length; i++)
            {
                Assert.AreSame(_catalogA.screws[i], _tray[i].Spec);
                Assert.IsTrue(_tray[i].gameObject.activeSelf);
            }
        }

        [Test]
        public void Show_SmallerCatalog_HidesTheSpareTraySlots()
        {
            _switcher.Show(1);
            Assert.AreSame(_catalogB.screws[0], _tray[0].Spec);
            Assert.IsFalse(_tray[2].gameObject.activeSelf);
            HoleTestKit.AssertRingColour(Hole.MissingColour, _hole40);
        }

        [Test]
        public void Next_CyclesThroughTheCatalogs()
        {
            _switcher.Show(0);
            _switcher.Next();
            Assert.AreSame(_catalogB, _switcher.Current);
            _switcher.Next();
            Assert.AreSame(_catalogA, _switcher.Current);
        }

        [Test]
        public void Switching_EmptiesTheHolesAndSendsScrewsHome()
        {
            _switcher.Show(0);
            var home = _tray[1].transform.position;
            Assert.AreEqual(AcceptResult.Accepted, _hole35.TryAccept(_tray[1]));
            _switcher.Show(1);
            Assert.IsNull(_hole35.Occupant);
            Assert.AreEqual(0f, Vector3.Distance(home, _tray[1].transform.position), Tolerance);
        }

        [Test]
        public void Switched_ReportsTheNewCatalog()
        {
            ImplantCatalog reported = null;
            _switcher.Switched += catalog => reported = catalog;
            _switcher.Show(1);
            Assert.AreSame(_catalogB, reported);
        }
    }
}
