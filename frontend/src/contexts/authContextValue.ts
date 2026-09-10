import { createContext } from 'react'
import type { AuthResponse } from '../types/auth'
export interface AuthUser {
  username: string
  email: string
  fullName: string
  role: string
  avatarUrl?: string
}

export interface AuthContextValue {
  user: AuthUser | null
  login: (response: AuthResponse) => void
  logout: () => void
  updateUser: (patch: Partial<AuthUser>) => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
