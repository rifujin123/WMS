import { createContext } from 'react'
import type { AuthResponse } from '../types/auth'
import type { UserRole } from '../router/routeRoles'

export interface AuthUser {
  username: string
  email: string
  fullName: string
  role: UserRole
  avatarUrl?: string
}

export interface AuthContextValue {
  user: AuthUser | null
  login: (response: AuthResponse) => void
  logout: () => void
  updateUser: (patch: Partial<AuthUser>) => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
