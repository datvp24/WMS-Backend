using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init1012 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_goodsIssueAllocates_LocationId",
                table: "goodsIssueAllocates",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_goodsIssueAllocates_Locations_LocationId",
                table: "goodsIssueAllocates",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_goodsIssueAllocates_Locations_LocationId",
                table: "goodsIssueAllocates");

            migrationBuilder.DropIndex(
                name: "IX_goodsIssueAllocates_LocationId",
                table: "goodsIssueAllocates");
        }
    }
}
