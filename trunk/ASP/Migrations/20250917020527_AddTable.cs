using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASP.Migrations
{
    public partial class AddTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descriptions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdateBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerCode);
                });

            migrationBuilder.CreateTable(
                name: "DelayHistory",
                columns: table => new
                {
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Old = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DelayType = table.Column<short>(type: "smallint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DelayTime = table.Column<double>(type: "float", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelayHistory", x => x.Uid);
                });

            migrationBuilder.CreateTable(
                name: "LeadtimeMasters",
                columns: table => new
                {
                    CustomerCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TransCd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CollectTimePerPallet = table.Column<double>(type: "float", nullable: false),
                    PrepareTimePerPallet = table.Column<double>(type: "float", nullable: false),
                    LoadingTimePerColumn = table.Column<double>(type: "float", nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadtimeMasters", x => x.CustomerCode);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Oid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShippingId = table.Column<long>(type: "bigint", nullable: false),
                    BookContDetailId = table.Column<long>(type: "bigint", nullable: false),
                    ContNo = table.Column<int>(type: "int", nullable: false),
                    PartNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PalletSize = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    TotalPallet = table.Column<int>(type: "int", nullable: false),
                    Warehouse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BookContStatus = table.Column<short>(type: "smallint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.Uid);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoorderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShipDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransCd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransMethod = table.Column<short>(type: "smallint", nullable: false),
                    ContSize = table.Column<short>(type: "smallint", nullable: false),
                    TotalColumn = table.Column<int>(type: "int", nullable: false),
                    PartList = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalPallet = table.Column<int>(type: "int", nullable: false),
                    OrderStatus = table.Column<short>(type: "smallint", nullable: false),
                    OrderCreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlanAsyTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlanDocumentsTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlanDeliveryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcAsyTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcDocumentsTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcDeliveryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Uid);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingLists",
                columns: table => new
                {
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Odid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectionId = table.Column<long>(type: "bigint", nullable: false),
                    PalletId = table.Column<long>(type: "bigint", nullable: false),
                    PalletNo = table.Column<int>(type: "int", nullable: false),
                    CollectionStatus = table.Column<short>(type: "smallint", nullable: false),
                    CollectedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingLists", x => x.Uid);
                });

            migrationBuilder.CreateTable(
                name: "ThreePointChecks",
                columns: table => new
                {
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Spid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PalletMarkQrContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PalletNoQrContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CasemarkQrContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreePointChecks", x => x.Uid);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "DelayHistory");

            migrationBuilder.DropTable(
                name: "LeadtimeMasters");

            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ShoppingLists");

            migrationBuilder.DropTable(
                name: "ThreePointChecks");
        }
    }
}
