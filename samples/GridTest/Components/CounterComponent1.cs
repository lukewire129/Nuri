using Nuri.UI.Dsl;

namespace GridTest.Components
{
    public class CounterComponent1 : Component
    {
        public CounterComponent1()
        {
            
        }
        public override IElement Render()
        {
            var (count, setCount) = useState (0);

            return Grid (
                        Button ($"Comopent1 Count!: {count}", () => setCount (current => current + 1))
                            .Size (150, 50)
                            .Start ()
                            .Row (0),

                        Button ("Comopent1 Count Reset", () => setCount (_ => 0))
                            .Row (1)
                    )
                    .Rows (Pixels(100), Pixels(100));
        }
    }

    // TODO : 제네레이터 만들어주는 기능 필요할까?
}
