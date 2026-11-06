using Lab.W01.VariableScrew;
using NUnit.Framework;
using UnityEngine;

namespace Lab.W01.ColourCodedHoles.Tests
{
    public class HoleTests
    {
        const float Tolerance = 1e-5f;
        static readonly Color Blue = new Color(0.16f, 0.42f, 0.85f);
        static readonly Color Green = new Color(0.2f, 0.7f, 0.3f);

        HoleTestKit _kit;
        ScrewSpec _spec35;
        ScrewSpec _spec40;
        ImplantCatalog _catalog;
        Hole _hole;

        [SetUp]
        public void SetUp()
        {
            _kit = new HoleTestKit();
            _spec35 = _kit.Spec(3.5f, Blue);
            _spec40 = _kit.Spec(4.0f, Green);
            _catalog = _kit.Catalog("Example", _spec35, _spec40);
            _hole = _kit.Hole(3.5f, _catalog, new Vector3(0f, 0.05f, 0f));
        }

        [TearDown]
        public void TearDown() => _kit.Dispose();

        [Test]
        public void TryAccept_MatchingDiameter_SeatsTheScrew()
        {
            var screw = _kit.Screw(_spec35, new Vector3(1f, 0f, 0f));
            Assert.AreEqual(AcceptResult.Accepted, _hole.TryAccept(screw));
            Assert.AreSame(screw, _hole.Occupant);
            Assert.AreEqual(0f, Vector3.Distance(_hole.Seat.position, screw.transform.position), Tolerance);
            Assert.AreEqual(0f, Quaternion.Angle(_hole.Seat.rotation, screw.transform.rotation), 0.01f);
        }

        [Test]
        public void TryAccept_WrongDiameter_IsRefusedAndLeavesTheScrewAlone()
        {
            var start = new Vector3(1f, 0f, 0f);
            var screw = _kit.Screw(_spec40, start);
            Assert.AreEqual(AcceptResult.WrongSize, _hole.TryAccept(screw));
            Assert.IsNull(_hole.Occupant);
            Assert.AreEqual(start, screw.transform.position);
        }

        [Test]
        public void TryAccept_WhileOccupied_IsRefused()
        {
            _hole.TryAccept(_kit.Screw(_spec35, Vector3.right));
            Assert.AreEqual(AcceptResult.Occupied, _hole.TryAccept(_kit.Screw(_spec35, Vector3.left)));
        }

        [Test]
        public void TryAccept_SizeTheCatalogDoesNotShip_IsRefused()
        {
            _hole.SetCatalog(_kit.Catalog("Other", _spec40));
            Assert.AreEqual(AcceptResult.NotInCatalog, _hole.TryAccept(_kit.Screw(_spec35, Vector3.right)));
        }

        [Test]
        public void TryAccept_NothingOrAScrewWithoutSpec_IsRefused()
        {
            Assert.AreEqual(AcceptResult.NoScrew, _hole.TryAccept(null));
            Assert.AreEqual(AcceptResult.WrongSize, _hole.TryAccept(_kit.Screw(null, Vector3.right)));
        }

        [Test]
        public void Events_ReportWhatHappened()
        {
            Screw accepted = null;
            AcceptResult? refusal = null;
            _hole.Accepted += (hole, screw) => accepted = screw;
            _hole.Refused += (hole, screw, result) => refusal = result;

            var wrong = _kit.Screw(_spec40, Vector3.right);
            _hole.TryAccept(wrong);
            Assert.AreEqual(AcceptResult.WrongSize, refusal);

            var right = _kit.Screw(_spec35, Vector3.left);
            _hole.TryAccept(right);
            Assert.AreSame(right, accepted);
        }

        [Test]
        public void Vacate_FreesTheHoleAndSaysSo()
        {
            Screw vacated = null;
            _hole.Vacated += (hole, screw) => vacated = screw;
            var screw = _kit.Screw(_spec35, Vector3.right);
            _hole.TryAccept(screw);
            _hole.Vacate();
            Assert.IsNull(_hole.Occupant);
            Assert.AreSame(screw, vacated);
        }

        [Test]
        public void Ring_TakesTheCatalogColour_OrGreyWhenTheSizeIsMissing()
        {
            HoleTestKit.AssertRingColour(Blue, _hole);
            _hole.SetCatalog(_kit.Catalog("Other", _spec40));
            HoleTestKit.AssertRingColour(Hole.MissingColour, _hole);
        }
    }
}
