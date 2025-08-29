using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepCheck.Migrations
{
    /// <inheritdoc />
    public partial class TestRunCronExpressionAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CronExpression",
                table: "TestRunSteps");

            migrationBuilder.AddColumn<string>(
                name: "CronExpression",
                table: "TestRuns",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CronExpression",
                table: "TestRuns");

            migrationBuilder.AddColumn<string>(
                name: "CronExpression",
                table: "TestRunSteps",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
