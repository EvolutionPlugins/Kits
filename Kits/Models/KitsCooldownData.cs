using System;
using System.Collections.Generic;

namespace Kits.Models
{
    [Serializable]
    public class KitsCooldownData
    {
        public Dictionary<string, List<KitCooldownData>> KitsCooldown { get; set; }
    }
}
