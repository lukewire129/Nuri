using Nuri.UI.Dsl;

namespace Nuri.YamlStyleSample;

public sealed class YamlStyleComponent : Component
{
    public override IElement Render()
    {
        return
            Column(
                new IElement[]
                {
                    Text("Nuri").Style("title"),
                    Text("YAML Style System").Style("description"),
                    Button("Continue").Style("primary-button")
                }
            )
            .Style("card");
    }
}
