using System;
using System.Collections.Generic;

namespace ActionFit.Cat.App.Order
{
    /// <summary>Preserves one Cat order-completion occurrence without exposing the project Order type.</summary>
    public sealed class CatOrderCompletionSnapshot
    {
        private readonly int[] _itemLevels;

        public CatOrderCompletionSnapshot(object completionIdentity, IEnumerable<int> itemLevels)
        {
            CompletionIdentity = completionIdentity ?? throw new ArgumentNullException(nameof(completionIdentity));
            if (itemLevels == null)
            {
                throw new ArgumentNullException(nameof(itemLevels));
            }

            _itemLevels = new List<int>(itemLevels).ToArray();
        }

        public object CompletionIdentity { get; }
        public IReadOnlyList<int> ItemLevels => _itemLevels;
    }
}
