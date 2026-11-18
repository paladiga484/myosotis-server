using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    credential = table.Column<string>(type: "TEXT", nullable: false),
                    account_type = table.Column<string>(type: "TEXT", nullable: false),
                    level = table.Column<int>(type: "INTEGER", nullable: false),
                    exp = table.Column<int>(type: "INTEGER", nullable: false),
                    stamina = table.Column<int>(type: "INTEGER", nullable: false),
                    last_stamina_recover = table.Column<long>(type: "INTEGER", nullable: false),
                    last_login_at = table.Column<long>(type: "INTEGER", nullable: false),
                    register_date = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.uid);
                });

            migrationBuilder.CreateTable(
                name: "announcer_presets",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    preset_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_announcer_presets", x => new { x.uid, x.preset_id });
                    table.ForeignKey(
                        name: "fk_announcer_presets_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "formations",
                columns: table => new
                {
                    formation_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formations", x => x.formation_id);
                    table.ForeignKey(
                        name: "fk_formations_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_banners",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    id = table.Column<int>(type: "INTEGER", nullable: false),
                    value = table.Column<int>(type: "INTEGER", nullable: false),
                    value2 = table.Column<int>(type: "INTEGER", nullable: false),
                    value3 = table.Column<int>(type: "INTEGER", nullable: false),
                    value4 = table.Column<int>(type: "INTEGER", nullable: false),
                    value5 = table.Column<int>(type: "INTEGER", nullable: false),
                    idx = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_banners", x => new { x.uid, x.id });
                    table.ForeignKey(
                        name: "fk_profile_banners_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_support_personalities",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    idx = table.Column<int>(type: "INTEGER", nullable: false),
                    pid = table.Column<int>(type: "INTEGER", nullable: false),
                    l = table.Column<int>(type: "INTEGER", nullable: false),
                    gl = table.Column<int>(type: "INTEGER", nullable: false),
                    gi = table.Column<int>(type: "INTEGER", nullable: false),
                    sid = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_support_personalities", x => new { x.uid, x.idx });
                    table.ForeignKey(
                        name: "fk_profile_support_personalities_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_tickets",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    ticket_type = table.Column<string>(type: "TEXT", nullable: false),
                    id = table.Column<int>(type: "INTEGER", nullable: false),
                    date = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_tickets", x => new { x.uid, x.ticket_type, x.id });
                    table.CheckConstraint("CK_profile_tickets_ticket_type", "ticket_type IN ('LEFT','RIGHT','EGOBG')");
                    table.ForeignKey(
                        name: "fk_profile_tickets_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "railway_saves",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    dungeon_id = table.Column<int>(type: "INTEGER", nullable: false),
                    prev_clear_node = table.Column<int>(type: "INTEGER", nullable: false),
                    current_node = table.Column<int>(type: "INTEGER", nullable: false),
                    last_clear_node = table.Column<int>(type: "INTEGER", nullable: false),
                    pay_reward = table.Column<int>(type: "INTEGER", nullable: false),
                    reward_state = table.Column<int>(type: "INTEGER", nullable: false),
                    current_clear_rotation = table.Column<int>(type: "INTEGER", nullable: false),
                    last_enter_node_id = table.Column<int>(type: "INTEGER", nullable: false),
                    last_clear_rotation = table.Column<int>(type: "INTEGER", nullable: false),
                    init_seed = table.Column<int>(type: "INTEGER", nullable: false),
                    current_seed = table.Column<int>(type: "INTEGER", nullable: false),
                    first_clear_date = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_railway_saves", x => new { x.uid, x.dungeon_id });
                    table.ForeignKey(
                        name: "fk_railway_saves_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_announcer_state",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    cur_preset_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_announcer_state", x => x.uid);
                    table.ForeignKey(
                        name: "fk_user_announcer_state_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_announcers",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    announcer_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_announcers", x => new { x.uid, x.announcer_id });
                    table.ForeignKey(
                        name: "fk_user_announcers_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_banners",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    id = table.Column<int>(type: "INTEGER", nullable: false),
                    acquiretime = table.Column<long>(type: "INTEGER", nullable: false),
                    value = table.Column<int>(type: "INTEGER", nullable: false),
                    value2 = table.Column<int>(type: "INTEGER", nullable: false),
                    value3 = table.Column<int>(type: "INTEGER", nullable: false),
                    value4 = table.Column<int>(type: "INTEGER", nullable: false),
                    value5 = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_banners", x => new { x.uid, x.id });
                    table.ForeignKey(
                        name: "fk_user_banners_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_egos",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    ego_id = table.Column<int>(type: "INTEGER", nullable: false),
                    gacksung = table.Column<int>(type: "INTEGER", nullable: false),
                    acquire_time = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_egos", x => new { x.uid, x.ego_id });
                    table.ForeignKey(
                        name: "fk_user_egos_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_items",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    item_id = table.Column<int>(type: "INTEGER", nullable: false),
                    num = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_items", x => new { x.uid, x.item_id });
                    table.ForeignKey(
                        name: "fk_user_items_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_personalities",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    personality_id = table.Column<int>(type: "INTEGER", nullable: false),
                    level = table.Column<int>(type: "INTEGER", nullable: false),
                    exp = table.Column<int>(type: "INTEGER", nullable: false),
                    gacksung = table.Column<int>(type: "INTEGER", nullable: false),
                    order_id = table.Column<int>(type: "INTEGER", nullable: false),
                    gacksung_illust_type = table.Column<int>(type: "INTEGER", nullable: false),
                    acquire_time = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_personalities", x => new { x.uid, x.personality_id });
                    table.ForeignKey(
                        name: "fk_user_personalities_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    public_uid = table.Column<string>(type: "TEXT", nullable: true),
                    illust_id = table.Column<int>(type: "INTEGER", nullable: true),
                    illust_gacksung_level = table.Column<int>(type: "INTEGER", nullable: true),
                    left_border_id = table.Column<int>(type: "INTEGER", nullable: true),
                    right_border_id = table.Column<int>(type: "INTEGER", nullable: true),
                    ego_background_id = table.Column<int>(type: "INTEGER", nullable: true),
                    sentence_id = table.Column<int>(type: "INTEGER", nullable: true),
                    word_id = table.Column<int>(type: "INTEGER", nullable: true),
                    level = table.Column<int>(type: "INTEGER", nullable: true),
                    date = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.uid);
                    table.ForeignKey(
                        name: "fk_user_profiles_users_uid",
                        column: x => x.uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "announcer_preset_entries",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    preset_id = table.Column<int>(type: "INTEGER", nullable: false),
                    idx = table.Column<int>(type: "INTEGER", nullable: false),
                    announcer_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_announcer_preset_entries", x => new { x.uid, x.preset_id, x.idx });
                    table.ForeignKey(
                        name: "fk_announcer_preset_entries_announcer_presets_uid_preset_id",
                        columns: x => new { x.uid, x.preset_id },
                        principalTable: "announcer_presets",
                        principalColumns: new[] { "uid", "preset_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "formation_details",
                columns: table => new
                {
                    formation_id = table.Column<long>(type: "INTEGER", nullable: false),
                    personality_id = table.Column<int>(type: "INTEGER", nullable: false),
                    is_participated = table.Column<bool>(type: "INTEGER", nullable: false),
                    participation_order = table.Column<int>(type: "INTEGER", nullable: false),
                    skin_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formation_details", x => new { x.formation_id, x.personality_id });
                    table.ForeignKey(
                        name: "fk_formation_details_formations_formation_id",
                        column: x => x.formation_id,
                        principalTable: "formations",
                        principalColumn: "formation_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "formation_names",
                columns: table => new
                {
                    formation_id = table.Column<long>(type: "INTEGER", nullable: false),
                    k = table.Column<int>(type: "INTEGER", nullable: false),
                    v = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formation_names", x => new { x.formation_id, x.k });
                    table.ForeignKey(
                        name: "fk_formation_names_formations_formation_id",
                        column: x => x.formation_id,
                        principalTable: "formations",
                        principalColumn: "formation_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_support_egos",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    idx = table.Column<int>(type: "INTEGER", nullable: false),
                    ego_idx = table.Column<int>(type: "INTEGER", nullable: false),
                    id = table.Column<int>(type: "INTEGER", nullable: false),
                    g = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_support_egos", x => new { x.uid, x.idx, x.ego_idx });
                    table.ForeignKey(
                        name: "fk_profile_support_egos_profile_support_personalities_uid_idx",
                        columns: x => new { x.uid, x.idx },
                        principalTable: "profile_support_personalities",
                        principalColumns: new[] { "uid", "idx" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "railway_save_units",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    dungeon_id = table.Column<int>(type: "INTEGER", nullable: false),
                    pord = table.Column<int>(type: "INTEGER", nullable: false),
                    pid = table.Column<int>(type: "INTEGER", nullable: false),
                    g = table.Column<int>(type: "INTEGER", nullable: false),
                    l = table.Column<int>(type: "INTEGER", nullable: false),
                    sp = table.Column<int>(type: "INTEGER", nullable: false),
                    gi = table.Column<int>(type: "INTEGER", nullable: false),
                    sid = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_railway_save_units", x => new { x.uid, x.dungeon_id, x.pord });
                    table.ForeignKey(
                        name: "fk_railway_save_units_railway_saves_uid_dungeon_id",
                        columns: x => new { x.uid, x.dungeon_id },
                        principalTable: "railway_saves",
                        principalColumns: new[] { "uid", "dungeon_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "formation_egos",
                columns: table => new
                {
                    formation_id = table.Column<long>(type: "INTEGER", nullable: false),
                    personality_id = table.Column<int>(type: "INTEGER", nullable: false),
                    idx = table.Column<int>(type: "INTEGER", nullable: false),
                    ego_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formation_egos", x => new { x.formation_id, x.personality_id, x.idx });
                    table.ForeignKey(
                        name: "fk_formation_egos_formation_details_formation_id_personality_id",
                        columns: x => new { x.formation_id, x.personality_id },
                        principalTable: "formation_details",
                        principalColumns: new[] { "formation_id", "personality_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "railway_save_unit_egos",
                columns: table => new
                {
                    uid = table.Column<long>(type: "INTEGER", nullable: false),
                    dungeon_id = table.Column<int>(type: "INTEGER", nullable: false),
                    pord = table.Column<int>(type: "INTEGER", nullable: false),
                    idx = table.Column<int>(type: "INTEGER", nullable: false),
                    ego_id = table.Column<int>(type: "INTEGER", nullable: false),
                    gacksung = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_railway_save_unit_egos", x => new { x.uid, x.dungeon_id, x.pord, x.idx });
                    table.ForeignKey(
                        name: "fk_railway_save_unit_egos_railway_save_units_uid_dungeon_id_pord",
                        columns: x => new { x.uid, x.dungeon_id, x.pord },
                        principalTable: "railway_save_units",
                        principalColumns: new[] { "uid", "dungeon_id", "pord" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_formations_uid_id",
                table: "formations",
                columns: new[] { "uid", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_credential",
                table: "users",
                column: "credential",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcer_preset_entries");

            migrationBuilder.DropTable(
                name: "formation_egos");

            migrationBuilder.DropTable(
                name: "formation_names");

            migrationBuilder.DropTable(
                name: "profile_banners");

            migrationBuilder.DropTable(
                name: "profile_support_egos");

            migrationBuilder.DropTable(
                name: "profile_tickets");

            migrationBuilder.DropTable(
                name: "railway_save_unit_egos");

            migrationBuilder.DropTable(
                name: "user_announcer_state");

            migrationBuilder.DropTable(
                name: "user_announcers");

            migrationBuilder.DropTable(
                name: "user_banners");

            migrationBuilder.DropTable(
                name: "user_egos");

            migrationBuilder.DropTable(
                name: "user_items");

            migrationBuilder.DropTable(
                name: "user_personalities");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "announcer_presets");

            migrationBuilder.DropTable(
                name: "formation_details");

            migrationBuilder.DropTable(
                name: "profile_support_personalities");

            migrationBuilder.DropTable(
                name: "railway_save_units");

            migrationBuilder.DropTable(
                name: "formations");

            migrationBuilder.DropTable(
                name: "railway_saves");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
