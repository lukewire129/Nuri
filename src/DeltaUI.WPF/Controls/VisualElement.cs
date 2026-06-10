namespace DeltaUI.WPF
{
    public class VisualElement : Element
    {
        public VisualElement()
        {
        }

        public VisualElement(string nodeType) : base(nodeType)
        {
        }

        public VisualElement(string nodeType, string kind) : base(nodeType, kind)
        {
        }

        public override int GetHashCode()
        {
            int hash = System.HashCode.Combine(Type, Kind);
            hash = (hash * 397) ^ Properties.GetHashCode ();
            foreach (var child in Children)
            {
                hash = (hash * 397) ^ child.GetHashCode ();
            }
            return hash;
        }
    }

    public static class UniqueIdGenerator
    {
        private static long _currentId = 0;

        public static string GenerateId()
        {
            return $"node_{System.Threading.Interlocked.Increment (ref _currentId)}";
        }
    }
}
