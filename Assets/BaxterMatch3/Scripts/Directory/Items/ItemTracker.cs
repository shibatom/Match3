using System.Collections.Generic;

namespace Internal.Scripts.Items
{
    public static class ItemTracker
    {
        public static HashSet<Item> ProcessedItems = new HashSet<Item>();
    }
}