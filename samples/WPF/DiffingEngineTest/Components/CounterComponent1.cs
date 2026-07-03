using Nuri.UI.Dsl;

namespace DiffingEngineTest.Components
{
    public class CounterComponent1 : Component
    {
        public override IElement Render()
        {
            var (count, setCount) = useState (0);

            return Grid (
                        Button ($"Comopent Count!: {count}", () => setCount (count + 1))
                            .Size (150, 50)
                            .Start ()
                            .Row (0),

                        Button ("Comopent Count Reset", () => setCount (0))
                            .Row (1)
                    )
                    .Rows (Pixels(100), Pixels(100));
        }
    }
}
