using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Nuri.UI.Values;

namespace Nuri.Avalonia
{
    internal sealed class AvaloniaDistributedStackPanel : Panel
    {
        public static readonly AttachedProperty<double> GrowProperty = AvaloniaProperty.RegisterAttached<
            AvaloniaDistributedStackPanel,
            Control,
            double>("Grow", 0d);

        private Orientation _orientation;
        private double _spacing;
        private ContentDistribution _justifyContent;

        public AvaloniaDistributedStackPanel(Orientation orientation)
        {
            _orientation = orientation;
        }

        public Orientation Orientation
        {
            get => _orientation;
            set
            {
                if (_orientation == value)
                    return;

                _orientation = value;
                InvalidateMeasure();
            }
        }

        public double Spacing
        {
            get => _spacing;
            set
            {
                if (_spacing.Equals(value))
                    return;

                _spacing = value;
                InvalidateMeasure();
            }
        }

        public ContentDistribution JustifyContent
        {
            get => _justifyContent;
            set
            {
                if (_justifyContent == value)
                    return;

                _justifyContent = value;
                InvalidateArrange();
            }
        }

        public static double GetGrow(AvaloniaObject element)
        {
            return element.GetValue(GrowProperty);
        }

        public static void SetGrow(AvaloniaObject element, double value)
        {
            element.SetValue(GrowProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var horizontal = Orientation == Orientation.Horizontal;
            var childConstraint = horizontal
                ? new Size(double.PositiveInfinity, availableSize.Height)
                : new Size(availableSize.Width, double.PositiveInfinity);
            var desiredMain = 0d;
            var desiredCross = 0d;

            foreach (var child in Children)
            {
                child.Measure(childConstraint);
                desiredMain += horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
                desiredCross = Math.Max(desiredCross, horizontal ? child.DesiredSize.Height : child.DesiredSize.Width);
            }

            if (Children.Count > 1)
                desiredMain += Spacing * (Children.Count - 1);

            return horizontal
                ? new Size(desiredMain, desiredCross)
                : new Size(desiredCross, desiredMain);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var horizontal = Orientation == Orientation.Horizontal;
            var finalMain = horizontal ? finalSize.Width : finalSize.Height;
            var finalCross = horizontal ? finalSize.Height : finalSize.Width;
            var childCount = Children.Count;
            var spacingMain = childCount > 1 ? Spacing * (childCount - 1) : 0d;
            var desiredMain = spacingMain;
            var fixedMain = spacingMain;
            var totalGrow = 0d;

            foreach (var child in Children)
            {
                var childDesiredMain = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
                var grow = Math.Max(0, GetGrow(child));
                desiredMain += childDesiredMain;
                totalGrow += grow;
                if (grow <= 0)
                    fixedMain += childDesiredMain;
            }

            var useGrow = totalGrow > 0 && !double.IsInfinity(finalMain);
            var growMain = useGrow ? Math.Max(0, finalMain - fixedMain) : 0d;
            var occupiedMain = useGrow ? fixedMain + growMain : desiredMain;

            CalculateDistribution(finalMain, occupiedMain, childCount, out var offset, out var gap);

            foreach (var child in Children)
            {
                var desiredChildMain = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
                var grow = Math.Max(0, GetGrow(child));
                var childMain = useGrow && grow > 0 ? growMain * grow / totalGrow : desiredChildMain;
                child.Arrange(horizontal
                    ? new Rect(offset, 0, childMain, finalCross)
                    : new Rect(0, offset, finalCross, childMain));
                offset += childMain + gap;
            }

            return finalSize;
        }

        private void CalculateDistribution(double finalMain, double occupiedMain, int childCount, out double offset, out double gap)
        {
            offset = 0;
            gap = Spacing;
            if (childCount == 0 || double.IsInfinity(finalMain))
                return;

            var freeSpace = Math.Max(0, finalMain - occupiedMain);
            switch (JustifyContent)
            {
                case ContentDistribution.Center:
                    offset = freeSpace / 2;
                    break;
                case ContentDistribution.End:
                    offset = freeSpace;
                    break;
                case ContentDistribution.SpaceBetween when childCount > 1:
                    gap += freeSpace / (childCount - 1);
                    break;
                case ContentDistribution.SpaceAround:
                    gap += freeSpace / childCount;
                    offset = freeSpace / (childCount * 2);
                    break;
                case ContentDistribution.SpaceEvenly:
                    gap += freeSpace / (childCount + 1);
                    offset = freeSpace / (childCount + 1);
                    break;
            }
        }
    }
}
