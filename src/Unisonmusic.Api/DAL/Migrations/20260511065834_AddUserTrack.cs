using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unisonmusic.Api.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTrack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_tracks",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TrackId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tracks", x => new { x.UserId, x.TrackId });
                    table.ForeignKey(
                        name: "FK_user_tracks_tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_tracks_TrackId",
                table: "user_tracks",
                column: "TrackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_tracks");
        }
    }
}
