using NUnit.Framework;
using UnityEngine;

namespace Lab.Core.Tests
{
    public class GrabbableTests
    {
        const float Tolerance = 1e-4f;
        static readonly Vector3 Home = new Vector3(1f, 2f, 3f);

        GameObject _go;
        Grabbable _item;

        [SetUp]
        public void SetUp()
        {
            _go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _go.transform.SetPositionAndRotation(Home, Quaternion.Euler(0f, 30f, 0f));
            _item = _go.AddComponent<Grabbable>();
            _item.SetHome();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void PickUp_MarksItHeldAndSaysSo()
        {
            Grabbable picked = null;
            _item.PickedUp += item => picked = item;
            _item.PickUp();
            Assert.IsTrue(_item.IsHeld);
            Assert.AreSame(_item, picked);
        }

        [Test]
        public void MoveTo_AndRotateHeld_ChangeThePose()
        {
            _item.PickUp();
            _item.MoveTo(Vector3.zero);
            _item.RotateHeld(15f);
            Assert.AreEqual(0f, _go.transform.position.magnitude, Tolerance);
            Assert.AreEqual(45f, _go.transform.eulerAngles.y, Tolerance);
        }

        [Test]
        public void RefusedDrop_GoesBackHome()
        {
            bool? accepted = null;
            _item.Dropped += (item, wasAccepted) => accepted = wasAccepted;
            _item.PickUp();
            _item.MoveTo(Vector3.zero);
            _item.RotateHeld(90f);
            _item.Drop(false);
            Assert.IsFalse(_item.IsHeld);
            Assert.AreEqual(false, accepted);
            Assert.AreEqual(0f, Vector3.Distance(Home, _go.transform.position), Tolerance);
            Assert.AreEqual(30f, _go.transform.eulerAngles.y, Tolerance);
        }

        [Test]
        public void AcceptedDrop_StaysWhereItWasPlaced()
        {
            _item.PickUp();
            _item.MoveTo(Vector3.zero);
            _item.Drop(true);
            Assert.IsFalse(_item.IsHeld);
            Assert.AreEqual(0f, _go.transform.position.magnitude, Tolerance);
        }

        [Test]
        public void ReturnHome_BeforeAHomeIsKnown_StaysWhereItIs()
        {
            var other = new GameObject("No home yet");
            try
            {
                other.transform.position = new Vector3(4f, 5f, 6f);
                var item = other.AddComponent<Grabbable>();
                item.ReturnHome(true);
                Assert.AreEqual(0f, Vector3.Distance(new Vector3(4f, 5f, 6f), other.transform.position), Tolerance);
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void SetHighlighted_WritesTheHighlight_AndKeepsOtherValues()
        {
            var renderer = _go.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Color.red);
            renderer.SetPropertyBlock(block);

            _item.SetHighlighted(true);
            renderer.GetPropertyBlock(block);
            Assert.AreEqual(1f, block.GetFloat("_Highlight"));
            Assert.AreEqual(Color.red, block.GetColor("_BaseColor"));

            _item.SetHighlighted(false);
            renderer.GetPropertyBlock(block);
            Assert.AreEqual(0f, block.GetFloat("_Highlight"));
        }
    }
}
