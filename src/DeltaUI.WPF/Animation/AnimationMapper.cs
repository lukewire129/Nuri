using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DeltaUI.WPF
{
    public class AnimationMapper
    {
       public static readonly HashSet<string> factory =
       new HashSet<string>
       {
            "Opacity",
            "Margin",
            "Background",
            "Foreground",
            "Rotate"
       };

        public static DependencyProperty? GetDependencyProperty(string property)
        {
            return property switch
            {
                "Opacity" => UIElement.OpacityProperty,
                "Margin" => FrameworkElement.MarginProperty,
                "Background" => Control.BackgroundProperty,
                "Foreground" => Control.ForegroundProperty,
                "Rotate" => RotateTransform.AngleProperty, 
                _ => null
            };
        }
    }
}
