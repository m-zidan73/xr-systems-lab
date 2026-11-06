using Lab.W01.VariableScrew;
using NUnit.Framework;
using UnityEngine;

namespace Lab.W01.ColourCodedHoles.Tests
{
    public class HoleDropTargetTests
    {
        HoleTestKit _kit;
        ScrewSpec _spec35;
        ScrewSpec _spec40;
        Hole _hole;
        HoleDropTarget _target;

        [SetUp]
        public void SetUp()
        {
            _kit = new HoleTestKit();
            _spec35 = _kit.Spec(3.5f, Color.blue);
            _spec40 = _kit.Spec(4.0f, Color.green);
            _hole = _kit.Hole(3.5f, _kit.Catalog("Example", _spec35, _spec40), Vector3.zero);
            _target = _hole.gameObject.AddComponent<HoleDropTarget>();
        }

        [TearDown]
        public void TearDown() => _kit.Dispose();

        [Test]
        public void DropPoint_IsTheSeat()
        {
            Assert.AreEqual(_hole.Seat.position, _target.DropPoint);
        }

        [Test]
        public void TryDrop_MatchingScrew_IsSeated()
        {
            var item = _kit.Grabbable(_kit.Screw(_spec35, Vector3.right));
            Assert.IsTrue(_target.TryDrop(item));
            Assert.AreSame(item.GetComponent<Screw>(), _hole.Occupant);
        }

        [Test]
        public void TryDrop_WrongScrew_IsRefused()
        {
            var item = _kit.Grabbable(_kit.Screw(_spec40, Vector3.right));
            Assert.IsFalse(_target.TryDrop(item));
            Assert.IsNull(_hole.Occupant);
        }

        [Test]
        public void PickingUpASeatedScrew_EmptiesTheHole()
        {
            var item = _kit.Grabbable(_kit.Screw(_spec35, Vector3.right));
            _target.TryDrop(item);
            item.PickUp();
            Assert.IsNull(_hole.Occupant);
        }
    }
}
