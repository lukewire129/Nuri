using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace BorderTest.Components
{
    public class CounterComponent : Component
    {
        public override IElement Render()
        {
            var (count, setCount) = useState (0);
            var (count2, setCount2) = useState (0);

            return Div (DivTypes.Block,
                        Text ("Div style host")
                            .Center ()
                            .FontColor (ColorValue.FromRgb(255, 255, 255))
                   )
                   .CornerRadius (20)
                   .Background (ColorValue.FromRgb(255, 0, 0))
                  .Brush (ColorValue.FromRgb(0, 0, 0));
        }
    }
}
