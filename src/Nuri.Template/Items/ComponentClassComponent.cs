using Nuri.UI.Dsl;

namespace NuriComponentReplace;

public class ComponentClassComponent : Component
{
    public override IElement Render()
    {
        var (count, setCount) = useState (0);


        return Grid (
                    Button ($"Count: {count}", () => setCount (count + 1))
                        .Size(100, 50)
                        .Start()
                        .Row(0),

                    Button ("Reset", () => setCount (0))
                        .Row (1)
                )
                .Rows (Pixels(100), Pixels(100), Pixels(300));
    }
}
