using Microsoft.EntityFrameworkCore.Migrations;

namespace MqttBroker.Migrations
{
    public partial class changeColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VechicleId",
                table: "Audit");

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "Audit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Topic",
                table: "Audit");

            migrationBuilder.AddColumn<string>(
                name: "VechicleId",
                table: "Audit",
                type: "text",
                nullable: true);
        }
    }
}
