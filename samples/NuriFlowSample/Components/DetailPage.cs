using Nuri.UI.Dsl;
using NuriFlowSample.Services;

namespace NuriFlowSample.Components
{
    public sealed class DetailPage : Component
    {
        public override IElement Render()
        {
            var id = useRouteParameter<int>();
            var appInfo = useService<IAppInfoService>();

            return Div(
                    Text("Detail").FontSize(28),
                    Text($"Route parameter: {id}"),
                    Text($"Service after Navigate: {appInfo.Name}")
                )
                .Padding(24);
        }
    }
}
