using System;
using System.Windows;
using System.Windows.Controls;
using Nuri.Platform.Abstractions;

namespace Nuri.WPF.PreviewHost;

internal sealed class ContentControlHost : IHostAdapter<FrameworkElement>
{
    private readonly ContentControl _contentControl;

    public ContentControlHost(ContentControl contentControl)
    {
        _contentControl = contentControl ?? throw new ArgumentNullException(nameof(contentControl));
    }

    public void SetContent(FrameworkElement root)
    {
        _contentControl.Content = root;
    }
}
