using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The warm-up takes the trace recorded on the running graphics API, and with none for it finishes at once,
     * warning; traces merge (only the same API's) and their shape tells which traced shader changed.</summary>
     */
    public class WarmUpTests
    {
        #region Private Fields

        private readonly List<GraphicsStateCollection> _made = new();
        private readonly List<string> _files = new();

        #endregion

        [TearDown]
        public void TearDown()
        {
            foreach (GraphicsStateCollection trace in _made)
                Object.DestroyImmediate(trace);
            _made.Clear();
            foreach (string file in _files)
                File.Delete(file);
            _files.Clear();
        }

        private GraphicsStateCollection RecordedOn(GraphicsDeviceType api)
        {
            var trace = new GraphicsStateCollection { graphicsDeviceType = api };
            _made.Add(trace);
            return trace;
        }

        [Test]
        public void TheTraceForTheApi_IsTheOneRecordedOnIt()
        {
            GraphicsStateCollection vulkan = RecordedOn(GraphicsDeviceType.Vulkan);
            GraphicsStateCollection metal = RecordedOn(GraphicsDeviceType.Metal);
            var traces = new List<GraphicsStateCollection> { vulkan, null, metal };

            Assert.AreSame(metal, AnimeWarmUp.For(traces, GraphicsDeviceType.Metal));
            Assert.AreSame(vulkan, AnimeWarmUp.For(traces, GraphicsDeviceType.Vulkan));
            Assert.IsNull(AnimeWarmUp.For(traces, GraphicsDeviceType.Direct3D12), "no trace for the API");
            Assert.IsNull(AnimeWarmUp.For(null, GraphicsDeviceType.Metal), "no traces at all");
        }

        [Test]
        public void WithNoTraceForTheApi_TheWarmUpFinishesAtOnce()
        {
            GraphicsDeviceType other = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan ? GraphicsDeviceType.Metal : GraphicsDeviceType.Vulkan;
            var traces = new List<GraphicsStateCollection> { RecordedOn(other) };

            LogAssert.Expect(LogType.Warning, new Regex("No shader trace for"));
            Awaitable warming = AnimeWarmUp.WarmAsync(traces, 4, CancellationToken.None);
            Assert.IsTrue(warming.IsCompleted);

            LogAssert.Expect(LogType.Warning, new Regex("No shader trace for"));
            Assert.IsFalse(AnimeWarmUp.WarmNow(traces), "warming at once has nothing to warm either");
        }

        private string Saved(GraphicsStateCollection trace)
        {
            string path = Path.Combine(Path.GetTempPath(), $"anime-trace-test-{_files.Count}.graphicsstate");
            Assume.That(trace.SaveToFile(path), "a trace saves");
            _files.Add(path);
            return path;
        }

        [Test]
        public void ARecording_MergesIntoATraceOfItsApi_KeepingBoth()
        {
            GraphicsStateCollection first = RecordedOn(SystemInfo.graphicsDeviceType);
            GraphicsStateCollection second = RecordedOn(SystemInfo.graphicsDeviceType);
            var shader = Shader.Find(AnimeShaderIds.CharacterShader);
            Assume.That(first.AddVariant(shader, new PassIdentifier(0, 0), new LocalKeyword[0]), "a variant in the first");
            Assume.That(second.AddVariant(shader, new PassIdentifier(0, 1), new LocalKeyword[0]), "another in the second");
            string into = Saved(first);

            Assert.IsNull(AnimeTraceTools.Merge(into, Saved(second)));
            var merged = new GraphicsStateCollection(into);
            _made.Add(merged);
            Assert.AreEqual(2, merged.variantCount, "both variants");
        }

        [Test]
        public void ARecordingOnAnotherApi_OrAMissingFile_IsRefused()
        {
            GraphicsDeviceType other = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan ? GraphicsDeviceType.Metal : GraphicsDeviceType.Vulkan;
            string into = Saved(RecordedOn(SystemInfo.graphicsDeviceType));

            StringAssert.Contains(other.ToString(), AnimeTraceTools.Merge(into, Saved(RecordedOn(other))));
            StringAssert.StartsWith("can't read", AnimeTraceTools.Merge(into, Path.Combine(Path.GetTempPath(), "no-such-trace.graphicsstate")));
        }

        [Test]
        public void AShaderWhoseLineChanged_IsNamed_AndOnlyIt()
        {
            const string recorded = "Anime/A | passes 2 | _ONE\nAnime/B | passes 1 | _TWO\n";
            const string now = "Anime/A | passes 3 | _ONE\nAnime/B | passes 1 | _TWO\nAnime/C | passes 1 | \n";

            CollectionAssert.AreEqual(new[] { "Anime/A", "Anime/C" }, AnimeTraceTools.Changed(recorded, now), "a new pass, and a shader the shape never held");
            Assert.IsEmpty(AnimeTraceTools.Changed(recorded, recorded));
        }
    }
}
