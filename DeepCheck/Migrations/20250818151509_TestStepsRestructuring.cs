using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepCheck.Migrations
{
    /// <inheritdoc />
    public partial class TestStepsRestructuring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CronExpression",
                table: "TestRunSteps",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "TestRunSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "TestRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CronExpression",
                table: "TestRunSteps");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "TestRunSteps");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "TestRuns");
        }
    }
}
