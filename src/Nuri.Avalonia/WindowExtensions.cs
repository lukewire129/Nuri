using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;

namespace Nuri.Avalonia
{
    /// <summary>
    /// Fluent helpers for configuring Avalonia windows before they are shown.
    /// </summary>
    public static class WindowExtensions
    {
        public static Window WithTransparency(
            this Window window,
            params WindowTransparencyLevel[] levels)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (levels == null)
                throw new ArgumentNullException(nameof(levels));

            window.TransparencyLevelHint = levels;
            return window;
        }

        public static Window WithTransparentBackground(this Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.Background = Brushes.Transparent;
            return window.WithTransparency(WindowTransparencyLevel.Transparent);
        }

        public static Window ExtendClientAreaIntoTitleBar(this Window window, bool enabled = true)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.ExtendClientAreaToDecorationsHint = enabled;
            return window;
        }

        public static Window WithTitleBarHeight(this Window window, double height)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (height < -1)
                throw new ArgumentOutOfRangeException(nameof(height), height, "The title bar height must be -1 (default) or non-negative.");

            window.ExtendClientAreaTitleBarHeightHint = height;
            return window;
        }

        public static Window WithChromeHints(this Window window, ExtendClientAreaChromeHints hints)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.ExtendClientAreaChromeHints = hints;
            return window;
        }

        public static Window WithSystemDecorations(this Window window, SystemDecorations decorations)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.SystemDecorations = decorations;
            return window;
        }

        public static Window WithResize(this Window window, bool canResize = true)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.CanResize = canResize;
            return window;
        }

        public static Window WithTopmost(this Window window, bool topmost = true)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.Topmost = topmost;
            return window;
        }

        public static Window WithTaskbarVisibility(this Window window, bool showInTaskbar = true)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.ShowInTaskbar = showInTaskbar;
            return window;
        }

        public static Window WithWindowState(this Window window, WindowState state)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.WindowState = state;
            return window;
        }

        public static Window WithMinimumSize(this Window window, double width, double height)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "The minimum width cannot be negative.");
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "The minimum height cannot be negative.");

            window.MinWidth = width;
            window.MinHeight = height;
            return window;
        }

        public static Window WithMaximumSize(this Window window, double width, double height)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "The maximum width cannot be negative.");
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "The maximum height cannot be negative.");

            window.MaxWidth = width;
            window.MaxHeight = height;
            return window;
        }
    }
}
