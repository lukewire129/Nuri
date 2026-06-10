using System.Collections.Generic;

namespace DeltaUI.WPF
{

    public interface IVisual : IElement
    {
    }

    public interface IContent : IVisual
    {

    }
    public interface IInput : IVisual
    {
    }

    public interface IScroll : IVisual
    {
    }
}
