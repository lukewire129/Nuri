using System;
using System.Collections.Generic;
using Nuri.Constants;
using Nuri.UI.Values;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Nuri.WPF
{
    internal static class WpfValueMapper
    {
        public static object? ToWpfValue(object? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case ColorValue color:
                    return ToWpfColor(color);
                case BrushValue.Solid solid:
                    return new SolidColorBrush(ToWpfColor(solid.Color));
                case BrushValue.LinearGradient linearGradient:
                    return ToWpfLinearGradientBrush(linearGradient);
                case ThicknessValue thickness:
                    return new Thickness(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);
                case CornerRadiusValue cornerRadius:
                    return new CornerRadius(cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight, cornerRadius.BottomLeft);
                case CursorValue cursor:
                    return ToWpfCursor(cursor);
                case LengthValue length:
                    return ToWpfGridLength(length);
                case FontWeightValue fontWeight:
                    return FontWeight.FromOpenTypeWeight(fontWeight.OpenTypeWeight);
                case FontFamilyValue fontFamily:
                    return new FontFamily(fontFamily.Source);
                case DropShadowEffectValue dropShadowEffect:
                    return ToWpfDropShadowEffect(dropShadowEffect);
                case HorizontalAlignmentValue horizontalAlignment:
                    return ToWpfHorizontalAlignment(horizontalAlignment);
                case VerticalAlignmentValue verticalAlignment:
                    return ToWpfVerticalAlignment(verticalAlignment);
                case ImageScalingModeValue imageScalingMode:
                    return ToWpfBitmapScalingMode(imageScalingMode);
                default:
                    return value;
            }
        }

        public static AnimationTimeline? ToWpfAnimation(AnimationValue animation)
        {
            var easing = ToWpfEasing(animation.Easing);
            var to = ToWpfValue(animation.To);
            var from = ToWpfValue(animation.From);
            var duration = new Duration(animation.Duration);

            switch (animation.PropertyName)
            {
                case "Opacity":
                case "Rotate":
                    return new DoubleAnimation
                    {
                        From = from == null ? null : Convert.ToDouble(from),
                        To = Convert.ToDouble(to),
                        Duration = duration,
                        EasingFunction = easing
                    };
                case "Margin":
                    return new ThicknessAnimation
                    {
                        From = from == null ? null : (Thickness)from,
                        To = (Thickness)to!,
                        Duration = duration,
                        EasingFunction = easing
                    };
                case PropertyKeys.Background:
                case PropertyKeys.Foreground:
                    return new ColorAnimation
                    {
                        From = from == null ? null : ExtractColor(from),
                        To = ExtractColor(to),
                        Duration = duration,
                        EasingFunction = easing
                    };
                default:
                    return null;
            }
        }

        public static Color ToWpfColor(ColorValue color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static ColorValue FromDrawingColor(System.Drawing.Color color)
        {
            return ColorValue.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static FontWeightValue FromWpfFontWeight(FontWeight fontWeight)
        {
            return new FontWeightValue(fontWeight.ToOpenTypeWeight());
        }

        public static BrushValue FromWpfBrush(Brush brush)
        {
            switch (brush)
            {
                case SolidColorBrush solid:
                    return new BrushValue.Solid(FromWpfColor(solid.Color));
                case LinearGradientBrush linearGradient:
                    return FromWpfLinearGradientBrush(linearGradient);
                default:
                    throw new NotSupportedException($"Brush type '{brush.GetType().Name}' is not supported by Core brush values.");
            }
        }

        public static DropShadowRenderingBiasValue FromWpfRenderingBias(RenderingBias renderingBias)
        {
            switch (renderingBias)
            {
                case RenderingBias.Quality:
                    return DropShadowRenderingBiasValue.Quality;
                default:
                    return DropShadowRenderingBiasValue.Performance;
            }
        }

        public static CursorValue FromWpfCursor(Cursor cursor)
        {
            if (cursor == Cursors.None)
                return new CursorValue(CursorKind.None);
            if (cursor == Cursors.Arrow)
                return new CursorValue(CursorKind.Arrow);
            if (cursor == Cursors.AppStarting)
                return new CursorValue(CursorKind.AppStarting);
            if (cursor == Cursors.Cross)
                return new CursorValue(CursorKind.Cross);
            if (cursor == Cursors.Hand)
                return new CursorValue(CursorKind.Hand);
            if (cursor == Cursors.Help)
                return new CursorValue(CursorKind.Help);
            if (cursor == Cursors.IBeam)
                return new CursorValue(CursorKind.IBeam);
            if (cursor == Cursors.No)
                return new CursorValue(CursorKind.No);
            if (cursor == Cursors.Pen)
                return new CursorValue(CursorKind.Pen);
            if (cursor == Cursors.ScrollAll)
                return new CursorValue(CursorKind.ScrollAll);
            if (cursor == Cursors.ScrollE)
                return new CursorValue(CursorKind.ScrollE);
            if (cursor == Cursors.ScrollN)
                return new CursorValue(CursorKind.ScrollN);
            if (cursor == Cursors.ScrollNE)
                return new CursorValue(CursorKind.ScrollNE);
            if (cursor == Cursors.ScrollNS)
                return new CursorValue(CursorKind.ScrollNS);
            if (cursor == Cursors.ScrollNW)
                return new CursorValue(CursorKind.ScrollNW);
            if (cursor == Cursors.ScrollS)
                return new CursorValue(CursorKind.ScrollS);
            if (cursor == Cursors.ScrollSE)
                return new CursorValue(CursorKind.ScrollSE);
            if (cursor == Cursors.ScrollSW)
                return new CursorValue(CursorKind.ScrollSW);
            if (cursor == Cursors.ScrollW)
                return new CursorValue(CursorKind.ScrollW);
            if (cursor == Cursors.ScrollWE)
                return new CursorValue(CursorKind.ScrollWE);
            if (cursor == Cursors.SizeAll)
                return new CursorValue(CursorKind.SizeAll);
            if (cursor == Cursors.SizeNESW)
                return new CursorValue(CursorKind.SizeNESW);
            if (cursor == Cursors.SizeNS)
                return new CursorValue(CursorKind.SizeNS);
            if (cursor == Cursors.SizeNWSE)
                return new CursorValue(CursorKind.SizeNWSE);
            if (cursor == Cursors.SizeWE)
                return new CursorValue(CursorKind.SizeWE);
            if (cursor == Cursors.UpArrow)
                return new CursorValue(CursorKind.UpArrow);
            if (cursor == Cursors.Wait)
                return new CursorValue(CursorKind.Wait);

            throw new NotSupportedException($"Cursor '{cursor}' is not supported by Core cursor values.");
        }

        public static System.Windows.GridLength ToWpfGridLength(LengthValue length)
        {
            switch (length.Unit)
            {
                case LengthUnit.Auto:
                    return System.Windows.GridLength.Auto;
                case LengthUnit.Star:
                    return new System.Windows.GridLength(length.Value, System.Windows.GridUnitType.Star);
                default:
                    return new System.Windows.GridLength(length.Value);
            }
        }

        private static Cursor ToWpfCursor(CursorValue cursor)
        {
            switch (cursor.Kind)
            {
                case CursorKind.None:
                    return Cursors.None;
                case CursorKind.AppStarting:
                    return Cursors.AppStarting;
                case CursorKind.Cross:
                    return Cursors.Cross;
                case CursorKind.Hand:
                    return Cursors.Hand;
                case CursorKind.Help:
                    return Cursors.Help;
                case CursorKind.IBeam:
                    return Cursors.IBeam;
                case CursorKind.No:
                    return Cursors.No;
                case CursorKind.Pen:
                    return Cursors.Pen;
                case CursorKind.ScrollAll:
                    return Cursors.ScrollAll;
                case CursorKind.ScrollE:
                    return Cursors.ScrollE;
                case CursorKind.ScrollN:
                    return Cursors.ScrollN;
                case CursorKind.ScrollNE:
                    return Cursors.ScrollNE;
                case CursorKind.ScrollNS:
                    return Cursors.ScrollNS;
                case CursorKind.ScrollNW:
                    return Cursors.ScrollNW;
                case CursorKind.ScrollS:
                    return Cursors.ScrollS;
                case CursorKind.ScrollSE:
                    return Cursors.ScrollSE;
                case CursorKind.ScrollSW:
                    return Cursors.ScrollSW;
                case CursorKind.ScrollW:
                    return Cursors.ScrollW;
                case CursorKind.ScrollWE:
                    return Cursors.ScrollWE;
                case CursorKind.SizeAll:
                    return Cursors.SizeAll;
                case CursorKind.SizeNESW:
                    return Cursors.SizeNESW;
                case CursorKind.SizeNS:
                    return Cursors.SizeNS;
                case CursorKind.SizeNWSE:
                    return Cursors.SizeNWSE;
                case CursorKind.SizeWE:
                    return Cursors.SizeWE;
                case CursorKind.UpArrow:
                    return Cursors.UpArrow;
                case CursorKind.Wait:
                    return Cursors.Wait;
                case CursorKind.Arrow:
                default:
                    return Cursors.Arrow;
            }
        }

        private static LinearGradientBrush ToWpfLinearGradientBrush(BrushValue.LinearGradient gradient)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = ToWpfPoint(gradient.StartPoint),
                EndPoint = ToWpfPoint(gradient.EndPoint)
            };

            foreach (var stop in gradient.Stops)
                brush.GradientStops.Add(new GradientStop(ToWpfColor(stop.Color), stop.Offset));

            return brush;
        }

        private static BrushValue.LinearGradient FromWpfLinearGradientBrush(LinearGradientBrush brush)
        {
            var stops = new List<GradientStopValue>();
            foreach (var stop in brush.GradientStops)
                stops.Add(new GradientStopValue(FromWpfColor(stop.Color), stop.Offset));

            return new BrushValue.LinearGradient(FromWpfPoint(brush.StartPoint), FromWpfPoint(brush.EndPoint), stops);
        }

        private static Point ToWpfPoint(GradientPointValue point)
        {
            return new Point(point.X, point.Y);
        }

        private static GradientPointValue FromWpfPoint(Point point)
        {
            return new GradientPointValue(point.X, point.Y);
        }

        public static ColorValue FromWpfColor(Color color)
        {
            return ColorValue.FromArgb(color.A, color.R, color.G, color.B);
        }

        private static DropShadowEffect ToWpfDropShadowEffect(DropShadowEffectValue value)
        {
            return new DropShadowEffect
            {
                Color = ToWpfColor(value.Color),
                BlurRadius = value.BlurRadius,
                ShadowDepth = value.ShadowDepth,
                Opacity = value.Opacity,
                Direction = value.Direction,
                RenderingBias = ToWpfRenderingBias(value.RenderingBias)
            };
        }

        private static RenderingBias ToWpfRenderingBias(DropShadowRenderingBiasValue renderingBias)
        {
            switch (renderingBias)
            {
                case DropShadowRenderingBiasValue.Quality:
                    return RenderingBias.Quality;
                default:
                    return RenderingBias.Performance;
            }
        }

        private static HorizontalAlignment ToWpfHorizontalAlignment(HorizontalAlignmentValue alignment)
        {
            switch (alignment.Kind)
            {
                case LayoutAlignmentKind.Center:
                    return HorizontalAlignment.Center;
                case LayoutAlignmentKind.End:
                    return HorizontalAlignment.Right;
                case LayoutAlignmentKind.Stretch:
                    return HorizontalAlignment.Stretch;
                default:
                    return HorizontalAlignment.Left;
            }
        }

        private static VerticalAlignment ToWpfVerticalAlignment(VerticalAlignmentValue alignment)
        {
            switch (alignment.Kind)
            {
                case LayoutAlignmentKind.Center:
                    return VerticalAlignment.Center;
                case LayoutAlignmentKind.End:
                    return VerticalAlignment.Bottom;
                case LayoutAlignmentKind.Stretch:
                    return VerticalAlignment.Stretch;
                default:
                    return VerticalAlignment.Top;
            }
        }

        private static BitmapScalingMode ToWpfBitmapScalingMode(ImageScalingModeValue scalingMode)
        {
            switch (scalingMode.Kind)
            {
                case ImageScalingModeKind.HighQuality:
                    return BitmapScalingMode.HighQuality;
                case ImageScalingModeKind.Fant:
                    return BitmapScalingMode.Fant;
                case ImageScalingModeKind.NearestNeighbor:
                    return BitmapScalingMode.NearestNeighbor;
                default:
                    return BitmapScalingMode.LowQuality;
            }
        }

        private static IEasingFunction? ToWpfEasing(EasingValue? easing)
        {
            if (easing == null)
                return null;

            EasingFunctionBase function;
            switch (easing.Kind)
            {
                case EasingKind.Cubic:
                default:
                    function = new CubicEase();
                    break;
            }

            function.EasingMode = ToWpfEasingMode(easing.Mode);
            return function;
        }

        private static EasingMode ToWpfEasingMode(EasingModeValue mode)
        {
            switch (mode)
            {
                case EasingModeValue.Out:
                    return EasingMode.EaseOut;
                case EasingModeValue.InOut:
                    return EasingMode.EaseInOut;
                default:
                    return EasingMode.EaseIn;
            }
        }

        private static Color ExtractColor(object? value)
        {
            switch (value)
            {
                case Color color:
                    return color;
                case SolidColorBrush brush:
                    return brush.Color;
                default:
                    throw new InvalidOperationException("Color animation target must be a Color or SolidColorBrush value.");
            }
        }
    }
}
