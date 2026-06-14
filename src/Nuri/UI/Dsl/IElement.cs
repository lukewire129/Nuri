using Nuri.UI.Values;

namespace Nuri.UI.Dsl
{
    public interface IElement : UI.IElement<IElement, AnimationValue>
    {
    }

    public interface IVisual : IElement
    {
    }

    public interface IContent : IVisual
    {
    }

    public interface IInput : IVisual
    {
    }

    public interface IDiv : IVisual
    {
    }

    public interface IImage : IVisual
    {
    }

    public interface IItems : IVisual
    {
    }

    public interface IOverlay : IVisual
    {
    }

    public interface ISelect : IVisual
    {
    }
}
