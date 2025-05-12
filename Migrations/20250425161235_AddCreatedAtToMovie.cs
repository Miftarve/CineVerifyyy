using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVerify.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToMovie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MovieReviews",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<double>(
                name: "RatinRg",
                table: "MovieReviews",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MovieReviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MovieAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MovieId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    AnalysisContent = table.Column<string>(type: "TEXT", nullable: false),
                    PdfPath = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieAnalyses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovieAnalyses_MovieId",
                table: "MovieAnalyses",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieAnalyses_UserId",
                table: "MovieAnalyses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovieAnalyses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MovieReviews");

            migrationBuilder.DropColumn(
                name: "RatinRg",
                table: "MovieReviews");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MovieReviews");
        }
    }
}
