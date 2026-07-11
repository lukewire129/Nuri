namespace Nuri.SettingsPreferencesSample.Components;

internal enum ThemeChoice
{
    System,
    Light,
    Dark
}

internal enum DensityChoice
{
    Comfortable,
    Compact
}

internal sealed record PreferenceState(
    string ProfileName,
    bool EmailNotifications,
    bool PushNotifications,
    bool WeeklyDigest,
    bool BetaFeatures,
    ThemeChoice Theme,
    DensityChoice Density,
    bool RequireConfirm,
    bool TriedSubmit);
