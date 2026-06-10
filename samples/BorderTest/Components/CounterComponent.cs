using DeltaUI.WPF;
using System.Drawing;
using Base = System.Windows.Controls;

namespace BorderTest.Components
{
    public class CounterComponent : Component
    {
        public override IVisual Render()
        {
            var (count, setCount) = useState (0);
            var (count2, setCount2) = useState (0);

            return Div (DivTypes.Block,
                        Text ("Div style host")
                            .Center ()
                            .FontColor (Color.White)
                   )
                   .CornerRadius (20)
                   .Background (Color.Red)
                  .Brush (Color.Black);
        }
    }
}
