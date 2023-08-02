using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenMod.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore.Configurator;
using System;

namespace Kits.Cooldowns.MySql;

// Due to how EF Core works, we cannot change the table name in runtime
// so if user want to store cooldowns in MySQL, but each server should use the own set of cooldowns then
// the user should create another DBs (let's say Server1, Server2) and set the connection string in configuration.
// Of course if user wants only global cooldowns then no configuration needed.
[ConnectionString("cooldown")]
public class KitCooldownsDbContext : OpenModDbContext<KitCooldownsDbContext>
{
    public DbSet<KitCooldown> KitCooldowns => Set<KitCooldown>();

    public KitCooldownsDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public KitCooldownsDbContext(IDbContextConfigurator configurator, IServiceProvider serviceProvider) : base(configurator, serviceProvider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var property = modelBuilder
            .Entity<KitCooldown>()
            .Property(x => x.UsedTime);

        // adding conversion to save DateTime as UTC and get back as UTC
        property.HasConversion(dateTimeConverter);
    }
}
