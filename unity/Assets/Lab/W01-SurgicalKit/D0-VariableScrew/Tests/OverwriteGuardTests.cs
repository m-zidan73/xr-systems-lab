using Lab.W01.VariableScrew.EditorTools;
using NUnit.Framework;

namespace Lab.W01.VariableScrew.Tests
{
    public class OverwriteGuardTests
    {
        const string MissingPath = "Assets/Lab/does-not-exist.unity";

        [Test]
        public void Allow_ExistingAsset_AsksAndCanBeRefused()
        {
            var asked = 0;
            Assert.IsFalse(OverwriteGuard.Allow(ScrewPaths.ScenePath, path => { asked++; return false; }));
            Assert.AreEqual(1, asked);
        }

        [Test]
        public void Allow_ExistingAsset_ProceedsWhenConfirmed()
        {
            Assert.IsTrue(OverwriteGuard.Allow(ScrewPaths.ScenePath, path => true));
        }

        [Test]
        public void Allow_MissingAsset_ProceedsWithoutAsking()
        {
            var asked = 0;
            Assert.IsTrue(OverwriteGuard.Allow(MissingPath, path => { asked++; return false; }));
            Assert.AreEqual(0, asked);
        }

        [Test]
        public void Allow_TellsTheQuestionWhichAsset()
        {
            string askedAbout = null;
            OverwriteGuard.Allow(ScrewPaths.HelixScenePath, path => { askedAbout = path; return false; });
            Assert.AreEqual(ScrewPaths.HelixScenePath, askedAbout);
        }
    }
}
