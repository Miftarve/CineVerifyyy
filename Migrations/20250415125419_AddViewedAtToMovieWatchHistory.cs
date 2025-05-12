using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVerify.Migrations
{
    /// <inheritdoc />
    public partial class AddViewedAtToMovieWatchHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WatchedAt",
                table: "MovieWatchHistory",
                newName: "ViewedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ViewedAt",
                table: "MovieWatchHistory",
                newName: "WatchedAt");
        }
    }
}
