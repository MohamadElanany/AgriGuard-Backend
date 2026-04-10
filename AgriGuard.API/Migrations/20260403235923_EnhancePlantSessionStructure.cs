using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriGuard.API.Migrations
{
    /// <inheritdoc />
    public partial class EnhancePlantSessionStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomTitle",
                table: "UserPlants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "UserPlants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GrowthDurationDays",
                table: "Crops",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SunHours",
                table: "Crops",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WaterLevel",
                table: "Crops",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaskCategory",
                table: "CareTaskTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomTitle",
                table: "UserPlants");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "UserPlants");

            migrationBuilder.DropColumn(
                name: "GrowthDurationDays",
                table: "Crops");

            migrationBuilder.DropColumn(
                name: "SunHours",
                table: "Crops");

            migrationBuilder.DropColumn(
                name: "WaterLevel",
                table: "Crops");

            migrationBuilder.DropColumn(
                name: "TaskCategory",
                table: "CareTaskTemplates");
        }
    }
}
