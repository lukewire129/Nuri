using System.Collections.Generic;

namespace Nuri.Runtime.Diagnostics
{
    public sealed class StoreSnapshot
    {
        public StoreSnapshot(
            string storeId,
            string storeType,
            string valueSummary,
            IReadOnlyList<StoreSubscriptionSnapshot> subscriptions)
        {
            StoreId = storeId;
            StoreType = storeType;
            ValueSummary = valueSummary;
            Subscriptions = subscriptions;
        }

        public string StoreId { get; }

        public string StoreType { get; }

        public string ValueSummary { get; }

        public IReadOnlyList<StoreSubscriptionSnapshot> Subscriptions { get; }
    }

    public sealed class StoreSubscriptionSnapshot
    {
        public StoreSubscriptionSnapshot(
            string storeId,
            string storeType,
            string componentId,
            int hookIndex,
            string selectedType,
            string selectedValueSummary)
        {
            StoreId = storeId;
            StoreType = storeType;
            ComponentId = componentId;
            HookIndex = hookIndex;
            SelectedType = selectedType;
            SelectedValueSummary = selectedValueSummary;
        }

        public string StoreId { get; }

        public string StoreType { get; }

        public string ComponentId { get; }

        public int HookIndex { get; }

        public string SelectedType { get; }

        public string SelectedValueSummary { get; }
    }
}
