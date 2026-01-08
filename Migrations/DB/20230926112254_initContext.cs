using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADDPerformance.Migrations.DB
{
    /// <inheritdoc />
    public partial class initContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADD_CK",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Month = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CY = table.Column<double>(type: "float", nullable: false),
                    LY = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<double>(type: "float", nullable: false),
                    AT = table.Column<double>(type: "float", nullable: false),
                    ALY = table.Column<double>(type: "float", nullable: false),
                    Total = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthNum = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADD_CK", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ByTourCode",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TourCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CORP_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CORPORATE_NAME = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target = table.Column<double>(type: "float", nullable: false),
                    ATPercent = table.Column<double>(type: "float", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MonthylyAmount = table.Column<double>(type: "float", nullable: false),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthNum = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ByTourCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorporateSales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Month = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorpType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CY = table.Column<double>(type: "float", nullable: false),
                    LY = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<double>(type: "float", nullable: false),
                    AT = table.Column<double>(type: "float", nullable: false),
                    ALY = table.Column<double>(type: "float", nullable: false),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthNum = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorporateSales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DateMaster",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Month = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthNum = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DateMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DestCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Month = table.Column<DateTime>(type: "datetime2", nullable: false),
                    paxCount = table.Column<long>(type: "bigint", nullable: false),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthNum = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnlineSales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Month = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CYPercent = table.Column<double>(type: "float", nullable: false),
                    LYPercent = table.Column<double>(type: "float", nullable: false),
                    TargetPercent = table.Column<double>(type: "float", nullable: false),
                    AT = table.Column<double>(type: "float", nullable: false),
                    ALY = table.Column<double>(type: "float", nullable: false),
                    Total = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthNum = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineSales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "REV_USD",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Month = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CY_USD = table.Column<double>(type: "float", nullable: false),
                    LY_USD = table.Column<double>(type: "float", nullable: false),
                    Target_USD = table.Column<double>(type: "float", nullable: false),
                    AT = table.Column<double>(type: "float", nullable: false),
                    ALY = table.Column<double>(type: "float", nullable: false),
                    Total = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthNum = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REV_USD", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADD_CK");

            migrationBuilder.DropTable(
                name: "ByTourCode");

            migrationBuilder.DropTable(
                name: "CorporateSales");

            migrationBuilder.DropTable(
                name: "DateMaster");

            migrationBuilder.DropTable(
                name: "Destinations");

            migrationBuilder.DropTable(
                name: "OnlineSales");

            migrationBuilder.DropTable(
                name: "REV_USD");
        }
    }
}
