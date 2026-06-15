using Nuri.UI.Dsl;

namespace NuriFlowSample.Components
{
    public sealed class ConfirmDialogPage : Component
    {
        public override IElement Render()
        {
            var dialog = useDialog();

            return Div(
                    Text("Dialog").FontSize(22),
                    Text("This modal dialog was opened through IWindowService."),
                    Button("OK", () => dialog.Close(true)),
                    Button("Cancel", () => dialog.Close(false))
                )
                .Padding(20);
        }
    }
}
