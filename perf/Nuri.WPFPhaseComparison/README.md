# Nuri WPF Phase Comparison

The Package and Source executables compile the same benchmark code against different references:

- Package uses published Nuri.WPF 0.2.0 and Nuri 0.2.0.
- Source uses the current src/Nuri.WPF and src/Nuri projects.

Run each executable in a fresh Release process:

    dotnet run --project "perf\Nuri.WPFPhaseComparison\Package\Nuri.WPFPhase.Package.csproj" -c Release -- --label package-0.2.0 --size 1000 --iterations 300 --warmup 30

    dotnet run --project "perf\Nuri.WPFPhaseComparison\Source\Nuri.WPFPhase.Source.csproj" -c Release -- --label current-source --size 1000 --iterations 300 --warmup 30

Compare medians across multiple independent processes. Result count is a correctness invariant: VirtualTreeDiff, WPF property patch, and Full sequential update must each report exactly one patch.
