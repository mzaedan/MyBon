using Microsoft.EntityFrameworkCore;
using MyBon.Domain.Entities;

namespace MyBon.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Pelanggan> Pelanggans { get; set; }

    public DbSet<Barang> Barangs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Pelanggan>(entity =>
        {
            entity.ToTable("pelanggan");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.Nama)
                  .HasColumnName("nama")
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.NomorHp)
                  .HasColumnName("nomor_hp")
                  .HasMaxLength(30);

            entity.Property(e => e.Alamat)
                  .HasColumnName("alamat");

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Barang>(entity =>
        {
            entity.ToTable("barang");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.Nama)
                  .HasColumnName("nama")
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Satuan)
                  .HasColumnName("satuan")
                  .HasMaxLength(30);

            entity.Property(e => e.Harga)
                  .HasColumnName("harga")
                  .HasPrecision(14, 2);

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}

