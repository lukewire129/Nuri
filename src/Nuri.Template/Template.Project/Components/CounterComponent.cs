using Nuri.UI.Dsl;

namespace Template.Project.Components
{
    public class CounterComponent : Component
    {
        public override IElement Render()
        {
            var (count, setCount) = useState (0);
            var (count2, setCount2) = useState (0);

            return Div (
                        Button ($"Count1: {count}", () => setCount (current => current + 1)),
                        Button ("Reset1", () => setCount (_ => 0)),
                        Button ($"Count2: {count2}", () => setCount2 (current => current + 1)),
                        Button ("Reset2", () => setCount2 (_ => 0))
                   );
        }
    }
}
