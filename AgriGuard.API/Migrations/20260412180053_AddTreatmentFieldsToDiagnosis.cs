using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriGuard.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentFieldsToDiagnosis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreventionTipsJson",
                table: "Diagnoses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedProductsJson",
                table: "Diagnoses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TreatmentGeneratedAt",
                table: "Diagnoses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreatmentNotes",
                table: "Diagnoses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreatmentPlan",
                table: "Diagnoses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreventionTipsJson",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "RecommendedProductsJson",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "TreatmentGeneratedAt",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "TreatmentNotes",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "TreatmentPlan",
                table: "Diagnoses");
        }
    }
}
