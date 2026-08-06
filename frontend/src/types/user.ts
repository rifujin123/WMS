export interface UserProfile {
  id: string
  username: string
  email: string
  fullName: string
  phoneNumber?: string
  avatarUrl?: string
  createdAt: string
  roles: string[]
}

export interface UpdateProfilePayload {
  fullName: string
  phoneNumber?: string
}

export interface ChangePasswordPayload {
  currentPassword: string
  newPassword: string
}