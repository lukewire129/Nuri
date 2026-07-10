using System.Windows.Input;

namespace Nuri.WPF
{
    public sealed class NuriApplicationOptions
    {
        public DevToolsOptions DevTools { get; } = new DevToolsOptions();

        internal NuriApplicationOptions Clone()
        {
            var clone = new NuriApplicationOptions();
            clone.DevTools.Enabled = DevTools.Enabled;
            clone.DevTools.ToggleKey = DevTools.ToggleKey;
            return clone;
        }
    }

    public sealed class DevToolsOptions
    {
        public DevToolsOptions()
        {
#if DEBUG
            Enabled = true;
#endif
        }

        public bool Enabled { get; set; }

        public Key ToggleKey { get; set; } = Key.F12;
    }

    internal readonly struct DevToolsConfiguration
    {
        public DevToolsConfiguration(bool enabled, Key toggleKey)
        {
            Enabled = enabled;
            ToggleKey = toggleKey;
        }

        public bool Enabled { get; }

        public Key ToggleKey { get; }
    }
}
