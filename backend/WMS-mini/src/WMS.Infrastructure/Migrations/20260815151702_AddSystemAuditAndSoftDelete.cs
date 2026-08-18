using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemAuditAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Warehouses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Warehouses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Warehouses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "Stocks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Stocks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Stocks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "StockMovements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StockMovements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "StockMovements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "StockAdjustments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "StockAdjustments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StockAdjustments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "StockAdjustments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "StockAdjustments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "StockAdjustmentDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "StockAdjustmentDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StockAdjustmentDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "StockAdjustmentDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "StockAdjustmentDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Shipments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "SaleOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "SaleOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SaleOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PackedById",
                table: "SaleOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PackedDate",
                table: "SaleOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "SaleOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "SaleOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "SaleOrderDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "SaleOrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SaleOrderDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "SaleOrderDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "SaleOrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "Rmas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Rmas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Rmas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Rmas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Rmas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "RmaDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "RmaDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RmaDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "RmaDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "RmaDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedById",
                table: "Receivings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedDate",
                table: "Receivings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "Receivings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Receivings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Receivings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Receivings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Receivings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "ReceivingDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "ReceivingDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ReceivingDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "ReceivingDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "ReceivingDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedById",
                table: "PutAwayTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "PutAwayTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedById",
                table: "PutAwayTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "PutAwayTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "PutAwayTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "PutAwayTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PutAwayTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "StartedById",
                table: "PutAwayTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedDate",
                table: "PutAwayTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "PutAwayTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "PutAwayTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedById",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedDate",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PurchaseOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "PurchaseOrderDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "PurchaseOrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PurchaseOrderDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "PurchaseOrderDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "PurchaseOrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedById",
                table: "Pickings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "Pickings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedById",
                table: "Pickings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "Pickings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "Pickings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Pickings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Pickings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "StartedById",
                table: "Pickings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedDate",
                table: "Pickings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Pickings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Pickings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "PickingDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "PickingDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PickingDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "PickingDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "PickingDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "Locations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Locations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Locations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Locations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Locations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "Categories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Categories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Categories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Categories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "AssociationRules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "AssociationRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AssociationRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "AssociationRules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "AssociationRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusHistories_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_DeletedById",
                table: "Warehouses",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_UpdatedById",
                table: "Warehouses",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_DeletedById",
                table: "Stocks",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_UpdatedById",
                table: "Stocks",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_DeletedById",
                table: "StockMovements",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_UpdatedById",
                table: "StockMovements",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_DeletedById",
                table: "StockAdjustments",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_UpdatedById",
                table: "StockAdjustments",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentDetails_DeletedById",
                table: "StockAdjustmentDetails",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentDetails_UpdatedById",
                table: "StockAdjustmentDetails",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DeletedById",
                table: "Shipments",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_UpdatedById",
                table: "Shipments",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrders_DeletedById",
                table: "SaleOrders",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrders_PackedById",
                table: "SaleOrders",
                column: "PackedById");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrders_UpdatedById",
                table: "SaleOrders",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrderDetails_DeletedById",
                table: "SaleOrderDetails",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrderDetails_UpdatedById",
                table: "SaleOrderDetails",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Rmas_DeletedById",
                table: "Rmas",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Rmas_UpdatedById",
                table: "Rmas",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RmaDetails_DeletedById",
                table: "RmaDetails",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RmaDetails_UpdatedById",
                table: "RmaDetails",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_ConfirmedById",
                table: "Receivings",
                column: "ConfirmedById");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_DeletedById",
                table: "Receivings",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Receivings_UpdatedById",
                table: "Receivings",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingDetails_DeletedById",
                table: "ReceivingDetails",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingDetails_UpdatedById",
                table: "ReceivingDetails",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PutAwayTasks_AssignedById",
                table: "PutAwayTasks",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_PutAwayTasks_CompletedById",
                table: "PutAwayTasks",
                column: "CompletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PutAwayTasks_DeletedById",
                table: "PutAwayTasks",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PutAwayTasks_StartedById",
                table: "PutAwayTasks",
                column: "StartedById");

            migrationBuilder.CreateIndex(
                name: "IX_PutAwayTasks_UpdatedById",
                table: "PutAwayTasks",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ClosedById",
                table: "PurchaseOrders",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_DeletedById",
                table: "PurchaseOrders",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_UpdatedById",
                table: "PurchaseOrders",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_DeletedById",
                table: "PurchaseOrderDetails",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_UpdatedById",
                table: "PurchaseOrderDetails",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Products_DeletedById",
                table: "Products",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UpdatedById",
                table: "Products",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Pickings_AssignedById",
                table: "Pickings",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_Pickings_CompletedById",
                table: "Pickings",
                column: "CompletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Pickings_DeletedById",
                table: "Pickings",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Pickings_StartedById",
                table: "Pickings",
                column: "StartedById");

            migrationBuilder.CreateIndex(
                name: "IX_Pickings_UpdatedById",
                table: "Pickings",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PickingDetails_DeletedById",
                table: "PickingDetails",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PickingDetails_UpdatedById",
                table: "PickingDetails",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_DeletedById",
                table: "Locations",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_UpdatedById",
                table: "Locations",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_DeletedById",
                table: "Categories",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UpdatedById",
                table: "Categories",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationRules_DeletedById",
                table: "AssociationRules",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationRules_UpdatedById",
                table: "AssociationRules",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_ActorUserId_OccurredAtUtc",
                table: "StatusHistories",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_EntityType_EntityId_OccurredAtUtc",
                table: "StatusHistories",
                columns: new[] { "EntityType", "EntityId", "OccurredAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_AssociationRules_AspNetUsers_DeletedById",
                table: "AssociationRules",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssociationRules_AspNetUsers_UpdatedById",
                table: "AssociationRules",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_DeletedById",
                table: "Categories",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_UpdatedById",
                table: "Categories",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_AspNetUsers_DeletedById",
                table: "Locations",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_AspNetUsers_UpdatedById",
                table: "Locations",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PickingDetails_AspNetUsers_DeletedById",
                table: "PickingDetails",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PickingDetails_AspNetUsers_UpdatedById",
                table: "PickingDetails",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pickings_AspNetUsers_AssignedById",
                table: "Pickings",
                column: "AssignedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pickings_AspNetUsers_CompletedById",
                table: "Pickings",
                column: "CompletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pickings_AspNetUsers_DeletedById",
                table: "Pickings",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pickings_AspNetUsers_StartedById",
                table: "Pickings",
                column: "StartedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pickings_AspNetUsers_UpdatedById",
                table: "Pickings",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AspNetUsers_DeletedById",
                table: "Products",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AspNetUsers_UpdatedById",
                table: "Products",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderDetails_AspNetUsers_DeletedById",
                table: "PurchaseOrderDetails",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderDetails_AspNetUsers_UpdatedById",
                table: "PurchaseOrderDetails",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_ClosedById",
                table: "PurchaseOrders",
                column: "ClosedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_DeletedById",
                table: "PurchaseOrders",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_UpdatedById",
                table: "PurchaseOrders",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PutAwayTasks_AspNetUsers_AssignedById",
                table: "PutAwayTasks",
                column: "AssignedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PutAwayTasks_AspNetUsers_CompletedById",
                table: "PutAwayTasks",
                column: "CompletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PutAwayTasks_AspNetUsers_DeletedById",
                table: "PutAwayTasks",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PutAwayTasks_AspNetUsers_StartedById",
                table: "PutAwayTasks",
                column: "StartedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PutAwayTasks_AspNetUsers_UpdatedById",
                table: "PutAwayTasks",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivingDetails_AspNetUsers_DeletedById",
                table: "ReceivingDetails",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivingDetails_AspNetUsers_UpdatedById",
                table: "ReceivingDetails",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Receivings_AspNetUsers_ConfirmedById",
                table: "Receivings",
                column: "ConfirmedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Receivings_AspNetUsers_DeletedById",
                table: "Receivings",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Receivings_AspNetUsers_UpdatedById",
                table: "Receivings",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RmaDetails_AspNetUsers_DeletedById",
                table: "RmaDetails",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RmaDetails_AspNetUsers_UpdatedById",
                table: "RmaDetails",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rmas_AspNetUsers_DeletedById",
                table: "Rmas",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rmas_AspNetUsers_UpdatedById",
                table: "Rmas",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrderDetails_AspNetUsers_DeletedById",
                table: "SaleOrderDetails",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrderDetails_AspNetUsers_UpdatedById",
                table: "SaleOrderDetails",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrders_AspNetUsers_DeletedById",
                table: "SaleOrders",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrders_AspNetUsers_PackedById",
                table: "SaleOrders",
                column: "PackedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrders_AspNetUsers_UpdatedById",
                table: "SaleOrders",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_AspNetUsers_DeletedById",
                table: "Shipments",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_AspNetUsers_UpdatedById",
                table: "Shipments",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentDetails_AspNetUsers_DeletedById",
                table: "StockAdjustmentDetails",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentDetails_AspNetUsers_UpdatedById",
                table: "StockAdjustmentDetails",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustments_AspNetUsers_DeletedById",
                table: "StockAdjustments",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustments_AspNetUsers_UpdatedById",
                table: "StockAdjustments",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_AspNetUsers_DeletedById",
                table: "StockMovements",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_AspNetUsers_UpdatedById",
                table: "StockMovements",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stocks_AspNetUsers_DeletedById",
                table: "Stocks",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stocks_AspNetUsers_UpdatedById",
                table: "Stocks",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_AspNetUsers_DeletedById",
                table: "Warehouses",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_AspNetUsers_UpdatedById",
                table: "Warehouses",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssociationRules_AspNetUsers_DeletedById",
                table: "AssociationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AssociationRules_AspNetUsers_UpdatedById",
                table: "AssociationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_DeletedById",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_UpdatedById",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Locations_AspNetUsers_DeletedById",
                table: "Locations");

            migrationBuilder.DropForeignKey(
                name: "FK_Locations_AspNetUsers_UpdatedById",
                table: "Locations");

            migrationBuilder.DropForeignKey(
                name: "FK_PickingDetails_AspNetUsers_DeletedById",
                table: "PickingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PickingDetails_AspNetUsers_UpdatedById",
                table: "PickingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Pickings_AspNetUsers_AssignedById",
                table: "Pickings");

            migrationBuilder.DropForeignKey(
                name: "FK_Pickings_AspNetUsers_CompletedById",
                table: "Pickings");

            migrationBuilder.DropForeignKey(
                name: "FK_Pickings_AspNetUsers_DeletedById",
                table: "Pickings");

            migrationBuilder.DropForeignKey(
                name: "FK_Pickings_AspNetUsers_StartedById",
                table: "Pickings");

            migrationBuilder.DropForeignKey(
                name: "FK_Pickings_AspNetUsers_UpdatedById",
                table: "Pickings");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_AspNetUsers_DeletedById",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_AspNetUsers_UpdatedById",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderDetails_AspNetUsers_DeletedById",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderDetails_AspNetUsers_UpdatedById",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_ClosedById",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_DeletedById",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_UpdatedById",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PutAwayTasks_AspNetUsers_AssignedById",
                table: "PutAwayTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_PutAwayTasks_AspNetUsers_CompletedById",
                table: "PutAwayTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_PutAwayTasks_AspNetUsers_DeletedById",
                table: "PutAwayTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_PutAwayTasks_AspNetUsers_StartedById",
                table: "PutAwayTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_PutAwayTasks_AspNetUsers_UpdatedById",
                table: "PutAwayTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivingDetails_AspNetUsers_DeletedById",
                table: "ReceivingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivingDetails_AspNetUsers_UpdatedById",
                table: "ReceivingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Receivings_AspNetUsers_ConfirmedById",
                table: "Receivings");

            migrationBuilder.DropForeignKey(
                name: "FK_Receivings_AspNetUsers_DeletedById",
                table: "Receivings");

            migrationBuilder.DropForeignKey(
                name: "FK_Receivings_AspNetUsers_UpdatedById",
                table: "Receivings");

            migrationBuilder.DropForeignKey(
                name: "FK_RmaDetails_AspNetUsers_DeletedById",
                table: "RmaDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_RmaDetails_AspNetUsers_UpdatedById",
                table: "RmaDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Rmas_AspNetUsers_DeletedById",
                table: "Rmas");

            migrationBuilder.DropForeignKey(
                name: "FK_Rmas_AspNetUsers_UpdatedById",
                table: "Rmas");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrderDetails_AspNetUsers_DeletedById",
                table: "SaleOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrderDetails_AspNetUsers_UpdatedById",
                table: "SaleOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrders_AspNetUsers_DeletedById",
                table: "SaleOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrders_AspNetUsers_PackedById",
                table: "SaleOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrders_AspNetUsers_UpdatedById",
                table: "SaleOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_AspNetUsers_DeletedById",
                table: "Shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_AspNetUsers_UpdatedById",
                table: "Shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentDetails_AspNetUsers_DeletedById",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentDetails_AspNetUsers_UpdatedById",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustments_AspNetUsers_DeletedById",
                table: "StockAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustments_AspNetUsers_UpdatedById",
                table: "StockAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_AspNetUsers_DeletedById",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_AspNetUsers_UpdatedById",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_Stocks_AspNetUsers_DeletedById",
                table: "Stocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Stocks_AspNetUsers_UpdatedById",
                table: "Stocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_AspNetUsers_DeletedById",
                table: "Warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_AspNetUsers_UpdatedById",
                table: "Warehouses");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "StatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_DeletedById",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_UpdatedById",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_DeletedById",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_UpdatedById",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_DeletedById",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_UpdatedById",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_DeletedById",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_UpdatedById",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustmentDetails_DeletedById",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustmentDetails_UpdatedById",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_DeletedById",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_UpdatedById",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrders_DeletedById",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrders_PackedById",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrders_UpdatedById",
                table: "SaleOrders");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrderDetails_DeletedById",
                table: "SaleOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrderDetails_UpdatedById",
                table: "SaleOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_Rmas_DeletedById",
                table: "Rmas");

            migrationBuilder.DropIndex(
                name: "IX_Rmas_UpdatedById",
                table: "Rmas");

            migrationBuilder.DropIndex(
                name: "IX_RmaDetails_DeletedById",
                table: "RmaDetails");

            migrationBuilder.DropIndex(
                name: "IX_RmaDetails_UpdatedById",
                table: "RmaDetails");

            migrationBuilder.DropIndex(
                name: "IX_Receivings_ConfirmedById",
                table: "Receivings");

            migrationBuilder.DropIndex(
                name: "IX_Receivings_DeletedById",
                table: "Receivings");

            migrationBuilder.DropIndex(
                name: "IX_Receivings_UpdatedById",
                table: "Receivings");

            migrationBuilder.DropIndex(
                name: "IX_ReceivingDetails_DeletedById",
                table: "ReceivingDetails");

            migrationBuilder.DropIndex(
                name: "IX_ReceivingDetails_UpdatedById",
                table: "ReceivingDetails");

            migrationBuilder.DropIndex(
                name: "IX_PutAwayTasks_AssignedById",
                table: "PutAwayTasks");

            migrationBuilder.DropIndex(
                name: "IX_PutAwayTasks_CompletedById",
                table: "PutAwayTasks");

            migrationBuilder.DropIndex(
                name: "IX_PutAwayTasks_DeletedById",
                table: "PutAwayTasks");

            migrationBuilder.DropIndex(
                name: "IX_PutAwayTasks_StartedById",
                table: "PutAwayTasks");

            migrationBuilder.DropIndex(
                name: "IX_PutAwayTasks_UpdatedById",
                table: "PutAwayTasks");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_ClosedById",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_DeletedById",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_UpdatedById",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderDetails_DeletedById",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderDetails_UpdatedById",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_Products_DeletedById",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_UpdatedById",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Pickings_AssignedById",
                table: "Pickings");

            migrationBuilder.DropIndex(
                name: "IX_Pickings_CompletedById",
                table: "Pickings");

            migrationBuilder.DropIndex(
                name: "IX_Pickings_DeletedById",
                table: "Pickings");

            migrationBuilder.DropIndex(
                name: "IX_Pickings_StartedById",
                table: "Pickings");

            migrationBuilder.DropIndex(
                name: "IX_Pickings_UpdatedById",
                table: "Pickings");

            migrationBuilder.DropIndex(
                name: "IX_PickingDetails_DeletedById",
                table: "PickingDetails");

            migrationBuilder.DropIndex(
                name: "IX_PickingDetails_UpdatedById",
                table: "PickingDetails");

            migrationBuilder.DropIndex(
                name: "IX_Locations_DeletedById",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Locations_UpdatedById",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Categories_DeletedById",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_UpdatedById",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_AssociationRules_DeletedById",
                table: "AssociationRules");

            migrationBuilder.DropIndex(
                name: "IX_AssociationRules_UpdatedById",
                table: "AssociationRules");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "StockAdjustmentDetails");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "PackedById",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "PackedDate",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "SaleOrders");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "SaleOrderDetails");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "SaleOrderDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SaleOrderDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SaleOrderDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "SaleOrderDetails");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Rmas");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Rmas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Rmas");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Rmas");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Rmas");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "RmaDetails");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "RmaDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RmaDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "RmaDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "RmaDetails");

            migrationBuilder.DropColumn(
                name: "ConfirmedById",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "ConfirmedDate",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Receivings");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "ReceivingDetails");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "ReceivingDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ReceivingDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "ReceivingDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "ReceivingDetails");

            migrationBuilder.DropColumn(
                name: "AssignedById",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "CompletedById",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "StartedById",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "StartedDate",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "PutAwayTasks");

            migrationBuilder.DropColumn(
                name: "ClosedById",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ClosedDate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AssignedById",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "CompletedById",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "StartedById",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "StartedDate",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Pickings");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "PickingDetails");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "PickingDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PickingDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "PickingDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "PickingDetails");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "AssociationRules");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "AssociationRules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AssociationRules");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "AssociationRules");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "AssociationRules");
        }
    }
}
