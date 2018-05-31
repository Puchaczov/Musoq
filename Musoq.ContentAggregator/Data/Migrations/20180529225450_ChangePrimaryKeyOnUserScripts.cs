using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Musoq.ContentAggregator.Data.Migrations
{
    public partial class ChangePrimaryKeyOnUserScripts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserScripts",
                table: "UserScripts");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserScripts",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<Guid>(
                name: "UserScriptId",
                table: "UserScripts",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserScripts",
                table: "UserScripts",
                column: "UserScriptId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserScripts",
                table: "UserScripts");

            migrationBuilder.DropColumn(
                name: "UserScriptId",
                table: "UserScripts");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserScripts",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserScripts",
                table: "UserScripts",
                column: "UserId");
        }
    }
}
