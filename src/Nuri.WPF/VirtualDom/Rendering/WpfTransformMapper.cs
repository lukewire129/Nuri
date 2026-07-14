using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Nuri.WPF
{
    internal static class WpfTransformMapper
    {
        public static ScaleTransform GetScale(FrameworkElement element)
        {
            return GetTransformGroup(element).Children.OfType<ScaleTransform>().First();
        }

        public static RotateTransform GetRotate(FrameworkElement element)
        {
            return GetTransformGroup(element).Children.OfType<RotateTransform>().First();
        }

        public static TranslateTransform GetTranslate(FrameworkElement element)
        {
            return GetTransformGroup(element).Children.OfType<TranslateTransform>().First();
        }

        private static TransformGroup GetTransformGroup(FrameworkElement element)
        {
            if (element.RenderTransform is TransformGroup existingGroup
                && existingGroup.Children.OfType<ScaleTransform>().Any()
                && existingGroup.Children.OfType<RotateTransform>().Any()
                && existingGroup.Children.OfType<TranslateTransform>().Any())
                return existingGroup;

            var scale = element.RenderTransform as ScaleTransform
                ?? (element.RenderTransform as TransformGroup)?.Children.OfType<ScaleTransform>().FirstOrDefault()
                ?? new ScaleTransform();
            var rotate = element.RenderTransform as RotateTransform
                ?? (element.RenderTransform as TransformGroup)?.Children.OfType<RotateTransform>().FirstOrDefault()
                ?? new RotateTransform();
            var translate = element.RenderTransform as TranslateTransform
                ?? (element.RenderTransform as TransformGroup)?.Children.OfType<TranslateTransform>().FirstOrDefault()
                ?? new TranslateTransform();

            var group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(rotate);
            group.Children.Add(translate);
            element.RenderTransform = group;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            return group;
        }
    }
}
