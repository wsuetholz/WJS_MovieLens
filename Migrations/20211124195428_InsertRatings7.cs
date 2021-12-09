using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WJS_MovieLens.Migrations
{
    public partial class InsertRatings7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SQL", @"6-7-InsertRatings.sql");
            migrationBuilder.Sql(File.ReadAllText(sqlFile));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("delete from usermovies where id >= 70000 and id < 80000");
        }
    }
}
