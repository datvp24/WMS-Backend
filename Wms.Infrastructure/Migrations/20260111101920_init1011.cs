using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init1011 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsIssueItems_SalesOrderItems_SalesOrderItemId",
                table: "GoodsIssueItems");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssueItems_SalesOrderItemId",
                table: "GoodsIssueItems");

            migrationBuilder.DropColumn(
                name: "SalesOrderItemId",
                table: "GoodsIssueItems");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueItems_SOIId",
                table: "GoodsIssueItems",
                column: "SOIId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsIssueItems_SalesOrderItems_SOIId",
                table: "GoodsIssueItems",
                column: "SOIId",
                principalTable: "SalesOrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsIssueItems_SalesOrderItems_SOIId",
                table: "GoodsIssueItems");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssueItems_SOIId",
                table: "GoodsIssueItems");

            migrationBuilder.AddColumn<Guid>(
                name: "SalesOrderItemId",
                table: "GoodsIssueItems",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueItems_SalesOrderItemId",
                table: "GoodsIssueItems",
                column: "SalesOrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsIssueItems_SalesOrderItems_SalesOrderItemId",
                table: "GoodsIssueItems",
                column: "SalesOrderItemId",
                principalTable: "SalesOrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
