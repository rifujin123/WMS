import { createContext, useContext, useState } from 'react'
import type { ReactNode } from 'react'
import type { AuthResponse } from '../types/auth'


const TOKEN_KEY = 'accessToken'
const USER_KEY = 'user'

export interface AuthUser{
    username: string
    email: string
    fullName: string
    role: string
    avatarUrl?: string
}

interface AuthContextValue{
    user: AuthUser | null
    login: (res: AuthResponse) => void
    logout: () => void
    updateUser: (patch: Partial<AuthUser>) => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function decodeJwtPayload(token: string): Record<string, unknown> | null {
    try{
        const base64 = token.split('.')[1].replace(/-/g,'+').replace(/_/g,'/')
        const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')
        const binary = atob(padded)
        const json = new TextDecoder().decode(
            Uint8Array.from(binary, c => c.charCodeAt(0)),
        )
        return JSON.parse(json)
    } catch {
        return null
    }
}

function getRoleFromToken(token:string): string{
    const payload = decodeJwtPayload(token)
    if(!payload) return ''
    // .NET phát JWT lưu role claim dưới URI đầy đủ của ClaimTypes.Role (đã xác minh từ token thật)
    const raw = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        ?? payload.role
    if(!raw) return ''
    return (Array.isArray(raw) ? String(raw[0]) : String(raw)).trim()
}

function getInitialUser(): AuthUser | null{
    const token = localStorage.getItem(TOKEN_KEY)
    const saved = localStorage.getItem(USER_KEY)
    if(!token || !saved) return null
    const payload = decodeJwtPayload(token)
    const expired = !payload || typeof payload.exp !== 'number' || payload.exp * 1000 < Date.now()
    if(expired)
    {
        localStorage.removeItem(TOKEN_KEY)
        localStorage.removeItem(USER_KEY)
        return null
    }
    const savedUser = JSON.parse(saved) as AuthUser
    // Refresh role từ token mỗi lần khởi động — sửa trường hợp role bị lưu sai/rỗng trước đó
    return { ...savedUser, role: getRoleFromToken(token) || savedUser.role }
}

export function AuthProvider({children}:{children: ReactNode}){
    const [user, setUser] = useState<AuthUser | null>(getInitialUser)

    const login = (res: AuthResponse) => {
        const nextUser: AuthUser = {
            username: res.username,
            email: res.email,
            fullName: res.fullName,
            role: getRoleFromToken(res.accessToken),
            avatarUrl: res.avatarUrl,
        }
        localStorage.setItem(TOKEN_KEY, res.accessToken)
        localStorage.setItem(USER_KEY, JSON.stringify(nextUser))
        setUser(nextUser)
    }

    const logout = () => {
        localStorage.removeItem(TOKEN_KEY)
        localStorage.removeItem(USER_KEY)
        setUser(null)
    }

    // Cập nhật một phần thông tin user (vd: avatar, fullName) sau khi đổi ở trang Profile
    const updateUser = (patch: Partial<AuthUser>) => {
        setUser((prev) => {
            if (!prev) return prev
            const next = { ...prev, ...patch }
            localStorage.setItem(USER_KEY, JSON.stringify(next))
            return next
        })
    }

    return (
        <AuthContext.Provider value={{user, login, logout, updateUser}}>
            {children}
        </AuthContext.Provider>
    )
}

export function useAuthContext(){
    const ctx = useContext(AuthContext)
    if(!ctx) throw new Error('useAuthContext must be used within an AuthProvider')
    return ctx
}