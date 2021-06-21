using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;

namespace Kits.API
{
    public class Kit
    {
        [YamlIgnore]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(25)]
        public string? Name { get; set; }

        public float Cooldown { get; set; }

        public decimal Cost { get; set; }

        public decimal Money { get; set; }

        [StringLength(5)]
        public string? VehicleId { get; set; }

        public IList<KitItem>? Items { get; set; }
    }
}