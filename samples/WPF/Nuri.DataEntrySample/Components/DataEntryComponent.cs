using Nuri.UI.Controls;
using Nuri.UI.Dsl;
using Nuri.UI.Values;

namespace Nuri.DataEntrySample.Components;

public sealed class DataEntryComponent : Component
{
    public override IElement Render()
    {
        var (state, setState) = useState(new FormState("", "", "", false, "email", false, Array.Empty<string>()));
        var stateRef = useLatest(state);

        void Update(Func<FormState, FormState> change)
        {
            var next = change(stateRef.Current);
            stateRef.Current = next;
            setState(_ => next);
        }

        void Submit()
        {
            var errors = Validate(stateRef.Current);
            Update(current => current with { Submitted = true, Errors = errors });
        }

        void Reset()
        {
            Update(_ => new FormState("", "", "", false, "email", false, Array.Empty<string>()));
        }

        return Grid(Rows(Auto, Star),
                Header().Row(0),
                Div(DivTypes.Scroll,
                    Section("Account",
                        Label("Name"),
                        TextBox(state.Name, value => Update(current => current with { Name = value }))
                            .Key("name")
                            .Height(36)
                            .Padding(10, 0, 10, 0)
                            .TextStart()
                            .TextVCenter(),
                        Label("Email"),
                        TextBox(state.Email, value => Update(current => current with { Email = value }))
                            .Key("email")
                            .Height(36)
                            .Padding(10, 0, 10, 0)
                            .TextStart()
                            .TextVCenter(),
                        Label("Password"),
                        TextBox(state.Password, value => Update(current => current with { Password = value }))
                            .Key("password")
                            .Height(36)
                            .Padding(10, 0, 10, 0)
                            .TextStart()
                            .TextVCenter()),
                    Section("Preferences",
                        CheckBox("Accept terms", value => Update(current => current with { AcceptTerms = value }))
                            .Checked(state.AcceptTerms)
                            .Margin(bottom: 12),
                        Text("Contact method").FontSize(12).FontColor("#374151").Margin(bottom: 8),
                        Div(DivTypes.Row,
                            RadioButton("Email", selected => { if (selected) Update(current => current with { ContactMethod = "email" }); })
                                .Group("contact")
                                .Checked(state.ContactMethod == "email")
                                .Margin(right: 18),
                            RadioButton("Phone", selected => { if (selected) Update(current => current with { ContactMethod = "phone" }); })
                                .Group("contact")
                                .Checked(state.ContactMethod == "phone")),
                        Text("Dropdown render sanity").FontSize(12).FontColor("#374151").Margin(top: 16, bottom: 8),
                        Select(Text("Small"), Text("Medium"), Text("Large")).Height(34)),
                    Section("Validation",
                        state.Submitted && state.Errors.Length > 0
                            ? Div(state.Errors.Select(error => (IElement)Text("- " + error).FontColor("#be123c").Margin(bottom: 6)).ToArray())
                            : Text(state.Submitted ? "Submit passed." : "Submit to validate.").FontColor(state.Submitted ? "#047857" : "#6b7280"),
                        Grid(
                                Button("Reset", Reset).Height(36).Column(0),
                                Button("Submit", Submit).Height(36).Background("#111827").FontColor("#ffffff").Brush("#111827").Thickness(1).Column(1)
                            )
                            .Columns(Star, Pixels(100))
                            .Margin(top: 16)))
                    .Row(1))
            .Padding(24)
            .Background("#f3f4f6");
    }

    private static IElement Header()
    {
        return Div(
                Text("Data Entry / Form").FontSize(26).FontWeight(FontWeightValue.Bold).FontColor("#111827"),
                Text("text input, password-like input, dropdown render, validation, submit/reset 검증").FontSize(13).FontColor("#6b7280").Margin(top: 6, bottom: 18));
    }

    private static IElement Label(string text)
    {
        return Text(text).FontSize(12).FontColor("#374151").Margin(top: 12, bottom: 6);
    }

    private static IElement Section(string title, params IElement[] children)
    {
        return Div(new[] { Text(title).FontSize(17).FontWeight(FontWeightValue.Bold).FontColor("#111827").Margin(bottom: 8) }.Concat(children).ToArray())
            .Padding(18)
            .Margin(bottom: 14, right: 8)
            .Background("#ffffff")
            .Brush("#e5e7eb")
            .Thickness(1)
            .CornerRadius(16);
    }

    private static string[] Validate(FormState state)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(state.Name)) errors.Add("Name is required.");
        if (!state.Email.Contains('@')) errors.Add("Email must contain @.");
        if (state.Password.Length < 6) errors.Add("Password must be at least 6 characters.");
        if (!state.AcceptTerms) errors.Add("Terms must be accepted.");
        return errors.ToArray();
    }
}

internal sealed record FormState(string Name, string Email, string Password, bool AcceptTerms, string ContactMethod, bool Submitted, string[] Errors);
