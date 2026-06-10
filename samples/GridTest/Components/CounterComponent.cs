using DeltaUI.WPF;
using System.Drawing;

namespace GridTest.Components
{
    public class CounterComponent : Component
    {
        public override IVisual Render()
        {
            var (count, setCount) = useState(0);
            var (text, setText) = useState("");
            var (checkedValue, setCheckedValue) = useState(false);
            var (radioValue, setRadioValue) = useState(false);

            return Div(
                    Rows(Auto, Auto, Auto, Auto),
                    Div(DivTypes.Row,
                        Input(InputTypes.Primary, $"Count: {count}")
                            .Key("count-increment")
                            .OnClick(() => setCount(count + 1))
                            .Size(120, 50)
                            .Background("#e8b8FFFF"),
                        Input(InputTypes.Button, "Reset")
                            .Key("count-reset")
                            .OnClick(() => setCount(0))
                            .Margin(left: 12)
                    ).Row(0),

                    Div(DivTypes.Column,
                        Text($"TextChanged: {text}")
                            .FontSize(20)
                            .FontColor(Color.Red),
                        Input(InputTypes.Text)
                            .OnTextChanged(setText)
                            .FontColor(Color.Blue)
                            .Size(160, 50)
                    ).Row(1),

                    Div(DivTypes.Column,
                        Text($"Checkbox: {checkedValue}")
                            .FontSize(20)
                            .FontColor(Color.Red),
                        Input(InputTypes.Checkbox)
                            .OnCheckChanged(setCheckedValue)
                            .Size(100, 50)
                    ).Row(2),

                    Div(DivTypes.Column,
                        Input(InputTypes.Radio, $"Radio 1: {radioValue}")
                            .OnCheckChanged(setRadioValue),
                        Input(InputTypes.Radio, $"Radio 2: {!radioValue}")
                            .OnCheckChanged(value => setRadioValue(!value))
                    ).Row(3)
                )
                .Padding(12);
        }
    }
}
