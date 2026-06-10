using DeltaUI.WPF;

namespace DeltaUISample.Components
{
    public class CounterComponent : Component
    {
        public override IVisual Render()
        {
            var (count, setCount) = useState (0);
            var (count2, setCount2) = useState (0);

            return Div (
                        Input (InputTypes.Button, $"Count1: {count}", (s, e) => setCount (count + 1)),
                        Input (InputTypes.Button, "Reset1", (s, e) => setCount (0)),
                        Input (InputTypes.Button, $"Count2: {count2}", (s, e) => setCount2 (count2 + 1)),
                        Input (InputTypes.Button, "Reset2", (s, e) => setCount2 (0))
                   );
        }
    }
}
