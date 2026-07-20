using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Nuri.WPF.Diagnostics
{
    public static class WpfElementHighlighter
    {
        private static FrameworkElement? _target;
        private static AdornerLayer? _layer;
        private static HighlightAdorner? _adorner;

        public static void Highlight(string componentId)
        {
            Clear();

            if (!WpfDevToolsRuntime.TryFindElement(componentId, out var element) || element == null)
                return;

            var layer = AdornerLayer.GetAdornerLayer(element);
            if (layer == null)
                return;

            _target = element;
            _layer = layer;
            _adorner = new HighlightAdorner(element);
            _target.Unloaded += OnTargetUnloaded;
            _layer.Add(_adorner);
        }

        public static void Clear()
        {
            if (_target != null)
                _target.Unloaded -= OnTargetUnloaded;

            if (_layer != null && _adorner != null)
                _layer.Remove(_adorner);

            _target = null;
            _layer = null;
            _adorner = null;
        }

        private static void OnTargetUnloaded(object sender, RoutedEventArgs args)
        {
            Clear();
        }

        private sealed class HighlightAdorner : Adorner
        {
            private static readonly Pen BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(37, 99, 235)), 2);
            private static readonly Brush FillBrush = new SolidColorBrush(Color.FromArgb(36, 37, 99, 235));

            public HighlightAdorner(UIElement adornedElement)
                : base(adornedElement)
            {
                IsHitTestVisible = false;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                var size = AdornedElement.RenderSize;
                if (size.Width <= 0 || size.Height <= 0)
                    return;

                var rect = new Rect(new Point(0, 0), size);
                drawingContext.DrawRectangle(FillBrush, BorderPen, rect);
            }
        }
    }
}
