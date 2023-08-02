using System.Collections.Generic;

namespace Kits.Cooldowns.DataStore;

public class KitsCooldownData
{
    public Dictionary<string, List<KitCooldownData>>? KitsCooldown { get; set; }
}