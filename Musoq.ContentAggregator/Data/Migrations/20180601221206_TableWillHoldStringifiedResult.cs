using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Musoq.ContentAggregator.Data.Migrations
{
    public partial class TableWillHoldStringifiedResult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rows");

            migrationBuilder.DropTable(
                name: "Columns");

            migrationBuilder.DropColumn(
                name: "ScriptId",
                table: "Tables");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Tables",
                newName: "Json");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefreshedAt",
                table: "UserScripts",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddForeignKey(
                name: "FK_Tables_Scripts_TableId",
                table: "Tables",
                column: "TableId",
                principalTable: "Scripts",
                principalColumn: "ScriptId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tables_Scripts_TableId",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "RefreshedAt",
                table: "UserScripts");

            migrationBuilder.RenameColumn(
                name: "Json",
                table: "Tables",
                newName: "Name");

            migrationBuilder.AddColumn<Guid>(
                name: "ScriptId",
                table: "Tables",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Columns",
                columns: table => new
                {
                    ColumnId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    TableId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Columns", x => x.ColumnId);
                    table.ForeignKey(
                        name: "FK_Columns_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "TableId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rows",
                columns: table => new
                {
                    RowId = table.Column<Guid>(nullable: false),
                    ColumnId = table.Column<Guid>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rows", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_Rows_Columns_ColumnId",
                        column: x => x.ColumnId,
                        principalTable: "Columns",
                        principalColumn: "ColumnId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Columns_TableId",
                table: "Columns",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_Rows_ColumnId",
                table: "Rows",
                column: "ColumnId");
        }
    }
}
