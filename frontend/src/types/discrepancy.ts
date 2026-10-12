export type DiscrepancyAction =
  | 'BacklogDeliverLater'
  | 'CreditOffset'
  | 'CancelShortage'
  | 'QuarantineAndHold'
  | 'ReturnToVendor'

export type DiscrepancyReportStatus = 'Pending' | 'Approved' | 'Rejected'

export interface DiscrepancyReportDto {
  id: string
  receivingId: string
  receivingNo: string
  purchaseOrderId: string
  poNumber?: string
  vendorId?: string
  vendorName?: string
  status: DiscrepancyReportStatus
  approvedById?: string
  approvedByName?: string
  approvedDate?: string
  resolutionSummary?: string
  notes?: string
  evidenceImagesJson?: string
  createdDate: string
}

export interface ApproveDiscrepancyReportDto {
  isApproved: boolean
  notes?: string
}

export const discrepancyActionLabels: Record<DiscrepancyAction, string> = {
  BacklogDeliverLater: 'Giao bù đợt sau',
  CreditOffset: 'Cấn trừ tiền công nợ',
  CancelShortage: 'Chốt nhận đủ, hủy phần thiếu',
  QuarantineAndHold: 'Cất vào khu cách ly',
  ReturnToVendor: 'Trả ngay cho tài xế',
}

export const discrepancyReportStatusLabels: Record<DiscrepancyReportStatus, string> = {
  Pending: 'Chờ duyệt',
  Approved: 'Đã duyệt',
  Rejected: 'Từ chối',
}

export const discrepancyReportStatusColors: Record<DiscrepancyReportStatus, string> = {
  Pending: 'orange',
  Approved: 'green',
  Rejected: 'red',
}
