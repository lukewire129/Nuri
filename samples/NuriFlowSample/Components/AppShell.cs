using Nuri.Navigation;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace NuriFlowSample.Components
{
    public sealed class AppShell : Component
    {
        public override IElement Render()
        {
            var router = useRouter();

            return Grid(
                    Sidebar(router).Column(0),
                    Outlet(router)
                        .FadeIn(180, EasingValue.CubicOut)
                        .FadeOut(120, EasingValue.CubicIn)
                        .Column(1)
                )
                .Columns(Pixels(220), Star);
        }

        private IElement Sidebar(IRouter router)
        {
            return Div(
                    Text("Nuri Flow").FontSize(22).FontWeight(FontWeightValue.Bold),
                    Button("Home", () => router.Navigate<HomePage>()),
                    Button("Detail 42", () => router.Navigate<DetailPage>(42)),
                    Button("Native Control", () => router.Navigate<NativePage>()),
                    Button("Back", () => router.Back())
                )
                .Padding(18)
                .Background("#F2F4F8");
        }
    }
}
