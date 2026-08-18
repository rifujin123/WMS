export type UserRole = 'Admin' | 'WarehouseManager' | 'WarehouseStaff'
export type UserStatus = 'active' | 'locked'

export interface UserListItem {
  id: string
  username: string
  email: string
  fullName: string
  avatarUrl?: string
  role: UserRole
  status: UserStatus
  createdAt: string
}

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

export interface UpdateUserDto {
  fullName: string
  email: string
  role: UserRole
}

export interface ResetPasswordDto {
  newPassword: string
}