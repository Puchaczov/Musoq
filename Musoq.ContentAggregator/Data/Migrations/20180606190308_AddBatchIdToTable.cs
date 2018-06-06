using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Musoq.ContentAggregator.Data.Migrations
{
    public partial class AddBatchIdToTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tables_Scripts_TableId",
                table: "Tables");

            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "Tables",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ScriptId",
                table: "Tables",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Tables_ScriptId",
                table: "Tables",
                column: "ScriptId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tables_Scripts_ScriptId",
                table: "Tables",
                column: "ScriptId",
                principalTable: "Scripts",
                principalColumn: "ScriptId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tables_Scripts_ScriptId",
                table: "Tables");

            migrationBuilder.DropIndex(
                name: "IX_Tables_ScriptId",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "ScriptId",
                table: "Tables");

            migrationBuilder.AddForeignKey(
                name: "FK_Tables_Scripts_TableId",
                table: "Tables",
                column: "TableId",
                principalTable: "Scripts",
                principalColumn: "ScriptId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
