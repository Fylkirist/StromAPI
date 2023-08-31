using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StromAPI.Migrations.MagazineStockDbMigrations
{
    /// <inheritdoc />
    public partial class NewDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MagazineStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Area = table.Column<int>(type: "INTEGER", nullable: false),
                    AreaType = table.Column<string>(type: "TEXT", nullable: false),
                    Capacity = table.Column<double>(type: "REAL", nullable: false),
                    Filling = table.Column<double>(type: "REAL", nullable: false),
                    FillingFactor = table.Column<double>(type: "REAL", nullable: false),
                    FillingFactorLastWeek = table.Column<double>(type: "REAL", nullable: false),
                    FillingFactorChange = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagazineStocks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MagazineStocks");
        }
    }
}
