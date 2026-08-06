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

export interface UpdateProfileDto {
  fullName: string
  phoneNumber?: string
}

export interface ChangePasswordDto {
  currentPassword: string
  newPassword: string
}