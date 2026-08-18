// Service trang Thông tin cá nhân — gọi API thật từ backend WMS.API
import api from '../lib/axios'
import type {
  ChangePasswordDto,
  ResetPasswordDto,
  UpdateProfileDto,
  UpdateUserDto,
  UserListItem,
  UserProfile,
} from '../types/user'

export function getUsers(params?: { role?: string; search?: string; status?: string }): Promise<UserListItem[]> {
  return api.get('/Users', { params })
}

export function getWarehouseStaff(): Promise<UserListItem[]> {
  return getUsers({ role: 'WarehouseStaff' })
}

export function updateUser(id: string, dto: UpdateUserDto): Promise<{ message: string }> {
  return api.put(`/Users/${id}`, dto)
}

export function resetUserPassword(id: string, dto: ResetPasswordDto): Promise<{ message: string }> {
  return api.post(`/Users/${id}/reset-password`, dto)
}

export function setUserLock(id: string, locked: boolean): Promise<{ message: string }> {
  return api.patch(`/Users/${id}/lock`, { locked })
}

export function getProfile(): Promise<UserProfile> {
  return api.get('/Users/me')
}

export function updateProfile(
  dto: UpdateProfileDto,
): Promise<UserProfile> {
  return api.put('/Users/me', dto)
}

export function changePassword(
  dto: ChangePasswordDto,
): Promise<{ message: string }> {
  return api.put('/Users/me/password', dto)
}

export function uploadAvatar(file: File): Promise<{ avatarUrl: string }> {
  const form = new FormData()
  form.append('file', file)
  // axios tự set Content-Type: multipart/form-data khi body là FormData
  return api.post('/Users/me/avatar', form)
}