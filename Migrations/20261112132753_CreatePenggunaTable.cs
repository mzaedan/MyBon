using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyBon.Migrations
{
    /// <inheritdoc />
    public partial class CreatePenggunaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pengguna",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    peran = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pelanggan_id = table.Column<int>(type: "integer", nullable: true),
                    aktif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    terakhir_masuk = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pengguna", x => x.id);
                    table.CheckConstraint("ck_pengguna_peran", "peran IN ('Admin', 'Pelanggan')");
                    table.CheckConstraint("ck_pengguna_peran_pelanggan", "(peran = 'Pelanggan' AND pelanggan_id IS NOT NULL) OR (peran = 'Admin' AND pelanggan_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_pengguna_pelanggan_pelanggan_id",
                        column: x => x.pelanggan_id,
                        principalTable: "pelanggan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pengguna_pelanggan_id",
                table: "pengguna",
                column: "pelanggan_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pengguna_username",
                table: "pengguna",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pengguna");
        }
    }
}
