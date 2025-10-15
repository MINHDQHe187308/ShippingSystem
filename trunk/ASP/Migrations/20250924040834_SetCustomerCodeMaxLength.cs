using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASP.Migrations
{
    public partial class SetCustomerCodeMaxLength : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa ràng buộc khóa chính của bảng Customers
            migrationBuilder.DropPrimaryKey(
                name: "PK_Customers",
                table: "Customers");

            // Đổi tên cột trong bảng Logs
            migrationBuilder.RenameColumn(
                name: "Ip",
                table: "Logs",
                newName: "IP");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Logs",
                newName: "ID");

            // Thay đổi các cột trong bảng Logs
            migrationBuilder.AlterColumn<string>(
                name: "LogType",
                table: "Logs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "IP",
                table: "Logs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Sửa Content thành nvarchar(max) thay vì ntext
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "Logs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Thay đổi cột CustomerCode trong bảng Customers
            migrationBuilder.AlterColumn<string>(
                name: "CustomerCode",
                table: "Customers",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            // Tạo lại ràng buộc khóa chính
            migrationBuilder.AddPrimaryKey(
                name: "PK_Customers",
                table: "Customers",
                column: "CustomerCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa ràng buộc khóa chính
            migrationBuilder.DropPrimaryKey(
                name: "PK_Customers",
                table: "Customers");

            // Đổi tên cột trong bảng Logs
            migrationBuilder.RenameColumn(
                name: "IP",
                table: "Logs",
                newName: "Ip");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Logs",
                newName: "Id");

            // Khôi phục các cột trong bảng Logs
            migrationBuilder.AlterColumn<string>(
                name: "LogType",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Ip",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            // Khôi phục cột CustomerCode trong bảng Customers
            migrationBuilder.AlterColumn<string>(
                name: "CustomerCode",
                table: "Customers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);

            // Tạo lại ràng buộc khóa chính
            migrationBuilder.AddPrimaryKey(
                name: "PK_Customers",
                table: "Customers",
                column: "CustomerCode");
        }
    }
}