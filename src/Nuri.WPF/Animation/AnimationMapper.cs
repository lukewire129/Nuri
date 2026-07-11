using System.Collections.Generic;
using Nuri.Constants;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Nuri.WPF
{
    public class AnimationMapper
    {
       public static readonly HashSet<string> factory =
       new HashSet<string>
       {
            "Opacity",
            "Margin",
             PropertyKeys.Background,
             PropertyKeys.Foreground,
            "Rotate"
       };

        public static DependencyProperty? GetDependencyProperty(string property)
        {
            return property switch
            {
                "Opacity" => UIElement.OpacityProperty,
                "Margin" => FrameworkElement.MarginProperty,
                PropertyKeys.Background => Control.BackgroundProperty,
                PropertyKeys.Foreground => Control.ForegroundProperty,
                "Rotate" => RotateTransform.AngleProperty, 
                _ => null
            };
        }
    }
}
