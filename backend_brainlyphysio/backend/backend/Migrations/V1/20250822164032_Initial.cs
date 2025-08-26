using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations.V1
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "anunturi",
                columns: table => new
                {
                    id_anunt = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    titlu_curs = table.Column<string>(type: "varchar(70)", maxLength: 70, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cale_imagine_curs = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    puncte_emc = table.Column<sbyte>(type: "tinyint", nullable: true),
                    descriere_curs = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    format = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    traineri = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nr_telefon_contact = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    locatie = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pret = table.Column<int>(type: "int", nullable: false),
                    durata_curs = table.Column<int>(type: "int", nullable: false),
                    data_venire = table.Column<DateTime>(type: "date", nullable: false),
                    data_plecare = table.Column<DateTime>(type: "date", nullable: false),
                    data_expirare = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anunturi", x => x.id_anunt);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "calitati",
                columns: table => new
                {
                    id_calitate = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nume_calitate = table.Column<string>(type: "varchar(150)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calitati", x => x.id_calitate);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "conturi",
                columns: table => new
                {
                    id_cont = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nume = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    prenume = table.Column<string>(type: "varchar(70)", maxLength: 70, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descriere = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nr_telefon = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    parola = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cale_imagine = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    image_hash = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rol = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ora_link = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "NOW()"),
                    cod_activare = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    verificat = table.Column<sbyte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conturi", x => x.id_cont);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "locatii",
                columns: table => new
                {
                    id_locatie = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    oras = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    judet = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locatii", x => x.id_locatie);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sectiuni_curs",
                columns: table => new
                {
                    id_sectiune_curs = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    titlu_sectiune = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ordine_sectiune = table.Column<int>(type: "int", nullable: false),
                    id_anunt = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sectiuni_curs", x => x.id_sectiune_curs);
                    table.ForeignKey(
                        name: "FK_sectiuni_curs_anunturi_id_anunt",
                        column: x => x.id_anunt,
                        principalTable: "anunturi",
                        principalColumn: "id_anunt",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "calitati_membru",
                columns: table => new
                {
                    id_calitate_membru = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_calitate = table.Column<int>(type: "int", nullable: false),
                    id_membru = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calitati_membru", x => x.id_calitate_membru);
                    table.ForeignKey(
                        name: "FK_calitati_membru_calitati_id_calitate",
                        column: x => x.id_calitate,
                        principalTable: "calitati",
                        principalColumn: "id_calitate",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_calitati_membru_conturi_id_membru",
                        column: x => x.id_membru,
                        principalTable: "conturi",
                        principalColumn: "id_cont",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sesiuni",
                columns: table => new
                {
                    id_sesiune = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_cont = table.Column<int>(type: "integer", nullable: false),
                    sesiune_stocata = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    issued_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(NOW() + INTERVAL 30 DAY)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesiuni", x => x.id_sesiune);
                    table.ForeignKey(
                        name: "FK_sesiuni_conturi_id_cont",
                        column: x => x.id_cont,
                        principalTable: "conturi",
                        principalColumn: "id_cont",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "locuri_de_operare",
                columns: table => new
                {
                    id_loc_operare = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_locatie = table.Column<int>(type: "int", nullable: false),
                    id_membru = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locuri_de_operare", x => x.id_loc_operare);
                    table.ForeignKey(
                        name: "FK_locuri_de_operare_conturi_id_membru",
                        column: x => x.id_membru,
                        principalTable: "conturi",
                        principalColumn: "id_cont",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_locuri_de_operare_locatii_id_locatie",
                        column: x => x.id_locatie,
                        principalTable: "locatii",
                        principalColumn: "id_locatie",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "continut_sectiune",
                columns: table => new
                {
                    id_continut_sectiune = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    continut = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ordine = table.Column<int>(type: "int", nullable: false),
                    id_sectiune_curs = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_continut_sectiune", x => x.id_continut_sectiune);
                    table.ForeignKey(
                        name: "FK_continut_sectiune_sectiuni_curs_id_sectiune_curs",
                        column: x => x.id_sectiune_curs,
                        principalTable: "sectiuni_curs",
                        principalColumn: "id_sectiune_curs",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_calitati_membru_id_calitate",
                table: "calitati_membru",
                column: "id_calitate");

            migrationBuilder.CreateIndex(
                name: "IX_calitati_membru_id_membru",
                table: "calitati_membru",
                column: "id_membru");

            migrationBuilder.CreateIndex(
                name: "IX_continut_sectiune_id_sectiune_curs",
                table: "continut_sectiune",
                column: "id_sectiune_curs");

            migrationBuilder.CreateIndex(
                name: "INDEX_EMAIL",
                table: "conturi",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_locuri_de_operare_id_locatie",
                table: "locuri_de_operare",
                column: "id_locatie");

            migrationBuilder.CreateIndex(
                name: "IX_locuri_de_operare_id_membru",
                table: "locuri_de_operare",
                column: "id_membru");

            migrationBuilder.CreateIndex(
                name: "IX_sectiuni_curs_id_anunt",
                table: "sectiuni_curs",
                column: "id_anunt");

            migrationBuilder.CreateIndex(
                name: "IX_sesiuni_id_cont",
                table: "sesiuni",
                column: "id_cont",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calitati_membru");

            migrationBuilder.DropTable(
                name: "continut_sectiune");

            migrationBuilder.DropTable(
                name: "locuri_de_operare");

            migrationBuilder.DropTable(
                name: "sesiuni");

            migrationBuilder.DropTable(
                name: "calitati");

            migrationBuilder.DropTable(
                name: "sectiuni_curs");

            migrationBuilder.DropTable(
                name: "locatii");

            migrationBuilder.DropTable(
                name: "conturi");

            migrationBuilder.DropTable(
                name: "anunturi");
        }
    }
}
