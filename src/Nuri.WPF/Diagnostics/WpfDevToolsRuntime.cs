using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Nuri.VirtualDom;

namespace Nuri.WPF.Diagnostics
{
    public static class WpfDevToolsRuntime
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, RootRegistration> Roots = new Dictionary<string, RootRegistration>();

        public static void RegisterRoot(
            string rootId,
            Func<FrameworkElement?> getRootElement,
            Func<VirtualEntry?> getCurrentVirtualEntry)
        {
            if (string.IsNullOrWhiteSpace(rootId))
                return;

            if (getRootElement == null)
                throw new ArgumentNullException(nameof(getRootElement));

            if (getCurrentVirtualEntry == null)
                throw new ArgumentNullException(nameof(getCurrentVirtualEntry));

            lock (SyncRoot)
            {
                Roots[rootId] = new RootRegistration(rootId, getRootElement, getCurrentVirtualEntry);
            }
        }

        public static void UnregisterRoot(string rootId)
        {
            lock (SyncRoot)
            {
                Roots.Remove(rootId);
            }
        }

        public static bool TryFindElement(string componentId, out FrameworkElement? element)
        {
            element = null;

            if (string.IsNullOrWhiteSpace(componentId))
                return false;

            RootRegistration[] roots;
            lock (SyncRoot)
            {
                roots = Roots.Values.ToArray();
            }

            foreach (var root in roots)
            {
                var rootElement = root.GetRootElement();
                var currentEntry = root.GetCurrentVirtualEntry();
                if (rootElement == null || currentEntry == null)
                    continue;

                var entry = currentEntry.FindByComponentId(componentId) ?? currentEntry.FindById(componentId);
                if (entry == null)
                    continue;

                element = WpfVirtualEntryRenderer.FindElementById(rootElement, entry.Id);
                if (element != null)
                    return true;
            }

            return false;
        }

        private sealed class RootRegistration
        {
            public RootRegistration(
                string rootId,
                Func<FrameworkElement?> getRootElement,
                Func<VirtualEntry?> getCurrentVirtualEntry)
            {
                RootId = rootId;
                GetRootElement = getRootElement;
                GetCurrentVirtualEntry = getCurrentVirtualEntry;
            }

            public string RootId { get; }

            public Func<FrameworkElement?> GetRootElement { get; }

            public Func<VirtualEntry?> GetCurrentVirtualEntry { get; }
        }
    }
}
