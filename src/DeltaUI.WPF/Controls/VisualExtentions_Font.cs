using DeltaUI.Core.UI.Values;
using System.Drawing;
using System.Windows;

namespace DeltaUI.WPF
{
    public static partial class VisualExtention
    {
        public static T FontWeight<T>(this T node, FontWeight weight) where T : IElement
        {
            node.SetProperty (nameof (FontWeight), WpfValueMapper.FromWpfFontWeight(weight));
            return node;
        }

        public static T FontSize<T>(this T node, double size) where T : IElement
        {
            node.SetProperty (nameof (FontSize), size);
            return node;
        }
        public static T FontFamily<T>(this T node, object content) where T : IElement
        {
            node.SetProperty (nameof (FontFamily), ToCoreFontFamilyIfSupported(content));
            return node;
        }
        public static T FontColor<T>(this T node, System.Windows.Media.LinearGradientBrush brushes) where T : IElement
        {
            node.SetProperty ("Foreground", WpfValueMapper.FromWpfBrush(brushes));
            return node;
        }

        public static T FontColor<T>(this T node, System.Windows.Media.SolidColorBrush brushes) where T : IElement
        {
            node.SetProperty ("Foreground", WpfValueMapper.FromWpfBrush(brushes));
            return node;
        }

        public static T FontColor<T>(this T node, Color color) where T : IElement
        {
            node.SetProperty ("Foreground", new BrushValue.Solid(WpfValueMapper.FromDrawingColor(color)));
            return node;
        }

        public static T FontColor<T>(this T node, string colorCode) where T : IElement
        {
            if (colorCode[0] != '#')
                throw new System.Exception ("ColorCode Error");

            node.SetProperty ("Foreground", new BrushValue.Solid(ColorValue.FromHex(colorCode)));

            return node;
        }

        private static object ToCoreFontFamilyIfSupported(object content)
        {
            switch (content)
            {
                case string source:
                    return new FontFamilyValue(source);
                case System.Windows.Media.FontFamily family:
                    return new FontFamilyValue(family.Source);
                default:
                    return content;
            }
        }
    }
}
