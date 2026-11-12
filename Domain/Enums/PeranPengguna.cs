namespace MyBon.Domain.Enums;

/// <summary>
/// Role an account can hold. There are exactly two: <see cref="Admin"/> runs the shop and reaches
/// every screen, <see cref="Pelanggan"/> may only read the bons written for their own customer row.
/// </summary>
public enum PeranPengguna
{
    Admin,
    Pelanggan
}