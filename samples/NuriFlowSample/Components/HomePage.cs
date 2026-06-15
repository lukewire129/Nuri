using Nuri.Hosting;
using Nuri.UI.Dsl;
using NuriFlowSample.Services;

namespace NuriFlowSample.Components
{
    public sealed class HomePage : Component
    {
        public override IElement Render()
        {
            var appInfo = useService<IAppInfoService>();
            var windows = useWindows();

            return Div(
                    Text("Home").FontSize(28),
                    Text($"Resolved from app IServiceProvider: {appInfo.Name}"),
                    Button("Open tool window", () =>
                        windows.Show<ToolWindowPage>(new NuriWindowOptions
                        {
                            Title = "Tool Window",
                            Width = 360,
                            Height = 240
                        })),
                    Button("Open modal dialog", async () =>
                    {
                        var result = await windows.ShowDialogAsync<ConfirmDialogPage>(new NuriWindowOptions
                        {
                            Title = "Dialog",
                            Width = 360,
                            Height = 220
                        });

                        System.Diagnostics.Debug.WriteLine($"Dialog result: {result}");
                    })
                )
                .Padding(24);
        }

        protected override void OnEnter()
        {
            System.Diagnostics.Debug.WriteLine("HomePage entered.");
        }

        protected override void OnLeave()
        {
            System.Diagnostics.Debug.WriteLine("HomePage left.");
        }
    }
}
