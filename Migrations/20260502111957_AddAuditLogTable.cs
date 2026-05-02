using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ptsamonitor.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "TADEYI");

            migrationBuilder.CreateTable(
                name: "PTSA_MONITOR_AUDIT_LOGS",
                schema: "TADEYI",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    EVENT_NAME = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    USER_ID = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    IP_ADDRESS = table.Column<string>(type: "NVARCHAR2(45)", maxLength: 45, nullable: true),
                    PAGE_URL = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    EVENT_DATE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTSA_MONITOR_AUDIT_LOGS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PTSA_MONITOR_INSTITUTIONS",
                schema: "TADEYI",
                columns: table => new
                {
                    INSTITUTION_ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    INSTITUTION_NAME = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    INSTITUTION_TYPE = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    INSTITUTION_EMAILS = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    BANK_BINS = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    TERMINAL_IDS = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    INSTITUTION_DOMAIN = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    INSTITUTION_CODE = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    INSTITUTION_LOGO = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    INSTITUTION_SHORT_NAME = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    INSTITUTION_SUB_CODES = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTSA_MONITOR_INSTITUTIONS", x => x.INSTITUTION_ID);
                });

            migrationBuilder.CreateTable(
                name: "PTSA_MONITOR_USERS",
                schema: "TADEYI",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    USER_NAME = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    PASSWORD = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: true),
                    INSTITUTION = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: true),
                    USER_TYPE = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    PRIVILEGES = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    STATUS = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    LAST_LOGIN_DATE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LAST_LOGIN_IP = table.Column<string>(type: "NVARCHAR2(45)", maxLength: 45, nullable: true),
                    CREATION_DATE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTSA_MONITOR_USERS", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PTSA_MONITOR_USERS_USER_NAME",
                schema: "TADEYI",
                table: "PTSA_MONITOR_USERS",
                column: "USER_NAME",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PTSA_MONITOR_AUDIT_LOGS",
                schema: "TADEYI");

            migrationBuilder.DropTable(
                name: "PTSA_MONITOR_INSTITUTIONS",
                schema: "TADEYI");

            migrationBuilder.DropTable(
                name: "PTSA_MONITOR_USERS",
                schema: "TADEYI");
        }
    }
}
