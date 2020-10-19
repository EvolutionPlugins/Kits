using System;
using System.Collections.Generic;

namespace Kits.Models
{
    [Serializable]
    public class KitsCooldownData
    {
        public KitsCooldownData()
        {
            KitsCooldown = new Dictionary<string, DateTime>();
        }

        public Dictionary<string, DateTime> KitsCooldown { get; set; }
    }
}
