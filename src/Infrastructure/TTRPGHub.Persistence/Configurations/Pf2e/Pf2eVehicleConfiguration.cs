using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Configurations.Pf2e;

internal sealed class Pf2eVehicleConfiguration : IEntityTypeConfiguration<Pf2eVehicle>
{
    public void Configure(EntityTypeBuilder<Pf2eVehicle> builder)
    {
        builder.ToTable("pf2e_vehicles");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new Pf2eVehicleId(v));

        builder.Property(v => v.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(v => v.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(v => v.NameRu).HasColumnName("name_ru").HasMaxLength(200).IsRequired();
        builder.Property(v => v.Size).HasColumnName("size").HasMaxLength(50);
        builder.Property(v => v.Price).HasColumnName("price").HasMaxLength(100);
        builder.Property(v => v.Dimensions).HasColumnName("dimensions");
        builder.Property(v => v.Crew).HasColumnName("crew");
        builder.Property(v => v.Passengers).HasColumnName("passengers");
        builder.Property(v => v.PilotingCheck).HasColumnName("piloting_check");
        builder.Property(v => v.Immunities).HasColumnName("immunities");
        builder.Property(v => v.Speed).HasColumnName("speed");
        builder.Property(v => v.Collision).HasColumnName("collision");
        builder.Property(v => v.AbilitiesText).HasColumnName("abilities_text");
        builder.Property(v => v.Source).HasColumnName("source").HasMaxLength(300);

        builder.Property(v => v.Level).HasColumnName("level");
        builder.Property(v => v.ArmorClass).HasColumnName("armor_class");
        builder.Property(v => v.Fortitude).HasColumnName("fortitude");
        builder.Property(v => v.Hardness).HasColumnName("hardness");
        builder.Property(v => v.HitPoints).HasColumnName("hit_points");
        builder.Property(v => v.BrokenThreshold).HasColumnName("broken_threshold");

        builder.HasIndex(v => v.Slug).IsUnique().HasDatabaseName("ix_pf2e_vehicles_slug");
        builder.HasIndex(v => v.Level).HasDatabaseName("ix_pf2e_vehicles_level");
    }
}
