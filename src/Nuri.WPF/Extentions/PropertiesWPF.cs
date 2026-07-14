using Nuri.UI.Values;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Nuri.WPF
{
    public static class AttachedPropertyExtensions
    {
        public static void UpdateAttachedProperty(this FrameworkElement? element, string propertyName, object value)
        {
            element.TryUpdateAttachedProperty(propertyName, value);
        }

        internal static bool TryUpdateAttachedProperty(this FrameworkElement? element, string propertyName, object value)
        {
            if (propertyName == "RowDefinitions")
            {
                if (element is not System.Windows.Controls.Grid grid || value is not List<LengthValue> rows)
                    return false;

                grid.RowDefinitions.Clear ();

                foreach (var rowdefinition in rows)
                {
                    grid.RowDefinitions.Add (new RowDefinition { Height = WpfValueMapper.ToWpfGridLength(rowdefinition) });
                }
                return true;
            }
            if (propertyName == "ColumnDefinitions")
            {
                if (element is not System.Windows.Controls.Grid grid || value is not List<LengthValue> columns)
                    return false;

                grid.ColumnDefinitions.Clear ();
                foreach (var columndefinition in columns)
                {
                    grid.ColumnDefinitions.Add (new ColumnDefinition { Width = WpfValueMapper.ToWpfGridLength(columndefinition) });
                }
                return true;
            }
            if (propertyName == "Grid.Row")
            {
                if (element == null)
                    return false;

                System.Windows.Controls.Grid.SetRow (element, (int)value);
                return true;
            }
            if (propertyName == "Grid.Column")
            {
                if (element == null)
                    return false;

                System.Windows.Controls.Grid.SetColumn (element, (int)value);
                return true;
            }
            if (propertyName == "Grid.RowSpan")
            {
                if (element == null)
                    return false;

                System.Windows.Controls.Grid.SetRowSpan (element, (int)value);
                return true;
            }
            if (propertyName == "Grid.ColumnSpan")
            {
                if (element == null)
                    return false;

                System.Windows.Controls.Grid.SetColumnSpan (element, (int)value);
                return true;
            }
            if (propertyName == "Canvas.Bottom")
            {
                if (element == null)
                    return false;

                System.Windows.Controls.Canvas.SetBottom (element, (double)value);
                return true;
            }
            if (propertyName == "Canvas.Top")
            {
                if (element == null)
                    return false;

                System.Windows.Controls.Canvas.SetTop (element, (double)value);
                return true;
            }
            if (propertyName == "Canvas.Left")
            {
                if (element == null)
                    return false;

                System.Windows.Controls.Canvas.SetLeft (element, (double)value);
                return true;
            }
            if (propertyName == "Canvas.Right")
            {
                if (element == null)
                    return false;

                System.Windows.Controls.Canvas.SetRight (element, (double)value);
                return true;
            }
            if (propertyName == "RenderOptions.BitmapScalingMode")
            {
                if (element == null)
                    return false;

                System.Windows.Media.RenderOptions.SetBitmapScalingMode (element, (System.Windows.Media.BitmapScalingMode)value);
                return true;
            }

            return false;
        }

        public static bool ResetAttachedProperty(this FrameworkElement? element, string propertyName)
        {
            if (element == null)
                return false;

            if (propertyName == "RowDefinitions")
            {
                if (element is not System.Windows.Controls.Grid grid)
                    return false;

                grid.RowDefinitions.Clear ();
                return true;
            }

            if (propertyName == "ColumnDefinitions")
            {
                if (element is not System.Windows.Controls.Grid grid)
                    return false;

                grid.ColumnDefinitions.Clear ();
                return true;
            }

            if (propertyName == "Grid.Row")
            {
                element.ClearValue (System.Windows.Controls.Grid.RowProperty);
                return true;
            }

            if (propertyName == "Grid.Column")
            {
                element.ClearValue (System.Windows.Controls.Grid.ColumnProperty);
                return true;
            }

            if (propertyName == "Grid.RowSpan")
            {
                element.ClearValue (System.Windows.Controls.Grid.RowSpanProperty);
                return true;
            }

            if (propertyName == "Grid.ColumnSpan")
            {
                element.ClearValue (System.Windows.Controls.Grid.ColumnSpanProperty);
                return true;
            }

            if (propertyName == "Canvas.Bottom")
            {
                element.ClearValue (System.Windows.Controls.Canvas.BottomProperty);
                return true;
            }

            if (propertyName == "Canvas.Top")
            {
                element.ClearValue (System.Windows.Controls.Canvas.TopProperty);
                return true;
            }

            if (propertyName == "Canvas.Left")
            {
                element.ClearValue (System.Windows.Controls.Canvas.LeftProperty);
                return true;
            }

            if (propertyName == "Canvas.Right")
            {
                element.ClearValue (System.Windows.Controls.Canvas.RightProperty);
                return true;
            }

            if (propertyName == "RenderOptions.BitmapScalingMode")
            {
                element.ClearValue (System.Windows.Media.RenderOptions.BitmapScalingModeProperty);
                return true;
            }

            if (propertyName == "Cursor")
            {
                element.ClearValue (FrameworkElement.CursorProperty);
                return true;
            }

            return false;
        }
    }
}
