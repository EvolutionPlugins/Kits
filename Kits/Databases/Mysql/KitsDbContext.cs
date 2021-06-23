using Kits.API;
using Kits.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenMod.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore.Configurator;
using System;

namespace Kits.Databases.Mysql
{
    public class KitsDbContext : OpenModDbContext<KitsDbContext>
    {
        public DbSet<Kit> Kits
        {
            get
            {
                return Set<Kit>();
            }
        }

        public KitsDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public KitsDbContext(IDbContextConfigurator configurator, IServiceProvider serviceProvider) : base(configurator, serviceProvider)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new KitConfiguration());
        }

        // https://stackoverflow.com/questions/44829824/how-to-store-json-in-an-entity-field-with-ef-core
        public class KitConfiguration : IEntityTypeConfiguration<Kit>
        {
            public virtual void Configure(EntityTypeBuilder<Kit> builder)
            {
                builder.Property(c => c.Items).HasByteArrayConversion();
            }
        }
    }
}
