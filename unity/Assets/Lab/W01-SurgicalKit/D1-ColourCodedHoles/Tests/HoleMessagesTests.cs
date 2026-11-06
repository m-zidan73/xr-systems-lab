using System.Globalization;
using Lab.Core;
using NUnit.Framework;
using UnityEngine;

namespace Lab.W01.ColourCodedHoles.Tests
{
    public class HoleMessagesTests
    {
        CultureInfo _culture;
        HoleTestKit _kit;
        Hole _hole;

        [SetUp]
        public void SetUp()
        {
            _culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            _kit = new HoleTestKit();
            _hole = _kit.Hole(3.5f, _kit.Catalog("Chromatic Ortho", _kit.Spec(3.5f, Color.blue)), Vector3.zero);
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = _culture;
            _kit.Dispose();
        }

        [Test]
        public void Accepted_NamesTheScrewAndTheHole()
        {
            var screw = _kit.Screw(_kit.Spec(3.5f, Color.blue), Vector3.zero);
            Assert.AreEqual("Seated: Ø 3.5 mm × 20 mm in the 3.5 mm hole.", HoleMessages.Describe(_hole, screw, AcceptResult.Accepted));
        }

        [Test]
        public void WrongSize_GivesBothSizes()
        {
            var screw = _kit.Screw(_kit.Spec(4.0f, Color.green), Vector3.zero);
            Assert.AreEqual("Refused: a 4 mm screw doesn't fit the 3.5 mm hole.", HoleMessages.Describe(_hole, screw, AcceptResult.WrongSize));
        }

        [Test]
        public void NotInCatalog_NamesTheManufacturer()
        {
            var screw = _kit.Screw(_kit.Spec(3.5f, Color.blue), Vector3.zero);
            _hole.SetCatalog(_kit.Catalog("Northline Fixation"));
            Assert.AreEqual("Refused: Northline Fixation has no 3.5 mm screw, so this hole stays empty.", HoleMessages.Describe(_hole, screw, AcceptResult.NotInCatalog));
        }

        [Test]
        public void Occupied_AndNoScrew_ExplainThemselves()
        {
            StringAssert.Contains("already holds a screw", HoleMessages.Describe(_hole, null, AcceptResult.Occupied));
            StringAssert.Contains("Only screws", HoleMessages.Describe(_hole, null, AcceptResult.NoScrew));
        }

        [Test]
        public void Switched_NamesTheCatalog()
        {
            StringAssert.StartsWith("Catalog: Chromatic Ortho.", HoleMessages.Switched(_hole.Catalog));
        }

        [Test]
        public void Feedback_ShowsTheMessageWhenAHoleRefuses()
        {
            var go = new GameObject("Feedback");
            try
            {
                var status = go.AddComponent<StatusLine>();
                var feedback = go.AddComponent<HoleFeedback>();
                feedback.Connect(new[] { _hole }, null, status);
                var screw = _kit.Screw(_kit.Spec(4.0f, Color.green), Vector3.zero);
                _hole.TryAccept(screw);
                Assert.AreEqual(HoleMessages.Describe(_hole, screw, AcceptResult.WrongSize), status.Message);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
