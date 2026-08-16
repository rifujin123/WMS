// Service trang Thông tin cá nhân — gọi API thật từ backend WMS.API
import api from '../lib/axios'
import type {
  ChangePasswordDto,
  UpdateProfileDto,
  UserListItem,
  UserProfile,
} from '../types/user'

export function getUsers(): Promise<UserListItem[]> {
  return api.get('/Users')
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