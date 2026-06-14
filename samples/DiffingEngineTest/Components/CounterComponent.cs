using Nuri.UI.Dsl;

namespace DiffingEngineTest.Components
{
    public class CounterComponent : Component
    {
        public override IElement Render()
        {
            var (visible, setVisible) = useState (true);

            return Div (
                        Button ($"Count1: {visible}", () => setVisible (!visible)),
                        visible? new CounterComponent1 () : null!
                   );
        }
    }
}
