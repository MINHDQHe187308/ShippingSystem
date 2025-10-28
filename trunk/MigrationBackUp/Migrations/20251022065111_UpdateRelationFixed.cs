using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASP.Migrations
{
    public partial class UpdateRelationFixed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the primary key before altering the column
            migrationBuilder.DropPrimaryKey(
                name: "PK_LeadtimeMasters",
                table: "LeadtimeMasters");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerCode",
                table: "LeadtimeMasters",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            // Add the primary key back after altering the column
            migrationBuilder.AddPrimaryKey(
                name: "PK_LeadtimeMasters",
                table: "LeadtimeMasters",
                column: "CustomerCode");

            migrationBuilder.AddForeignKey(
                name: "FK_LeadtimeMasters_Customers_CustomerCode",
                table: "LeadtimeMasters",
                column: "CustomerCode",
                principalTable: "Customers",
                principalColumn: "CustomerCode",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingSchedules_Customers_CustomerCode",
                table: "ShippingSchedules",
                column: "CustomerCode",
                principalTable: "Customers",
                principalColumn: "CustomerCode",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeadtimeMasters_Customers_CustomerCode",
                table: "LeadtimeMasters");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingSchedules_Customers_CustomerCode",
                table: "ShippingSchedules");

            // Drop the primary key before reverting the column
            migrationBuilder.DropPrimaryKey(
                name: "PK_LeadtimeMasters",
                table: "LeadtimeMasters");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerCode",
                table: "LeadtimeMasters",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);

            // Add the primary key back after reverting the column
            migrationBuilder.AddPrimaryKey(
                name: "PK_LeadtimeMasters",
                table: "LeadtimeMasters",
                column: "CustomerCode");
        }
    }
}