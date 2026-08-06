// Các type dùng cho auth, tên field khớp DTO phía backend
// (backend/WMS-mini/src/WMS.Application/DTOs: LoginDto, RegisterDto, AuthResponseDto)

export interface LoginDto {
  username: string
  password: string
}

export interface RegisterDto {
  fullName: string
  username: string
  email: string
  password: string
  role: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  username: string
  email: string
  fullName: string
  avatarUrl?: string
}

// Type cho form, thêm field chỉ dùng ở frontend
export interface LoginFormValues extends LoginDto {
  remember?: boolean
}

export interface RegisterFormValues extends RegisterDto {
  confirmPassword: string
}