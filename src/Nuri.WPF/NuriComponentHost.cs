using System;
using System.Windows.Controls;
using Nuri.Runtime;
using Nuri.UI.Dsl;

namespace Nuri.WPF
{
    public class NuriComponentHost<TComponent> : ContentControl
        where TComponent : Component, new()
    {
        private ApplicationRoot? _root;

        public NuriServiceProvider Services { get; set; } = NuriApplication.CreateDefaultServices();

        public ApplicationRoot Root => _root ?? throw new InvalidOperationException("The Nuri component host is not attached.");

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            Attach();
            Unloaded += OnHostUnloaded;
        }

        public void Attach()
        {
            if (_root != null)
                return;

            _root = NuriApplication.Attach(this, new TComponent(), Services);
        }

        private void OnHostUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Unloaded -= OnHostUnloaded;
            _root?.Dispose();
            _root = null;
        }
    }
}
