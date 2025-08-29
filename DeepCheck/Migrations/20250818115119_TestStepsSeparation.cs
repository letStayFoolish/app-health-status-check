using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepCheck.Migrations
{
    /// <inheritdoc />
    public partial class TestStepsSeparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestResponses");

            migrationBuilder.CreateTable(
                name: "TestRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ElapsedMs = table.Column<long>(type: "INTEGER", nullable: false),
                    RunMethod = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FailReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestRunSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestStepName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ElapsedMs = table.Column<long>(type: "INTEGER", nullable: false),
                    RunMethod = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    FailReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TestRunId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRunSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestRunSteps_TestRuns_TestRunId",
                        column: x => x.TestRunId,
                        principalTable: "TestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestRunSteps_TestRunId",
                table: "TestRunSteps",
                column: "TestRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestRunSteps");

            migrationBuilder.DropTable(
                name: "TestRuns");

            migrationBuilder.CreateTable(
                name: "TestResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElapsedMs = table.Column<long>(type: "INTEGER", nullable: false),
                    FailReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RunMethod = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    TestName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResponses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestResponses_StartedAt",
                table: "TestResponses",
                column: "StartedAt");
        }
    }
}
