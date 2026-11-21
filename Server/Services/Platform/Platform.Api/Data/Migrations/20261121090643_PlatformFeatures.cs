using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlatformFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenant_subscriptions_tenant_id",
                table: "tenant_subscriptions");

            migrationBuilder.CreateTable(
                name: "platform_features",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_db_created = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_subscription_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_features", x => x.id);
                    table.ForeignKey(
                        name: "FK_platform_features_tenant_subscriptions_tenant_subscription_~",
                        column: x => x.tenant_subscription_id,
                        principalTable: "tenant_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_tenant_id",
                table: "tenant_subscriptions",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_features_tenant_subscription_id_code",
                table: "platform_features",
                columns: new[] { "tenant_subscription_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_features");

            migrationBuilder.DropIndex(
                name: "IX_tenant_subscriptions_tenant_id",
                table: "tenant_subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_tenant_id",
                table: "tenant_subscriptions",
                column: "tenant_id");
        }
    }
}
