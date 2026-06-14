using System.Windows;

namespace Nuri.WPF
{
    public static class FrameworkElementExtensions
    {
        public static readonly DependencyProperty UniqueIdProperty = DependencyProperty.RegisterAttached(
            "UniqueId",
            typeof(string),
            typeof(FrameworkElementExtensions),
            new PropertyMetadata(string.Empty));

        public static void SetUniqueId(this FrameworkElement element, string id)
        {
            element.SetValue(UniqueIdProperty, id);
        }

        public static string GetUniqueId(this FrameworkElement element)
        {
            return (string)element.GetValue(UniqueIdProperty);
        }
    }
}
