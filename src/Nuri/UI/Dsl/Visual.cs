namespace Nuri.UI.Dsl
{
    public class Visual : Element, IVisual
    {
        public Visual(string type) : base(type)
        {
        }

        public Visual(string type, string kind) : base(type, kind)
        {
        }
    }
}
