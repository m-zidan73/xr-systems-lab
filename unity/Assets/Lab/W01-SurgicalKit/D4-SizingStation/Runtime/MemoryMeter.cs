using System.Collections.Generic;
using System.Globalization;
using Lab.W01.VariableScrew;
using UnityEngine;
using UnityEngine.Profiling;

namespace Lab.W01.SizingStation
{
    /// <summary>What a set of screws costs: how many there are, and what they share.</summary>
    public readonly struct ScrewMemory
    {
        public readonly int Screws;
        public readonly int Meshes;
        public readonly int Materials;
        public readonly int Textures;
        public readonly long MeshBytes;
        public readonly long MaterialBytes;
        public readonly long TextureBytes;

        public ScrewMemory(int screws, int meshes, int materials, int textures, long meshBytes, long materialBytes, long textureBytes)
        {
            Screws = screws;
            Meshes = meshes;
            Materials = materials;
            Textures = textures;
            MeshBytes = meshBytes;
            MaterialBytes = materialBytes;
            TextureBytes = textureBytes;
        }

        /// <summary>Everything the screws share, counted once however many screws there are.</summary>
        public long SharedBytes => MeshBytes + MaterialBytes + TextureBytes;

        /// <summary>The size of one mesh. With a single shared mesh this is all the mesh memory there is.</summary>
        public long OneMeshBytes => Meshes > 0 ? MeshBytes / Meshes : 0;
    }

    /// <summary>
    /// Measures the screws that are really in the scene. Every size comes from Unity's profiler, so
    /// nothing here is a typed-in number. The figures are the Editor's, which are larger than a build's.
    /// </summary>
    public static class MemoryMeter
    {
        /// <summary>Shader values each screw sets in its own property block: stretch, head end, shaft length, colour, thread tiling, thread starts.</summary>
        public const int ValuesPerScrew = 6;

        public static ScrewMemory Measure(IEnumerable<Screw> screws)
        {
            var meshes = new HashSet<Mesh>();
            var materials = new HashSet<Material>();
            var textures = new HashSet<Texture>();
            var count = 0;
            if (screws != null)
            {
                foreach (var screw in screws)
                {
                    if (screw == null) continue;
                    count++;
                    var filter = screw.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null) meshes.Add(filter.sharedMesh);
                    var renderer = screw.GetComponent<Renderer>();
                    if (renderer == null) continue;
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || !materials.Add(material)) continue;
                        foreach (var property in material.GetTexturePropertyNameIDs())
                        {
                            var texture = material.GetTexture(property);
                            if (texture != null) textures.Add(texture);
                        }
                    }
                }
            }

            long meshBytes = 0, materialBytes = 0, textureBytes = 0;
            foreach (var mesh in meshes) meshBytes += Profiler.GetRuntimeMemorySizeLong(mesh);
            foreach (var material in materials) materialBytes += Profiler.GetRuntimeMemorySizeLong(material);
            foreach (var texture in textures) textureBytes += Profiler.GetRuntimeMemorySizeLong(texture);
            return new ScrewMemory(count, meshes.Count, materials.Count, textures.Count, meshBytes, materialBytes, textureBytes);
        }

        /// <summary>How many different screws a catalog ships: every length of every diameter.</summary>
        public static int SizesIn(ImplantCatalog catalog)
        {
            if (catalog == null || catalog.screws == null) return 0;
            var sizes = 0;
            foreach (var spec in catalog.screws)
                if (spec != null && spec.availableLengthsMm != null) sizes += spec.availableLengthsMm.Length;
            return sizes;
        }

        /// <summary>
        /// What the meshes would take with one mesh per size. Calculated, not measured: a mesh modelled
        /// at another length has the same number of vertices, so it is this mesh's size, once per size.
        /// </summary>
        public static long OneMeshPerSizeBytes(long oneMeshBytes, int sizes) => oneMeshBytes * sizes;
    }

    /// <summary>The words the memory readout shows.</summary>
    public static class MemoryText
    {
        public const string NowTitle = "One mesh, every size";
        public const string ComparedTitle = "One mesh per size (calculated)";

        public static string Size(long bytes)
        {
            if (bytes < 1024) return string.Format(CultureInfo.InvariantCulture, "{0} B", bytes);
            if (bytes < 1024 * 1024) return string.Format(CultureInfo.InvariantCulture, "{0:0.#} KB", bytes / 1024.0);
            return string.Format(CultureInfo.InvariantCulture, "{0:0.##} MB", bytes / (1024.0 * 1024.0));
        }

        /// <summary>
        /// The measured numbers. The draw calls are the whole scene's for one frame, split the way
        /// Unity counts them: drawn one by one, and drawn through the SRP Batcher. Below zero means
        /// the counters aren't available.
        /// </summary>
        public static string[] Now(ScrewMemory memory, long drawnOneByOne, long drawnBatched)
        {
            return new[]
            {
                $"Screws on screen: {memory.Screws}",
                $"Meshes: {memory.Meshes}   Materials: {memory.Materials}",
                $"Shared memory: {Size(memory.SharedBytes)}",
                $"   mesh {Size(memory.MeshBytes)}, material {Size(memory.MaterialBytes)}, texture {Size(memory.TextureBytes)}",
                $"Each screw adds: one property block, {MemoryMeter.ValuesPerScrew} values",
                "Cost: one draw call per screw, not SRP-batched",
                drawnOneByOne >= 0 && drawnBatched >= 0
                    ? $"Draw calls, whole scene: {drawnOneByOne} one by one, {drawnBatched} batched"
                    : "Draw calls: not available here",
                "Measured in the Editor.",
            };
        }

        public static string[] Compared(ScrewMemory memory, int sizes, string manufacturer)
        {
            var perSize = MemoryMeter.OneMeshPerSizeBytes(memory.OneMeshBytes, sizes);
            return new[]
            {
                $"{manufacturer} ships {sizes} sizes",
                $"Meshes: {sizes}   Materials: {memory.Materials}",
                $"Mesh memory: {sizes} × {Size(memory.OneMeshBytes)} = {Size(perSize)}",
                $"With one stretched mesh: {Size(memory.MeshBytes)}",
                "Calculated from the measured mesh, not built.",
            };
        }
    }
}
