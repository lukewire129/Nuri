using DeltaUI.Core.UI.Values;

namespace DeltaUI.WPF
{
    readonly public partial struct GridLength
    {
        internal readonly LengthValue Value;

        public GridLength(LengthValue value) => Value = value;

        public GridLength(System.Windows.GridLength value)
        {
            if (value.IsAuto)
            {
                Value = LengthValue.Auto();
            }
            else if (value.IsStar)
            {
                Value = LengthValue.Star(value.Value);
            }
            else
            {
                Value = LengthValue.Pixels(value.Value);
            }
        }

        public static implicit operator System.Windows.GridLength(GridLength value) => (System.Windows.GridLength)WpfValueMapper.ToWpfValue(value.Value)!;
        public static implicit operator GridLength(System.Windows.GridLength value) => new (value);

        public static implicit operator GridLength(double value) => new GridLength(LengthValue.Pixels(value));
    }
}
