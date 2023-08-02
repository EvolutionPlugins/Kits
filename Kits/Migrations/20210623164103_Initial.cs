﻿using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kits.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kits_Kits",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 25, nullable: false),
                    Cooldown = table.Column<float>(nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Money = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VehicleId = table.Column<string>(maxLength: 5, nullable: true),
                    Items = table.Column<byte[]>(type: "blob", maxLength: 65535, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kits_Kits", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kits_Kits");
        }
    }
}
