using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASP.Migrations
{
    public partial class UpdateDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ThreePointChecks_SPId",
                table: "ThreePointChecks");

            migrationBuilder.DropColumn(
                name: "AcAsyTime",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PlanAsyTime",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "CollectionStatus",
                table: "ShoppingLists",
                newName: "PLStatus");

            migrationBuilder.RenameColumn(
                name: "PlanDocumentsTime",
                table: "Orders",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "PlanDeliveryTime",
                table: "Orders",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "AcDocumentsTime",
                table: "Orders",
                newName: "AcStartTime");

            migrationBuilder.RenameColumn(
                name: "AcDeliveryTime",
                table: "Orders",
                newName: "AcEndTime");

            migrationBuilder.CreateIndex(
                name: "IX_ThreePointChecks_SPId",
                table: "ThreePointChecks",
                column: "SPId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ThreePointChecks_SPId",
                table: "ThreePointChecks");

            migrationBuilder.RenameColumn(
                name: "PLStatus",
                table: "ShoppingLists",
                newName: "CollectionStatus");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Orders",
                newName: "PlanDocumentsTime");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "Orders",
                newName: "PlanDeliveryTime");

            migrationBuilder.RenameColumn(
                name: "AcStartTime",
                table: "Orders",
                newName: "AcDocumentsTime");

            migrationBuilder.RenameColumn(
                name: "AcEndTime",
                table: "Orders",
                newName: "AcDeliveryTime");

            migrationBuilder.AddColumn<DateTime>(
                name: "AcAsyTime",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanAsyTime",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_ThreePointChecks_SPId",
                table: "ThreePointChecks",
                column: "SPId");
        }
    }
}
