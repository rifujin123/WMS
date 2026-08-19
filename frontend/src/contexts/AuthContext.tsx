import { useState } from 'react'
import type { ReactNode } from 'react'
import type { AuthResponse } from '../types/auth'
import { isValidRole, type UserRole } from '../router/routeRoles'
import { AuthContext, type AuthUser } from './authContextValue'

const TOKEN_KEY = 'accessToken'
const USER_KEY = 'user'

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

function getRoleFromToken(token: string): UserRole | undefined {
    const payload = decodeJwtPayload(token)
    if (!payload) return undefined

    // .NET phát JWT lưu role claim dưới URI đầy đủ của ClaimTypes.Role.
    const raw = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        ?? payload.role
    const role = Array.isArray(raw) ? String(raw[0]).trim() : String(raw ?? '').trim()

    return isValidRole(role) ? role : undefined
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
    try {
        const savedUser = JSON.parse(saved) as AuthUser
        const role = getRoleFromToken(token)
        if (!role) {
            localStorage.removeItem(TOKEN_KEY)
            localStorage.removeItem(USER_KEY)
            return null
        }

        // Refresh role từ token mỗi lần khởi động.
        return { ...savedUser, role }
    } catch {
        localStorage.removeItem(TOKEN_KEY)
        localStorage.removeItem(USER_KEY)
        return null
    }
}

export function AuthProvider({children}:{children: ReactNode}){
    const [user, setUser] = useState<AuthUser | null>(getInitialUser)

    const login = (res: AuthResponse) => {
        const role = getRoleFromToken(res.accessToken)
        if (!role) {
            localStorage.removeItem(TOKEN_KEY)
            localStorage.removeItem(USER_KEY)
            setUser(null)
            return
        }

        const nextUser: AuthUser = {
            username: res.username,
            email: res.email,
            fullName: res.fullName,
            role,
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
