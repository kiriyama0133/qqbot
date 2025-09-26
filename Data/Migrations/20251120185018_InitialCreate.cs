using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qqbot.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    ChatType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MessageType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    MessageSequence = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SenderNickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SenderCard = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SenderRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GroupId = table.Column<long>(type: "bigint", nullable: false),
                    PrivateUserId = table.Column<long>(type: "bigint", nullable: false),
                    SelfId = table.Column<long>(type: "bigint", nullable: false),
                    RawMessage = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    FormattedMessage = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    MessageSegmentCount = table.Column<int>(type: "integer", nullable: false),
                    HasAtMessage = table.Column<bool>(type: "boolean", nullable: false),
                    HasImage = table.Column<bool>(type: "boolean", nullable: false),
                    HasFile = table.Column<bool>(type: "boolean", nullable: false),
                    HasForward = table.Column<bool>(type: "boolean", nullable: false),
                    Font = table.Column<int>(type: "integer", nullable: false),
                    Time = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRecalled = table.Column<bool>(type: "boolean", nullable: false),
                    RecalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");
        }
    }
}
