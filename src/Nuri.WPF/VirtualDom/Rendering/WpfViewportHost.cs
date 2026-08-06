using System.Windows.Controls;
using System.Windows.Media;

namespace Nuri.WPF;

internal sealed class WpfViewportHost : Canvas
{
    private readonly MatrixTransform _cameraTransform = new();
    private double _offsetX;
    private double _offsetY;
    private double _zoom = 1;

    public WpfViewportHost()
    {
        RenderTransform = _cameraTransform;
    }

    public double OffsetX
    {
        get => _offsetX;
        set
        {
            if (_offsetX == value)
                return;

            _offsetX = value;
            UpdateTransform();
        }
    }

    public double OffsetY
    {
        get => _offsetY;
        set
        {
            if (_offsetY == value)
                return;

            _offsetY = value;
            UpdateTransform();
        }
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            if (_zoom == value)
                return;

            _zoom = value;
            UpdateTransform();
        }
    }

    private void UpdateTransform()
    {
        _cameraTransform.Matrix = new Matrix(
            _zoom,
            0,
            0,
            _zoom,
            -_offsetX * _zoom,
            -_offsetY * _zoom);
    }
}
