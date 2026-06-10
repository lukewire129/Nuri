using DeltaUI.WPF;

namespace DiffingEngineTest.Components
{
    public class CounterComponent1 : Component
    {
        public override IVisual Render()
        {
            var (count, setCount) = useState (0);

            return Div (
                        Rows (100, 100),
                        Input (InputTypes.Button, $"Comopent Count!: {count}", (s, e) => setCount (count + 1))
                            .Size (150, 50)
                            .Start ()
                            .Row (0),

                        Input (InputTypes.Button, "Comopent Count Reset", (s, e) => setCount (0))
                            .Row (1)
                    );
        }
    }
}
