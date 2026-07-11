using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace ContentControlChildrenTest.Components
{
    public class CounterComponent : Component
    {
        public override IElement Render()
        {
            var (isHover, setIsHover) = useState(false);
            var (isToggle, setIsToggle) = useState(true);

            return Div(DivTypes.Grid,
                    Div(DivTypes.Block)
                        .Background("#00070E")
                        .Brush("#34291E")
                        .Margin(10, 0, 0, 0),

                    Image("Resources/logo.png")
                        .Start()
                        .Height(38)
                        .BitmapScalingMode(ImageScalingModeValue.Fant),

                    Div(DivTypes.Block)
                        .Background(isHover ? "#1D3B4A" : "#1E2328")
                        .Brush(isHover ? "#46E6FF" : "#09343D")
                        .Thickness(2)
                        .Margin(50, 4, 4, 4),

                    Div(DivTypes.Grid,
                        Text("Play")
                            .FontSize(15)
                            .FontWeight(FontWeightValue.Bold)
                            .Center()
                            .FontColor("#FFFFFF")
                            .Margin(30, isToggle ? 0 : 100, 0, 0)
                            .Transition(500, EasingValue.CubicInOut),
                        Text("Stop")
                            .FontSize(15)
                            .FontWeight(FontWeightValue.Bold)
                            .Center()
                            .FontColor("#3C3C41")
                            .Margin(30, 0, 0, isToggle ? 100 : 0)
                            .Transition(500, EasingValue.CubicInOut)
                    )
                )
                .OnHover(value => setIsHover(_ => value))
                .OnClick(() => setIsToggle(current => !current))
                .Size(165, 38)
                .Cursor(isHover ? CursorValue.Hand : CursorValue.Arrow)
                .Background(ColorValue.FromArgb(0, 0, 0, 0));
        }
    }
}
