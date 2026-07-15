using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Nuri.WPF
{
    internal sealed class NuriGrid : Grid
    {
        private double _rowSpacing;
        private double _columnSpacing;

        public double RowSpacing
        {
            get => _rowSpacing;
            set
            {
                if (_rowSpacing.Equals(value))
                    return;

                _rowSpacing = value;
                InvalidateMeasure();
            }
        }

        public double ColumnSpacing
        {
            get => _columnSpacing;
            set
            {
                if (_columnSpacing.Equals(value))
                    return;

                _columnSpacing = value;
                InvalidateMeasure();
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var rowGap = GetTotalGap(RowDefinitions.Count, RowSpacing);
            var columnGap = GetTotalGap(ColumnDefinitions.Count, ColumnSpacing);
            var compactConstraint = new Size(
                SubtractGap(constraint.Width, columnGap),
                SubtractGap(constraint.Height, rowGap));
            var desired = base.MeasureOverride(compactConstraint);

            return new Size(desired.Width + columnGap, desired.Height + rowGap);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var rowCount = Math.Max(1, RowDefinitions.Count);
            var columnCount = Math.Max(1, ColumnDefinitions.Count);
            var rowGap = GetTotalGap(rowCount, RowSpacing);
            var columnGap = GetTotalGap(columnCount, ColumnSpacing);
            var compactSize = new Size(
                Math.Max(0, arrangeSize.Width - columnGap),
                Math.Max(0, arrangeSize.Height - rowGap));

            base.ArrangeOverride(compactSize);

            foreach (UIElement child in InternalChildren)
            {
                if (child is not FrameworkElement frameworkChild)
                    continue;

                var compactSlot = LayoutInformation.GetLayoutSlot(frameworkChild);
                var row = Math.Min(GetRow(child), rowCount - 1);
                var column = Math.Min(GetColumn(child), columnCount - 1);
                var rowSpan = Math.Min(Math.Max(1, GetRowSpan(child)), rowCount - row);
                var columnSpan = Math.Min(Math.Max(1, GetColumnSpan(child)), columnCount - column);

                child.Arrange(new Rect(
                    compactSlot.X + column * ColumnSpacing,
                    compactSlot.Y + row * RowSpacing,
                    compactSlot.Width + (columnSpan - 1) * ColumnSpacing,
                    compactSlot.Height + (rowSpan - 1) * RowSpacing));
            }

            return arrangeSize;
        }

        private static double GetTotalGap(int definitionCount, double spacing)
        {
            return Math.Max(0, definitionCount - 1) * spacing;
        }

        private static double SubtractGap(double value, double gap)
        {
            return double.IsInfinity(value) ? value : Math.Max(0, value - gap);
        }
    }
}
