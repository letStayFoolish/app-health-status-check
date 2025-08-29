using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepCheck.Migrations
{
    /// <inheritdoc />
    public partial class SlimingDownDatabaseEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "TestRunSteps");

            migrationBuilder.DropColumn(
                name: "RunMethod",
                table: "TestRunSteps");

            migrationBuilder.DropColumn(
                name: "CronExpression",
                table: "TestRuns");

            migrationBuilder.DropColumn(
                name: "FailReason",
                table: "TestRuns");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "TestRuns");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TestRuns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "TestRunSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RunMethod",
                table: "TestRunSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CronExpression",
                table: "TestRuns",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FailReason",
                table: "TestRuns",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "TestRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TestRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
