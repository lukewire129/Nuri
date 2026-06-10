using DeltaUI.WPF;

namespace DiffingEngineTest.Components
{
    public class CounterComponent : Component
    {
        public override IVisual Render()
        {
            var (visible, setVisible) = useState (true);

            return Div (
                        Input (InputTypes.Button, $"Count1: {visible}", (s, e) => setVisible (!visible)),
                        visible? new CounterComponent1 () : null!
                   );
        }
    }
}
