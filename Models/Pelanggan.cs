using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyBon.Models;

[Table("pelanggan")]
public class Pelanggan
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama wajib diisi")]
    [MaxLength(200)]
    [Column("nama")]
    public string Nama { get; set; } = string.Empty;

    [MaxLength(30)]
    [Column("nomor_hp")]
    public string? NomorHp { get; set; }

    [Column("alamat")]
    public string? Alamat { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
