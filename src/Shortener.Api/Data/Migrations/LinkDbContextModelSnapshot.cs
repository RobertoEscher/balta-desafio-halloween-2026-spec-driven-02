using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Shortener.Api.Data;

#nullable disable

namespace Shortener.Api.Data.Migrations;

[DbContext(typeof(LinkDbContext))]
public sealed class LinkDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.14")
            .HasAnnotation("Relational:MaxIdentifierLength", 64);

        modelBuilder.Entity("Shortener.Api.Domain.Link", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            entity.Property<long>("AccessCount")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasDefaultValue(0L);

            entity.Property<string>("Code")
                .IsRequired()
                .HasMaxLength(7)
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset?>("ExpiresAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("TargetUrl")
                .IsRequired()
                .HasMaxLength(2048)
                .HasColumnType("TEXT");

            entity.HasKey("Id");
            entity.HasIndex("Code").IsUnique();
            entity.ToTable("Links");
        });
    }
}
