using System;
using System.Collections.Generic;

namespace Kits.Models
{
    [Serializable]
    public class KitsCooldownData
    {
        public KitsCooldownData()
        {
            KitsCooldown = new Dictionary<string, Dictionary<string, DateTime>>();
        }

        public Dictionary<string, Dictionary<string, DateTime>> KitsCooldown { get; set; }
    }
}
