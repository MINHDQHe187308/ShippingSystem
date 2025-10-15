using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASP.Migrations
{
    public partial class AddTableShippingSchedule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShippingSchedules",
                columns: table => new
                {
                    CustomerCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TransCd = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Weekday = table.Column<int>(type: "int", nullable: false),
                    CutOffTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingSchedules", x => new { x.CustomerCode, x.TransCd, x.Weekday });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingSchedules");
        }
    }
}
