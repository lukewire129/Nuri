namespace DeltaUI.WPF
{
    public class Visual : VisualElement, IVisual
    {
        public Visual(string type) : base(type) { }

        public Visual(string type, string kind) : base(type, kind) { }
    }
}
