using System.Linq;
using Lab.W01.VariableScrew;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;

namespace Lab.W01.SizingStation.Tests
{
    public class MemoryMeterTests
    {
        SizingTestKit _kit;

        [SetUp]
        public void Build() => _kit = new SizingTestKit();

        [TearDown]
        public void Destroy() => _kit.Dispose();

        [Test]
        public void ScrewsThatShareAMesh_CountAsOneMeshAndOneMaterial_HoweverManyThereAre()
        {
            var screws = new[] { _kit.Screw(), _kit.Screw(), _kit.Screw(), _kit.Screw() };
            screws[0].Set(_kit.CatalogA.screws[0], 8f);
            screws[1].Set(_kit.CatalogA.screws[1], 24f);
            screws[2].Set(_kit.CatalogA.screws[2], 30f);

            var one = MemoryMeter.Measure(screws.Take(1));
            var four = MemoryMeter.Measure(screws);

            Assert.AreEqual(4, four.Screws);
            Assert.AreEqual(1, four.Meshes);
            Assert.AreEqual(1, four.Materials);
            Assert.AreEqual(one.SharedBytes, four.SharedBytes, "Three more screws, of other sizes, share everything.");
        }

        [Test]
        public void TheBytes_ComeFromTheProfiler_NotFromTypedInNumbers()
        {
            var memory = MemoryMeter.Measure(new[] { _kit.Screw() });

            Assert.AreEqual(Profiler.GetRuntimeMemorySizeLong(_kit.SharedMesh), memory.MeshBytes);
            Assert.AreEqual(Profiler.GetRuntimeMemorySizeLong(_kit.SharedMaterial), memory.MaterialBytes);
            Assert.That(memory.MeshBytes, Is.GreaterThan(0));
            Assert.AreEqual(memory.MeshBytes, memory.OneMeshBytes);
        }

        [Test]
        public void ABiggerMesh_MeasuresBigger()
        {
            var small = MemoryMeter.Measure(new[] { _kit.Screw() }).MeshBytes;

            var big = _kit.Track(new Mesh());
            big.vertices = Enumerable.Range(0, 3000).Select(i => new Vector3(i, 0f, 0f)).ToArray();
            big.triangles = Enumerable.Range(0, 3000).ToArray();
            var screw = _kit.Screw();
            screw.GetComponent<MeshFilter>().sharedMesh = big;

            Assert.That(MemoryMeter.Measure(new[] { screw }).MeshBytes, Is.GreaterThan(small));
        }

        [Test]
        public void ASecondMesh_IsCounted()
        {
            var other = _kit.Screw();
            other.GetComponent<MeshFilter>().sharedMesh = _kit.Track(new Mesh());
            var memory = MemoryMeter.Measure(new[] { _kit.Screw(), other });
            Assert.AreEqual(2, memory.Meshes);
        }

        [Test]
        public void ATextureOnTheMaterial_IsCountedOnce()
        {
            var texture = _kit.Track(new Texture2D(8, 8));
            _kit.SharedMaterial.mainTexture = texture;

            var memory = MemoryMeter.Measure(new[] { _kit.Screw(), _kit.Screw() });

            Assert.AreEqual(1, memory.Textures);
            Assert.AreEqual(Profiler.GetRuntimeMemorySizeLong(texture), memory.TextureBytes);
        }

        [Test]
        public void NoScrews_MeasureAsNothing()
        {
            var memory = MemoryMeter.Measure(new Screw[] { null });
            Assert.AreEqual(0, memory.Screws);
            Assert.AreEqual(0, memory.SharedBytes);
            Assert.AreEqual(0, memory.OneMeshBytes);
            Assert.AreEqual(0, MemoryMeter.Measure(null).Screws);
        }

        [Test]
        public void TheSizesInACatalog_AreEveryLengthOfEveryDiameter()
        {
            Assert.AreEqual(3 + 8 + 3, MemoryMeter.SizesIn(_kit.CatalogA));
            Assert.AreEqual(2 + 14, MemoryMeter.SizesIn(_kit.CatalogB));
            Assert.AreEqual(0, MemoryMeter.SizesIn(null));
        }

