using System.Collections.Generic;

namespace Kits.Cooldowns.Models;

public class KitsCooldownData
{
    public Dictionary<string, List<KitCooldownData>>? KitsCooldown { get; set; }
}