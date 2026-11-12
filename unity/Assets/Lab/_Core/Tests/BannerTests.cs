using NUnit.Framework;
using UnityEngine;

namespace Lab.Core.Tests
{
    public class BannerTests
    {
        GameObject _go;
        Banner _banner;

        [SetUp]
        public void Build()
        {
            _go = new GameObject("Banner");
            _banner = _go.AddComponent<Banner>();
        }

        [TearDown]
        public void Destroy() => Object.DestroyImmediate(_go);

        [Test]
        public void StartsHidden() => Assert.IsFalse(_banner.IsShowing);

        [Test]
        public void Show_SetsTitleAndSubtitle_ThenHideHidesIt()
        {
            _banner.Show("Plate in place", "Screws go in next.");
            Assert.IsTrue(_banner.IsShowing);
            Assert.AreEqual("Plate in place", _banner.Title);
            Assert.AreEqual("Screws go in next.", _banner.Subtitle);
            _banner.Hide();
            Assert.IsFalse(_banner.IsShowing);
        }

        [Test]
        public void Show_AgainReplacesTheText()
        {
            _banner.Show("First", "One");
            _banner.Show("Second", "Two");
            Assert.AreEqual("Second", _banner.Title);
            Assert.AreEqual("Two", _banner.Subtitle);
        }
    }
}
