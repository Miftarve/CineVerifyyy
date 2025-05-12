using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVerify.Migrations
{
    /// <inheritdoc />
    public partial class FixMovieWatchHistoryUserIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovieReviews_AspNetUsers_UserId",
                table: "MovieReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieReviews_Movies_MovieId1",
                table: "MovieReviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieWatchHistory",
                table: "MovieWatchHistory");

            migrationBuilder.DropIndex(
                name: "IX_MovieReviews_MovieId1",
                table: "MovieReviews");

            migrationBuilder.DropColumn(
                name: "MovieId1",
                table: "MovieReviews");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "MovieWatchHistory",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "MovieWatchHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Completed",
                table: "MovieWatchHistory",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieWatchHistory",
                table: "MovieWatchHistory",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MovieWatchHistory_ApplicationUserId",
                table: "MovieWatchHistory",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieWatchHistory_UserId",
                table: "MovieWatchHistory",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovieReviews_AspNetUsers_UserId",
                table: "MovieReviews",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MovieWatchHistory_AspNetUsers_ApplicationUserId",
                table: "MovieWatchHistory",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovieReviews_AspNetUsers_UserId",
                table: "MovieReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_MovieWatchHistory_AspNetUsers_ApplicationUserId",
                table: "MovieWatchHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieWatchHistory",
                table: "MovieWatchHistory");

            migrationBuilder.DropIndex(
                name: "IX_MovieWatchHistory_ApplicationUserId",
                table: "MovieWatchHistory");

            migrationBuilder.DropIndex(
                name: "IX_MovieWatchHistory_UserId",
                table: "MovieWatchHistory");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "MovieWatchHistory");

            migrationBuilder.DropColumn(
                name: "Completed",
                table: "MovieWatchHistory");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "MovieWatchHistory",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "MovieId1",
                table: "MovieReviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieWatchHistory",
                table: "MovieWatchHistory",
                columns: new[] { "UserId", "MovieId" });

            migrationBuilder.CreateIndex(
                name: "IX_MovieReviews_MovieId1",
                table: "MovieReviews",
                column: "MovieId1");

            migrationBuilder.AddForeignKey(
                name: "FK_MovieReviews_AspNetUsers_UserId",
                table: "MovieReviews",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MovieReviews_Movies_MovieId1",
                table: "MovieReviews",
                column: "MovieId1",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