        [Test]
        public void OneMeshPerSize_IsTheMeasuredMesh_OncePerSize()
        {
            Assert.AreEqual(14 * 2048L, MemoryMeter.OneMeshPerSizeBytes(2048, 14));
        }
    }

    public class MemoryTextTests
    {
        [Test]
        public void Sizes_ReadInTheUnitThatFits()
        {
            Assert.AreEqual("512 B", MemoryText.Size(512));
            Assert.AreEqual("1.5 KB", MemoryText.Size(1536));
            Assert.AreEqual("2 KB", MemoryText.Size(2048));
            Assert.AreEqual("1.25 MB", MemoryText.Size(1310720));
        }

        [Test]
        public void TheMeasuredLines_GiveTheCounts_TheCost_AndWhereTheyWereMeasured()
        {
            var lines = MemoryText.Now(new ScrewMemory(5, 1, 1, 1, 2048, 1024, 4096), 37, 12);

            StringAssert.Contains("Screws on screen: 5", lines[0]);
            StringAssert.Contains("Meshes: 1", lines[1]);
            StringAssert.Contains("Materials: 1", lines[1]);
            StringAssert.Contains("7 KB", lines[2]);
            Assert.IsTrue(lines.Any(line => line.Contains("one draw call per screw, not SRP-batched")), "The cost is stated, not hidden.");
            Assert.IsTrue(lines.Any(line => line.Contains("37 one by one, 12 batched")));
            Assert.IsTrue(lines.Any(line => line.Contains("Measured in the Editor")));
        }

        [Test]
        public void WithNoDrawCallCount_ItSaysSo_AndShowsNoNumber()
        {
            var lines = MemoryText.Now(new ScrewMemory(1, 1, 1, 0, 1, 1, 0), -1, -1);
            Assert.IsTrue(lines.Any(line => line.Contains("not available")));
        }

        [Test]
        public void TheComparison_ShowsTheSum_AndCallsItCalculated()
        {
            var lines = MemoryText.Compared(new ScrewMemory(5, 1, 1, 1, 2048, 1024, 4096), 14, "Maker A");

            StringAssert.Contains("Maker A ships 14 sizes", lines[0]);
            StringAssert.Contains("Meshes: 14", lines[1]);
            StringAssert.Contains("14 × 2 KB = 28 KB", lines[2]);
            StringAssert.Contains("2 KB", lines[3]);
            Assert.IsTrue(lines.Any(line => line.Contains("Calculated")));
            StringAssert.Contains("calculated", MemoryText.ComparedTitle);
        }
    }

    public class MemoryReadoutTests
    {
        SizingTestKit _kit;
        MemoryReadout _readout;
        ScrewSizer _sizer;

        [SetUp]
        public void Build()
        {
            _kit = new SizingTestKit();
            _sizer = _kit.Sizer(_kit.Screw());
            _readout = _kit.Track(new GameObject("Readout")).AddComponent<MemoryReadout>();
            _readout.Connect(_sizer);
        }

        [TearDown]
        public void Destroy() => _kit.Dispose();

        [Test]
        public void ItCountsTheScrewsThatAreInTheScene()
        {
            var before = _readout.Memory.Screws;
            _kit.Screw();
            _kit.Screw();
            _readout.Refresh();
            Assert.AreEqual(before + 2, _readout.Memory.Screws);
        }

        [Test]
        public void TheComparison_UsesTheSizersCatalog_AndTogglesBack()
        {
            Assert.IsFalse(_readout.Comparing);
            Assert.AreEqual(MemoryText.NowTitle, _readout.Title);

            _readout.ToggleComparison();
            Assert.IsTrue(_readout.Comparing);
            Assert.AreEqual(MemoryText.ComparedTitle, _readout.Title);
            StringAssert.Contains("Maker A ships 14 sizes", _readout.Lines[0]);

            _sizer.NextCatalog();
            _readout.Refresh();
            StringAssert.Contains("Maker B ships 16 sizes", _readout.Lines[0]);

            _readout.ToggleComparison();
            StringAssert.Contains("Screws on screen", _readout.Lines[0]);
        }
    }
}
