using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyBon.Migrations
{
    /// <inheritdoc />
    public partial class CreateBonTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "bon_nomor_seq");

            migrationBuilder.CreateTable(
                name: "bon",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nomor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'BN-' || lpad(nextval('bon_nomor_seq')::text, 3, '0')"),
                    pelanggan_id = table.Column<int>(type: "integer", nullable: false),
                    tanggal = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    catatan = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bon", x => x.id);
                    table.ForeignKey(
                        name: "FK_bon_pelanggan_pelanggan_id",
                        column: x => x.pelanggan_id,
                        principalTable: "pelanggan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bon_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bon_id = table.Column<int>(type: "integer", nullable: false),
                    barang_id = table.Column<int>(type: "integer", nullable: true),
                    nama_barang = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    satuan = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    harga_satuan = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    jumlah = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    tanggal = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bon_item", x => x.id);
                    table.CheckConstraint("ck_bon_item_harga", "harga_satuan >= 0");
                    table.CheckConstraint("ck_bon_item_jumlah", "jumlah >= 1");
                    table.ForeignKey(
                        name: "FK_bon_item_barang_barang_id",
                        column: x => x.barang_id,
                        principalTable: "barang",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_bon_item_bon_bon_id",
                        column: x => x.bon_id,
                        principalTable: "bon",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bon_payment",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bon_id = table.Column<int>(type: "integer", nullable: false),
                    tanggal = table.Column<DateOnly>(type: "date", nullable: false),
                    jumlah = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    catatan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bon_payment", x => x.id);
                    table.CheckConstraint("ck_bon_payment_jumlah", "jumlah > 0");
                    table.ForeignKey(
                        name: "FK_bon_payment_bon_bon_id",
                        column: x => x.bon_id,
                        principalTable: "bon",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bon_nomor",
                table: "bon",
                column: "nomor",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bon_pelanggan_id_tanggal_id",
                table: "bon",
                columns: new[] { "pelanggan_id", "tanggal", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_bon_tanggal_id",
                table: "bon",
                columns: new[] { "tanggal", "id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_bon_item_barang_id",
                table: "bon_item",
                column: "barang_id");

            migrationBuilder.CreateIndex(
                name: "IX_bon_item_bon_id",
                table: "bon_item",
                column: "bon_id");

            migrationBuilder.CreateIndex(
                name: "IX_bon_payment_bon_id_tanggal",
                table: "bon_payment",
                columns: new[] { "bon_id", "tanggal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bon_item");

            migrationBuilder.DropTable(
                name: "bon_payment");

            migrationBuilder.DropTable(
                name: "bon");

            migrationBuilder.DropSequence(
                name: "bon_nomor_seq");
        }
    }
}
