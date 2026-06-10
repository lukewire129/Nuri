using DeltaUI.WPF;
using System.Drawing;
using System.Windows;
using System.Windows.Input;

namespace ContentControlChildrenTest.Components
{
    public class CounterComponent : Component
    {
        public override IVisual Render()
        {
            var (isHover, setIsHover) = useState(false);
            var (isToggle, setIsToggle) = useState(true);

            return Div(DivTypes.Grid,
                    Div(DivTypes.Block)
                        .Background("#00070E")
                        .Brush("#34291E")
                        .Margin(left: 10),

                    Image("Resources/logo.png")
                        .Start()
                        .Height(38)
                        .BitmapScalingMode(System.Windows.Media.BitmapScalingMode.Fant),

                    Div(DivTypes.Block)
                        .Background(isHover ? "#1D3B4A" : "#1E2328")
                        .Brush(isHover ? "#46E6FF" : "#09343D")
                        .Thickness(2)
                        .Margin(50, 4, 4, 4),

                    Div(DivTypes.Grid,
                        Text("Play")
                            .FontSize(15)
                            .FontWeight(FontWeights.Bold)
                            .Center()
                            .FontColor("#FFFFFF")
                            .Margin(30, isToggle ? 0 : 100)
                            .Transitions("Margin", 500, Easing.CubicInOut),
                        Text("Stop")
                            .FontSize(15)
                            .FontWeight(FontWeights.Bold)
                            .Center()
                            .FontColor("#3C3C41")
                            .Margin(30, bottom: isToggle ? 100 : 0)
                            .Transitions("Margin", 500, Easing.CubicInOut)
                    )
                )
                .OnHover((s, e) => setIsHover(e.RoutedEvent == UIElement.MouseEnterEvent))
                .OnClick((s, e) => setIsToggle(!isToggle))
                .Size(165, 38)
                .Cursor(isHover ? Cursors.Hand : Cursors.Arrow)
                .Background(Color.Transparent);
        }
    }
}
