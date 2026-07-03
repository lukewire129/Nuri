using Nuri.UI.Dsl;

namespace GridTest.Components
{
    public class CounterComponent2 : Component
    {
        public CounterComponent2()
        {
            
        }
        public override IElement Render()
        {
            var (count, setCount) = useState (0);

            return Grid (
                        Button ($"Comopent2 Count!: {count}", () => setCount (count + 1))
                            .Size (150, 50)
                            .Start ()
                            .Row (0),

                        Button ("Comopent2 Count Reset", () => setCount (0))
                            .Row (1)
                    )
                    .Rows (Pixels(100), Pixels(100));
        }
    }

    // TODO : 제네레이터 만들어주는 기능 필요할까?
}
