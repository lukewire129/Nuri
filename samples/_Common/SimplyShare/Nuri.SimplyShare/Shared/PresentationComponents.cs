namespace Nuri.SimplyShare.Shared;

internal sealed class PageLayout : Component
{
    private readonly string _title;
    private readonly string _subtitle;
    private readonly IElement[] _children;

    public PageLayout(string title, string subtitle, params IElement[] children)
    {
        _title = title;
        _subtitle = subtitle;
        _children = children;
    }

    public override IElement Render()
    {
        return Div(DivTypes.Scroll,
                Div(new IElement[]
                    {
                        Text(_title).FontSize(30).FontWeight(FontWeightValue.Bold).FontColor(Palette.Ink),
                        Text(_subtitle).FontSize(13).FontColor(Palette.Muted).Margin(top: 6, bottom: 22)
                    }
                    .Concat(_children)
                    .ToArray())
                    .Padding(28))
            .Background(Palette.Canvas);
    }
}

internal sealed class SurfaceCard : Component
{
    private readonly IElement[] _children;

    public SurfaceCard(params IElement[] children) => _children = children;

    public override IElement Render()
    {
        return Div(_children)
            .Padding(20)
            .Margin(bottom: 14)
            .Background(Palette.White)
            .Brush(Palette.Border)
            .Thickness(1)
            .CornerRadius(14);
    }
}

internal static class Formatters
{
    public static string FileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }
}
