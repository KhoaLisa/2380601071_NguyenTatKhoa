using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NguyenTatKhoa_2380601071.Migrations
{
    /// <inheritdoc />
    public partial class ToeicImageNullGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ToeicContentBlocks_Content",
                table: "ToeicContentBlocks");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToeicContentBlocks_Content",
                table: "ToeicContentBlocks",
                sql: "([Type]=2 AND [BinaryData] IS NOT NULL AND DATALENGTH([BinaryData])>0 AND [MediaType] IS NOT NULL AND [MediaType] LIKE 'image/%' AND [TextContent] IS NULL) OR ([Type] IN (1,3) AND [TextContent] IS NOT NULL AND [BinaryData] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ToeicContentBlocks_Content",
                table: "ToeicContentBlocks");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToeicContentBlocks_Content",
                table: "ToeicContentBlocks",
                sql: "([Type]=2 AND DATALENGTH([BinaryData])>0 AND [MediaType] LIKE 'image/%' AND [TextContent] IS NULL) OR ([Type] IN (1,3) AND [TextContent] IS NOT NULL AND [BinaryData] IS NULL)");
        }
    }
}
