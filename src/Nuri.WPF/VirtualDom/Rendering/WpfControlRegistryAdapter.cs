using Nuri.VirtualDom.Rendering;
using System.Windows;

namespace Nuri.WPF
{
    public sealed class WpfControlRegistryAdapter : IControlRegistry<FrameworkElement>
    {
        public FrameworkElement Create(string type)
        {
            return WpfControlRegistry.Create(type);
        }

        public void AddChild(FrameworkElement parent, FrameworkElement child, int? index = null)
        {
            WpfControlRegistry.AddChild(parent, child, index);
        }

        public void RemoveChild(FrameworkElement parent, FrameworkElement child)
        {
            WpfControlRegistry.RemoveChild(parent, child);
        }

        public void MoveChild(FrameworkElement parent, FrameworkElement child, int newIndex)
        {
            WpfControlRegistry.MoveChild(parent, child, newIndex);
        }

        public void ReplaceChild(FrameworkElement parent, FrameworkElement oldChild, FrameworkElement newChild)
        {
            WpfControlRegistry.ReplaceChild(parent, oldChild, newChild);
        }
    }
}
