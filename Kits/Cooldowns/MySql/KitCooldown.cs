using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kits.Cooldowns.MySql;
public sealed class KitCooldown
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string PlayerId { get; set; } = string.Empty;

    [StringLength(25)]
    public string Kit { get; set; } = string.Empty;

    public DateTime UsedTime { get; set; }
}
