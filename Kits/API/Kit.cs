using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Kits.API
{
    public class Kit
    {
        public string? Name { get; set; }
        public float? Cooldown { get; set; }
        public decimal? Cost { get; set; }
        public decimal? Money { get; set; }
        public string? VehicleId { get; set; }
        public List<KitItem>? Items { get; set; }
    }
}