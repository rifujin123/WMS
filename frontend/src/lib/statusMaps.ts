import type { PurchaseOrderStatus } from '../types/purchaseOrder'
import type { PickingStatus } from '../types/picking'
import type { PutAwayTaskStatus } from '../types/putAwayTask'
import type { SaleOrderStatus } from '../types/saleOrder'

// Bảng màu trạng thái chuẩn theo docs/issues/ANTD-RULES.md §3:
// Mới/Chờ duyệt/Nháp/Mở = orange · Đang xử lý/Đã phân công = blue · Hoàn thành/Đã duyệt/Đã giao = green · Đã hủy/Hỏng = red · Đã đóng = default

export const SALE_ORDER_STATUS_LABEL: Record<SaleOrderStatus, string> = {
  New: 'Mới',
  Allocated: 'Đã phân bổ',
  Picking: 'Đang lấy hàng',
  Packed: 'Đã đóng gói',
  Shipped: 'Đã giao',
}

export const SALE_ORDER_STATUS_COLOR: Record<SaleOrderStatus, string> = {
  New: 'orange',
  Allocated: 'blue',
  Picking: 'blue',
  Packed: 'green',
  Shipped: 'green',
}

export const PUT_AWAY_STATUS_LABEL: Record<PutAwayTaskStatus, string> = {
  Open: 'Mở',
  Assigned: 'Đã phân công',
  InProgress: 'Đang cất',
  Completed: 'Hoàn thành',
}

export const PUT_AWAY_STATUS_COLOR: Record<PutAwayTaskStatus, string> = {
  Open: 'orange',
  Assigned: 'blue',
  InProgress: 'blue',
  Completed: 'green',
}

export const PICKING_STATUS_LABEL: Record<PickingStatus, string> = {
  Open: 'Mở',
  Assigned: 'Đã phân công',
  InProgress: 'Đang lấy',
  Completed: 'Hoàn thành',
}

export const PICKING_STATUS_COLOR: Record<PickingStatus, string> = {
  Open: 'orange',
  Assigned: 'blue',
  InProgress: 'blue',
  Completed: 'green',
}

export const PURCHASE_ORDER_STATUS_LABEL: Record<PurchaseOrderStatus, string> = {
  Pending: 'Chờ duyệt',
  Approved: 'Đã duyệt',
  Received: 'Đã nhận hàng',
  Closed: 'Đã đóng',
}

export const PURCHASE_ORDER_STATUS_COLOR: Record<PurchaseOrderStatus, string> = {
  Pending: 'orange',
  Approved: 'green',
  Received: 'blue',
  Closed: 'default',
}
