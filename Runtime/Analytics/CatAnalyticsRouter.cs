using System;
using System.Collections;
using System.Collections.Generic;

namespace ActionFit.Cat.App
{
    /// <summary>Project Shell leaf for ThinkingData readiness and tracking.</summary>
    public abstract class CatAnalyticsPrimaryDestinationBase
    {
        public abstract bool IsReady { get; }
        public abstract void Track(string eventName, IReadOnlyDictionary<string, object> properties);
    }

    /// <summary>Project Shell leaf for the current Singular mirror behavior.</summary>
    public abstract class CatAnalyticsMirrorDestinationBase
    {
        public abstract void Track(string eventName, IReadOnlyDictionary<string, object> properties);
    }

    /// <summary>Owns Cat TD-first routing, readiness drop, and Singular reward flattening.</summary>
    public sealed class CatAnalyticsRouter
    {
        private readonly CatAnalyticsPrimaryDestinationBase _primary;
        private readonly CatAnalyticsMirrorDestinationBase _mirror;

        public CatAnalyticsRouter(
            CatAnalyticsPrimaryDestinationBase primary,
            CatAnalyticsMirrorDestinationBase mirror)
        {
            _primary = primary ?? throw new ArgumentNullException(nameof(primary));
            _mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));
        }

        public bool Track(
            string eventName,
            IReadOnlyDictionary<string, object> properties,
            bool flattenRewardForMirror = false)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException("An analytics event name is required.", nameof(eventName));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
            if (!_primary.IsReady)
                return false;

            _primary.Track(eventName, properties);

            var mirrorProperties = new Dictionary<string, object>(properties.Count);
            foreach (KeyValuePair<string, object> property in properties)
                mirrorProperties.Add(property.Key, property.Value);
            if (flattenRewardForMirror)
                FlattenRewardInfo(mirrorProperties);
            _mirror.Track(eventName, mirrorProperties);
            return true;
        }

        public static void FlattenRewardInfo(IDictionary<string, object> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            int gold = 0;
            int energy = 0;
            int dia = 0;
            string itemId = string.Empty;

            if (properties.TryGetValue("reward_info", out object rawRows)
                && rawRows is IEnumerable rows)
            {
                foreach (object rawRow in rows)
                {
                    if (!TryReadRow(rawRow, out string type, out string rowItemId, out int amount))
                        continue;

                    switch (type)
                    {
                        case "gold":
                            gold += amount;
                            break;
                        case "energy":
                            energy += amount;
                            break;
                        case "dia":
                            dia += amount;
                            break;
                        case "item" when string.IsNullOrEmpty(itemId):
                            itemId = rowItemId;
                            break;
                    }
                }
            }

            properties.Remove("reward_info");
            properties["reward_gold"] = gold;
            properties["reward_energy"] = energy;
            properties["reward_dia"] = dia;
            properties["reward_item_id"] = itemId;
        }

        private static bool TryReadRow(
            object rawRow,
            out string type,
            out string itemId,
            out int amount)
        {
            type = string.Empty;
            itemId = string.Empty;
            amount = 0;

            if (rawRow is not IDictionary<string, object> row)
                return false;

            if (row.TryGetValue("type", out object rawType))
                type = rawType?.ToString() ?? string.Empty;
            if (row.TryGetValue("item_id", out object rawItemId))
                itemId = rawItemId?.ToString() ?? string.Empty;
            if (row.TryGetValue("amount", out object rawAmount) && rawAmount != null)
                amount = Convert.ToInt32(rawAmount);
            return true;
        }
    }
}
