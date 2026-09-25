using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_Name",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_ProductId_LocationId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_AdjustmentNo",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_StatusHistories_ActorUserId_OccurredAtUtc",
                table: "StatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_StatusHistories_EntityType_EntityId_OccurredAtUtc",
                table: "StatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrders_OrderNo",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_Receivings_PurchaseOrderId",
                table: "Receivings");

            migrationBuilder.DropIndex(
                name: "IX_Receivings_ReceivingNo",
                table: "Receivings");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_PoNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Products_Sku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Pickings_PickingNo",
                table: "Pickings");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Name",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActorUserId_OccurredAtUtc",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityType_EntityId_OccurredAtUtc",
                table: "AuditLogs");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Vendors",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "Stocks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Stocks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "StockAdjustments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "StockAdjustmentDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "StatusHistories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SaleOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SaleOrderDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Rmas",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "RmaDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Receivings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "ReceivingDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "ReceivingDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ReceivingDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PutAwayTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PurchaseOrderDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Pickings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "PickingDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "PickingDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PickingDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Locations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Categories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AssociationRules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HasExpiryManagement = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "Name", "Code", "Address", "ContactEmail", "ContactPhone", "HasExpiryManagement", "IsActive", "CreatedDate", "VerifiedDate" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Công ty TNHH Demo Logistics", "demo-corp", "12 Nguyễn Văn Linh, Quận 7, TP.HCM", "admin@demo.com", "0901234567", false, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.Sql(@"
                UPDATE [dbo].[Warehouses] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[AspNetUsers] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[Categories] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[Products] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[Locations] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[Stocks] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[StockMovements] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[Vendors] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[Customers] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[PurchaseOrders] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[PurchaseOrderDetails] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[Receivings] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[ReceivingDetails] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[PutAwayTasks] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[SaleOrders] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[SaleOrderDetails] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[Pickings] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[PickingDetails] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[Shipments] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[StockAdjustments] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[StockAdjustmentDetails] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[Rmas] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[RmaDetails] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[AssociationRules] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[AuditLogs] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [dbo].[StatusHistories] SET [TenantId] = '11111111-1111-1111-1111-111111111111' WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_Code",
                table: "Warehouses",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_TenantId_Name",
                table: "Vendors",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductId",
                table: "Stocks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ProductId_LocationId_LotNumber_ExpiryDate",
                table: "Stocks",
                columns: new[] { "TenantId", "ProductId", "LocationId", "LotNumber", "ExpiryDate" },
                unique: true,
                filter: "[LotNumber] IS NOT NULL AND [ExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId",
                table: "StockMovements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_TenantId_AdjustmentNo",
                table: "StockAdjustments",
                columns: new[] { "TenantId", "AdjustmentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentDetails_TenantId",
                table: "StockAdjustmentDetails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_ActorUserId",
                table: "StatusHistories",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_TenantId_ActorUserId_OccurredAtUtc",
                table: "StatusHistories",
                columns: new[] { "TenantId", "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_TenantId_EntityType_EntityId_OccurredAtUtc",
                table: "StatusHistories",
                columns: new[] { "TenantId", "EntityType", "EntityId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId",
                table: "Shipments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrders_TenantId_OrderNo",
                table: "SaleOrders",
                columns: new[] { "TenantId", "OrderNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrderDetails_TenantId",
                table: "SaleOrderDetails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Rmas_TenantId",
                table: "Rmas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RmaDetails_TenantId",
                table: "RmaDetails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_PurchaseOrderId",
                table: "Receivings",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_TenantId_PurchaseOrderId",
                table: "Receivings",
                columns: new[] { "TenantId", "PurchaseOrderId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_TenantId_ReceivingNo",
                table: "Receivings",
                columns: new[] { "TenantId", "ReceivingNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingDetails_TenantId",
                table: "ReceivingDetails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PutAwayTasks_TenantId",
                table: "PutAwayTasks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_PoNumber",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "PoNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_TenantId",
                table: "PurchaseOrderDetails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Sku",
                table: "Products",
                columns: new[] { "TenantId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pickings_TenantId_PickingNo",
                table: "Pickings",
                columns: new[] { "TenantId", "PickingNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickingDetails_TenantId",
                table: "PickingDetails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId",
                table: "Locations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Name",
                table: "Customers",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TenantId",
                table: "Categories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_ActorUserId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "TenantId", "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_EntityType_EntityId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "TenantId", "EntityType", "EntityId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationRules_TenantId",
                table: "AssociationRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Code",
                table: "Tenants",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssociationRules_Tenants_TenantId",
                table: "AssociationRules",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Tenants_TenantId",
                table: "AuditLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Tenants_TenantId",
                table: "Categories",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Tenants_TenantId",
                table: "Customers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_Tenants_TenantId",
                table: "Locations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PickingDetails_Tenants_TenantId",
                table: "PickingDetails",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pickings_Tenants_TenantId",
                table: "Pickings",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderDetails_Tenants_TenantId",
                table: "PurchaseOrderDetails",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Tenants_TenantId",
                table: "PurchaseOrders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PutAwayTasks_Tenants_TenantId",
                table: "PutAwayTasks",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivingDetails_Tenants_TenantId",
                table: "ReceivingDetails",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Receivings_Tenants_TenantId",
                table: "Receivings",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RmaDetails_Tenants_TenantId",
                table: "RmaDetails",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rmas_Tenants_TenantId",
                table: "Rmas",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrderDetails_Tenants_TenantId",
                table: "SaleOrderDetails",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrders_Tenants_TenantId",
                table: "SaleOrders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Tenants_TenantId",
                table: "Shipments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusHistories_Tenants_TenantId",
                table: "StatusHistories",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentDetails_Tenants_TenantId",
                table: "StockAdjustmentDetails",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustments_Tenants_TenantId",
                table: "StockAdjustments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Tenants_TenantId",
                table: "StockMovements",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stocks_Tenants_TenantId",
                table: "Stocks",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_Tenants_TenantId",
                table: "Vendors",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Tenants_TenantId",
                table: "Warehouses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AssociationRules_Tenants_TenantId",
                table: "AssociationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Tenants_TenantId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Tenants_TenantId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Tenants_TenantId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Locations_Tenants_TenantId",
                table: "Locations");

            migrationBuilder.DropForeignKey(
                name: "FK_PickingDetails_Tenants_TenantId",
                table: "PickingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Pickings_Tenants_TenantId",
                table: "Pickings");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderDetails_Tenants_TenantId",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Tenants_TenantId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PutAwayTasks_Tenants_TenantId",
                table: "PutAwayTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivingDetails_Tenants_TenantId",
                table: "ReceivingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Receivings_Tenants_TenantId",
                table: "Receivings");

            migrationBuilder.DropForeignKey(
                name: "FK_RmaDetails_Tenants_TenantId",
                table: "RmaDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Rmas_Tenants_TenantId",
                table: "Rmas");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrderDetails_Tenants_TenantId",
                table: "SaleOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrders_Tenants_TenantId",
                table: "SaleOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Tenants_TenantId",
                table: "Shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_StatusHistories_Tenants_TenantId",
                table: "StatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentDetails_Tenants_TenantId",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustments_Tenants_TenantId",
                table: "StockAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Tenants_TenantId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_Stocks_Tenants_TenantId",
                table: "Stocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_Tenants_TenantId",
                table: "Vendors");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Tenants_TenantId",
                table: "Warehouses");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_TenantId_Code",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_TenantId_Name",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_ProductId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ProductId_LocationId_LotNumber_ExpiryDate",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_TenantId_AdjustmentNo",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustmentDetails_TenantId",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropIndex(
                name: "IX_StatusHistories_ActorUserId",
                table: "StatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_StatusHistories_TenantId_ActorUserId_OccurredAtUtc",
                table: "StatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_StatusHistories_TenantId_EntityType_EntityId_OccurredAtUtc",
                table: "StatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrders_TenantId_OrderNo",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrderDetails_TenantId",
                table: "SaleOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_Rmas_TenantId",
                table: "Rmas");

            migrationBuilder.DropIndex(
                name: "IX_RmaDetails_TenantId",
                table: "RmaDetails");

            migrationBuilder.DropIndex(
                name: "IX_Receivings_PurchaseOrderId",
                table: "Receivings");

            migrationBuilder.DropIndex(
                name: "IX_Receivings_TenantId_PurchaseOrderId",
                table: "Receivings");

            migrationBuilder.DropIndex(
                name: "IX_Receivings_TenantId_ReceivingNo",
                table: "Receivings");

            migrationBuilder.DropIndex(
                name: "IX_ReceivingDetails_TenantId",
                table: "ReceivingDetails");

            migrationBuilder.DropIndex(
                name: "IX_PutAwayTasks_TenantId",
                table: "PutAwayTasks");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenantId_PoNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderDetails_TenantId",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Sku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Pickings_TenantId_PickingNo",
                table: "Pickings");

            migrationBuilder.DropIndex(
                name: "IX_PickingDetails_TenantId",
                table: "PickingDetails");

            migrationBuilder.DropIndex(
                name: "IX_Locations_TenantId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_Name",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Categories_TenantId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId_ActorUserId_OccurredAtUtc",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId_EntityType_EntityId_OccurredAtUtc",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AssociationRules_TenantId",
                table: "AssociationRules");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StatusHistories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SaleOrderDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Rmas");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RmaDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "ReceivingDetails");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "ReceivingDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ReceivingDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "PickingDetails");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "PickingDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PickingDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AssociationRules");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Name",
                table: "Vendors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductId_LocationId",
                table: "Stocks",
                columns: new[] { "ProductId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_AdjustmentNo",
                table: "StockAdjustments",
                column: "AdjustmentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_ActorUserId_OccurredAtUtc",
                table: "StatusHistories",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_EntityType_EntityId_OccurredAtUtc",
                table: "StatusHistories",
                columns: new[] { "EntityType", "EntityId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrders_OrderNo",
                table: "SaleOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_PurchaseOrderId",
                table: "Receivings",
                column: "PurchaseOrderId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_ReceivingNo",
                table: "Receivings",
                column: "ReceivingNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PoNumber",
                table: "PurchaseOrders",
                column: "PoNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pickings_PickingNo",
                table: "Pickings",
                column: "PickingNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "OccurredAtUtc" });
        }
    }
}
