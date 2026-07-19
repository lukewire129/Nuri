# Nuri WPF Editor Stress Comparison

These two executables compile the same shared 1,000-line eager keyed editor component.

- PackageSample references the published Nuri.WPF 0.2.0 and its transitive Nuri 0.2.0 package.
- SourceSample references the current src/Nuri.WPF project.

Build and run both in Release. Close one process before starting the other so GC and UI scheduling do not compete.

    dotnet run --project "samples\WPF\Nuri.WPFEditorStressComparison\PackageSample\Nuri.WPFEditorStress.PackageSample.csproj" -c Release

    dotnet run --project "samples\WPF\Nuri.WPFEditorStressComparison\SourceSample\Nuri.WPFEditorStress.SourceSample.csproj" -c Release

In each process, run Run 100 once as a warmup, then run Run 1,000 three times. Compare the median elapsed time and allocated MB. Each iteration performs one state update, one render/diff/patch/commit, and changes exactly one keyed editor row.

The comparison measures the full published-package versus current-source stack, not only one Core method. Use the Core performance harness when isolating VirtualTreeDiff itself.

For a deterministic phase comparison that separates virtual-tree creation, diff, initial WPF build, property patch, and the sequential combined path, use perf/Nuri.WPFPhaseComparison. The interactive elapsed value also includes hooks, Dispatcher scheduling, effect flushing, layout, and process-wide GC behavior.
