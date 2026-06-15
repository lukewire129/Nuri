using System.Windows.Controls;
using Nuri.UI.Dsl;
using Nuri.WPF;

namespace NuriFlowSample.Components
{
    public sealed class NativePage : Component
    {
        public override IElement Render()
        {
            return Div(
                    Text("Native WPF Control").FontSize(28),
                    WpfNative.Control(() => new ProgressBar
                    {
                        Minimum = 0,
                        Maximum = 100,
                        Value = 72,
                        Height = 24
                    })
                )
                .Padding(24);
        }
    }
}
