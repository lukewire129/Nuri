using Nuri.UI.Dsl;

namespace NuriFlowSample.Components
{
    public sealed class ToolWindowPage : Component
    {
        public override IElement Render()
        {
            return Div(
                    Text("Tool Window").FontSize(22),
                    Text("This window was opened through IWindowService.")
                )
                .Padding(20);
        }
    }
}
