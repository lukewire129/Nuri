using Duxel.Core;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.Duxel;

public static class DuxelColorExtensions
{
    public static ColorValue ToColorValue(this UiColor color)
    {
        var rgba = color.Rgba;
        return ColorValue.FromArgb(
            (byte)(rgba >> 24),
            (byte)rgba,
            (byte)(rgba >> 8),
            (byte)(rgba >> 16));
    }

    public static T Background<T>(this T node, UiColor color)
        where T : IElement
    {
        return ElementExtensions.Background(node, color.ToColorValue());
    }
}
