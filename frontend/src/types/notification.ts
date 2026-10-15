export type NotificationType = 'TaskAssigned' | 'ApprovalRequired' | 'Reminder' | 'System'

export interface NotificationDto {
  id: string
  tenantId: string
  userId?: string
  targetRole?: string
  title: string
  message: string
  type: NotificationType
  actionUrl?: string
  isRead: boolean
  createdAtUtc: string
}

export interface NotificationListParams {
  page?: number
  pageSize?: number
  unreadOnly?: boolean
  type?: string
}

export interface NotificationUnreadCountDto {
  count: number
}
