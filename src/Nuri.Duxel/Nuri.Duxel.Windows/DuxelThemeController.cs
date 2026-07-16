using Duxel.Core;

namespace Nuri.Duxel;

public sealed class DuxelThemeController
{
    private Action<UiTheme>? _requestTheme;
    private readonly object _themeGate = new();
    private UiTheme _currentTheme;
    private bool _hasCurrentTheme;

    public bool IsAttached => Volatile.Read(ref _requestTheme) is not null;

    public event Action<UiTheme>? CurrentThemeChanged;

    public UiTheme? CurrentTheme
    {
        get
        {
            lock (_themeGate)
            {
                return _hasCurrentTheme ? _currentTheme : null;
            }
        }
    }

    public bool RequestTheme(UiTheme theme)
    {
        var requestTheme = Volatile.Read(ref _requestTheme);
        if (requestTheme is null)
        {
            return false;
        }

        requestTheme(theme);
        return true;
    }

    internal void Attach(Action<UiTheme> requestTheme)
    {
        ArgumentNullException.ThrowIfNull(requestTheme);
        if (Interlocked.CompareExchange(ref _requestTheme, requestTheme, null) is not null)
        {
            throw new InvalidOperationException("The Duxel theme controller is already attached to a host.");
        }
    }

    internal void Detach(Action<UiTheme> requestTheme)
    {
        Interlocked.CompareExchange(ref _requestTheme, null, requestTheme);
    }

    internal void ObserveTheme(UiTheme theme)
    {
        Action<UiTheme>? currentThemeChanged = null;
        lock (_themeGate)
        {
            if (_hasCurrentTheme && ThemesEqual(_currentTheme, theme))
            {
                return;
            }

            _currentTheme = theme;
            _hasCurrentTheme = true;
            currentThemeChanged = CurrentThemeChanged;
        }

        currentThemeChanged?.Invoke(theme);
    }

    private static bool ThemesEqual(UiTheme left, UiTheme right)
    {
        for (var index = 0; index < UiThemeColors.StyleColorCount; index++)
        {
            var color = (UiStyleColor)index;
            if (left[color] != right[color])
            {
                return false;
            }
        }

        return true;
    }
}
