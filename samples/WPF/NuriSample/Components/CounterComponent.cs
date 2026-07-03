using Nuri.UI.Dsl;

namespace NuriSample.Components
{
    public class CounterComponent : Component
    {
        public override IElement Render()
        {
            var (count, setCount) = useState (0);
            var (count2, setCount2) = useState (0);

            return Div (
                        Button ($"Count1: {count}", () => setCount (count + 1)),
                        Button ("Reset1", () => setCount (0)),
                        Button ($"Count2: {count2}", () => setCount2 (count2 + 1)),
                        Button ("Reset2", () => setCount2 (0))
                   );
        }
    }
}
