namespace Nuri.VirtualDom.Rendering
{
    public interface IControlRegistry<TNative>
    {
        TNative Create(string type);

        void AddChild(TNative parent, TNative child, int? index = null);

        void RemoveChild(TNative parent, TNative child);

        void MoveChild(TNative parent, TNative child, int newIndex);

        void ReplaceChild(TNative parent, TNative oldChild, TNative newChild);
    }
}
