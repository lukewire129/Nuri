using DeltaUI.WPF;

namespace DeltaComponentReplace;

public class ComponentClassComponent : Component
{
    public override IVisual Render()
    {
        var (count, setCount) = useState (0);


        return Div (
                    Rows (100, 100, 300),
                    Input (InputTypes.Button, $"Count: {count}", (s, e) => setCount (count + 1))
                        .Size(100, 50)
                        .Start()
                        .Row(0),

                    Input (InputTypes.Button, "Reset", (s, e) => setCount (0))
                        .Row (1)
                );
    }
}
