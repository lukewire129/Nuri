using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace GridTest.Components
{
    public class CounterComponent : Component
    {
        public override IElement Render()
        {
            var (count, setCount) = useState(0);
            var (text, setText) = useState("");
            var (checkedValue, setCheckedValue) = useState(false);
            var (radioValue, setRadioValue) = useState(false);

            return Grid(
                    Div(DivTypes.Row,
                        Button($"Count: {count}")
                            .Key("count-increment")
                            .OnClick(() => setCount(count + 1))
                            .Size(120, 50)
                            .Background("#e8b8FFFF"),
                        Button("Reset")
                            .Key("count-reset")
                            .OnClick(() => setCount(0))
                            .Margin(12, 0, 0, 0)
                    ).Row(0),

                    Div(DivTypes.Column,
                        Text($"TextChanged: {text}")
                            .FontSize(20)
                            .FontColor(ColorValue.FromRgb(255, 0, 0)),
                        TextBox()
                            .OnTextChanged(setText)
                            .FontColor(ColorValue.FromRgb(0, 0, 255))
                            .Size(160, 50)
                    ).Row(1),

                    Div(DivTypes.Column,
                        Text($"Checkbox: {checkedValue}")
                            .FontSize(20)
                            .FontColor(ColorValue.FromRgb(255, 0, 0)),
                        CheckBox()
                            .OnCheckChanged(setCheckedValue)
                            .Size(100, 50)
                    ).Row(2),

                    Div(DivTypes.Column,
                        RadioButton($"Radio 1: {radioValue}")
                            .OnCheckChanged(setRadioValue),
                        RadioButton($"Radio 2: {!radioValue}")
                            .OnCheckChanged(value => setRadioValue(!value))
                    ).Row(3)
                )
                .Rows(Auto, Auto, Auto, Auto)
                .Padding(12);
        }
    }
}
