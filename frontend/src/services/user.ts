// Service trang Thông tin cá nhân — gọi API thật từ backend WMS.API
import api from '../lib/axios'
import type {
  ChangePasswordPayload,
  UpdateProfilePayload,
  UserProfile,
} from '../types/user'

export function getProfile(): Promise<UserProfile> {
  return api.get('/Users/me').then((res) => res.data)
}

export function updateProfile(
  payload: UpdateProfilePayload,
): Promise<UserProfile> {
  return api.put('/Users/me', payload).then((res) => res.data)
}

export function changePassword(
  payload: ChangePasswordPayload,
): Promise<{ message: string }> {
  return api.put('/Users/me/password', payload).then((res) => res.data)
}

export function uploadAvatar(file: File): Promise<{ avatarUrl: string }> {
  const form = new FormData()
  form.append('file', file)
  // axios tự set Content-Type: multipart/form-data khi body là FormData
  return api.post('/Users/me/avatar', form).then((res) => res.data)
}