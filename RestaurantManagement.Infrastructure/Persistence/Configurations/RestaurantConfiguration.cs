
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantManagement.Domain.Entities;

namespace RestaurantManagement.Infrastructure.Persistence.Configurations;

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        // Database table name
        builder.ToTable("Restaurants");

        // Primary key
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        // Restaurant name
        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(150);

        // City
        builder.Property(r => r.City)
            .IsRequired()
            .HasMaxLength(100);

        // Area
        builder.Property(r => r.Area)
            .IsRequired()
            .HasMaxLength(100);

        // Optional cuisine
        builder.Property(r => r.Cuisine)
            .HasMaxLength(100);

        // Delivery time
        builder.Property(r => r.DeliveryTimeMinutes)
            .IsRequired();

        // Active / deleted status
        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.Property(r => r.IsDeleted)
            .IsRequired();

        // Timestamps
        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .IsRequired(false);

        // Index for common restaurant searches
        builder.HasIndex(r => new { r.City, r.Area });

        // Exclude soft-deleted restaurants by default
        builder.HasQueryFilter(r => !r.IsDeleted);

        // Database-level check constraints
        builder.ToTable(t =>
        {
            // Restaurant name cannot contain only spaces
            t.HasCheckConstraint(
                "CK_Restaurants_Name_NotBlank",
                "LEN(LTRIM(RTRIM([Name]))) > 0");

            // City cannot contain only spaces
            t.HasCheckConstraint(
                "CK_Restaurants_City_NotBlank",
                "LEN(LTRIM(RTRIM([City]))) > 0");

            // Area cannot contain only spaces
            t.HasCheckConstraint(
                "CK_Restaurants_Area_NotBlank",
                "LEN(LTRIM(RTRIM([Area]))) > 0");

            // Delivery time must be between 1 and 180 minutes
            t.HasCheckConstraint(
                "CK_Restaurants_DeliveryTime",
                "[DeliveryTimeMinutes] BETWEEN 1 AND 180");
        });
    }
}
