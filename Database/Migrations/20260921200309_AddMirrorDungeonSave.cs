using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMirrorDungeonSave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mirror_dungeon_saves",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    dungeon_id = table.Column<int>(type: "INTEGER", nullable: false),
                    idx = table.Column<int>(type: "INTEGER", nullable: false),
                    is_ended = table.Column<int>(type: "INTEGER", nullable: false),
                    save_json = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mirror_dungeon_saves", x => x.uid);
                    table.ForeignKey(
                        name: "fk_mirror_dungeon_saves_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mirror_dungeon_saves");
        }
    }
}
