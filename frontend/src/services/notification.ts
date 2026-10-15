import api from '../lib/axios'
import type { NotificationDto, NotificationListParams, NotificationUnreadCountDto } from '../types/notification'
import type { PagedResponse } from '../types/pagination'

export async function getNotifications(params?: NotificationListParams): Promise<PagedResponse<NotificationDto>> {
  const { data } = await api.get<PagedResponse<NotificationDto>>('/api/notifications', { params })
  return data
}

export async function getUnreadNotificationCount(): Promise<NotificationUnreadCountDto> {
  const { data } = await api.get<NotificationUnreadCountDto>('/api/notifications/unread-count')
  return data
}

export async function markNotificationAsRead(id: string): Promise<void> {
  await api.put(`/api/notifications/${id}/read`)
}

export async function markAllNotificationsAsRead(): Promise<{ updatedCount: number }> {
  const { data } = await api.put<{ updatedCount: number }>('/api/notifications/read-all')
  return data
}

export async function seedSampleNotifications(): Promise<{ seededCount: number }> {
  const { data } = await api.post<{ seededCount: number }>('/api/notifications/seed-sample')
  return data
}
