using Nuri.Duxel;
using Nuri.DuxelEditorStressSample.Components;
using Nuri.Runtime.Diagnostics;

NuriDiagnostics.Enable();

NuriApplication.Run<DuxelEditorStressComponent>(
    title: "Nuri Editor Stress - Duxel",
    width: 1280,
    height: 800);
