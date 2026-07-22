using Duxel.Core;
using Nuri.Duxel;
using Nuri.Duxel.Diagnostics;
using Nuri.DuxelDiagnosticsSample.Components;

var app = NuriApplication.Create<DiagnosticsSampleComponent>(
    title: "Nuri Duxel Diagnostics Sample",
    width: 940,
    height: 620,
    theme: UiTheme.Nord);

#if DEBUG
app.UseAttachDevTools();
#endif

app.Run();
