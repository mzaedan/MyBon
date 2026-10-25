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

    public DbSet<Bon> Bons { get; set; }

    public DbSet<BonItem> BonItems { get; set; }

    public DbSet<BonPayment> BonPayments { get; set; }

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

        // BN-001 ... BN-999, then BN-1000: lpad only pads up to three digits. A dedicated sequence keeps
        // the number safe when two tabs save at once, with no MAX(nomor) + 1 race.
        modelBuilder.HasSequence<int>("bon_nomor_seq", sequence =>
        {
            sequence.StartsAt(1);
            sequence.IncrementsBy(1);
        });

        modelBuilder.Entity<Bon>(entity =>
        {
            entity.ToTable("bon");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.Nomor)
                  .HasColumnName("nomor")
                  .HasMaxLength(20)
                  .IsRequired()
                  .HasDefaultValueSql("'BN-' || lpad(nextval('bon_nomor_seq')::text, 3, '0')")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.PelangganId)
                  .HasColumnName("pelanggan_id");

            entity.Property(e => e.Tanggal)
                  .HasColumnName("tanggal")
                  .HasDefaultValueSql("CURRENT_DATE");

            entity.Property(e => e.Catatan)
                  .HasColumnName("catatan");

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // A customer who still owns a bon cannot be deleted; the debt would vanish with the name.
            entity.HasOne(e => e.Pelanggan)
                  .WithMany()
                  .HasForeignKey(e => e.PelangganId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Nomor).IsUnique();
            entity.HasIndex(e => new { e.Tanggal, e.Id }).IsDescending(true, true);
            entity.HasIndex(e => new { e.PelangganId, e.Tanggal, e.Id }).IsDescending(false, true, true);
        });

        modelBuilder.Entity<BonItem>(entity =>
        {
            entity.ToTable("bon_item", table =>
            {
                table.HasCheckConstraint("ck_bon_item_harga", "harga_satuan >= 0");
                table.HasCheckConstraint("ck_bon_item_jumlah", "jumlah >= 1");
            });

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.BonId)
                  .HasColumnName("bon_id");

            entity.Property(e => e.BarangId)
                  .HasColumnName("barang_id");

            entity.Property(e => e.NamaBarang)
                  .HasColumnName("nama_barang")
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Satuan)
                  .HasColumnName("satuan")
                  .HasMaxLength(30);

            entity.Property(e => e.HargaSatuan)
                  .HasColumnName("harga_satuan")
                  .HasPrecision(14, 2);

            entity.Property(e => e.Jumlah)
                  .HasColumnName("jumlah")
                  .HasDefaultValue(1);

            entity.Property(e => e.Tanggal)
                  .HasColumnName("tanggal")
                  .HasDefaultValueSql("CURRENT_DATE");

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Bon)
                  .WithMany(b => b.Items)
                  .HasForeignKey(e => e.BonId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Deleting a stock item keeps the line and its snapshot, so an old page stays readable.
            entity.HasOne(e => e.Barang)
                  .WithMany()
                  .HasForeignKey(e => e.BarangId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.BonId);
            entity.HasIndex(e => e.BarangId);
        });

        modelBuilder.Entity<BonPayment>(entity =>
        {
            entity.ToTable("bon_payment", table =>
                table.HasCheckConstraint("ck_bon_payment_jumlah", "jumlah > 0"));

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.BonId)
                  .HasColumnName("bon_id");

            entity.Property(e => e.Tanggal)
                  .HasColumnName("tanggal");

            entity.Property(e => e.Jumlah)
                  .HasColumnName("jumlah")
                  .HasPrecision(14, 2);

            entity.Property(e => e.Catatan)
                  .HasColumnName("catatan")
                  .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Bon)
                  .WithMany(b => b.Payments)
                  .HasForeignKey(e => e.BonId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.BonId, e.Tanggal });
        });
    }
}

