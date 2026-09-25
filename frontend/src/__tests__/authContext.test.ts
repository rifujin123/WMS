import { describe, it, expect } from 'vitest'
import { isValidRole } from '../router/routeRoles'

function createMockJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
  const body = btoa(JSON.stringify(payload))
  return `${header}.${body}.mock-signature`
}

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')
    const binary = atob(padded)
    const json = new TextDecoder().decode(
      Uint8Array.from(binary, (c) => c.charCodeAt(0)),
    )
    return JSON.parse(json)
  } catch {
    return null
  }
}

describe('AuthContext - JWT Claims & Role Validation', () => {
  it('decodeJwtPayload: giải mã đúng payload JWT', () => {
    const mockPayload = {
      tenant_id: 'tenant-123',
      tenant_code: 'logistics-alpha',
      has_expiry_management: true,
      role: 'Admin',
      sub: 'admin-user',
    }
    const token = createMockJwt(mockPayload)
    const decoded = decodeJwtPayload(token)

    expect(decoded).not.toBeNull()
    expect(decoded?.tenant_id).toBe('tenant-123')
    expect(decoded?.has_expiry_management).toBe(true)
    expect(decoded?.tenant_code).toBe('logistics-alpha')
  })

  it('isValidRole: chỉ chấp nhận các vai trò hợp lệ trong hệ thống', () => {
    expect(isValidRole('Admin')).toBe(true)
    expect(isValidRole('WarehouseManager')).toBe(true)
    expect(isValidRole('WarehouseStaff')).toBe(true)

    // Vai trò không hợp lệ
    expect(isValidRole('SuperAdmin')).toBe(false)
    expect(isValidRole('Customer')).toBe(false)
    expect(isValidRole('')).toBe(false)
  })

  it('xử lý cờ has_expiry_management dạng boolean hoặc chuỗi', () => {
    const tokenTrueStr = createMockJwt({ has_expiry_management: 'true' })
    const tokenTrueBool = createMockJwt({ has_expiry_management: true })
    const tokenFalse = createMockJwt({ has_expiry_management: false })

    const p1 = decodeJwtPayload(tokenTrueStr)
    const p2 = decodeJwtPayload(tokenTrueBool)
    const p3 = decodeJwtPayload(tokenFalse)

    const isFefo1 = p1?.has_expiry_management === true || p1?.has_expiry_management === 'true'
    const isFefo2 = p2?.has_expiry_management === true || p2?.has_expiry_management === 'true'
    const isFefo3 = p3?.has_expiry_management === true || p3?.has_expiry_management === 'true'

    expect(isFefo1).toBe(true)
    expect(isFefo2).toBe(true)
    expect(isFefo3).toBe(false)
  })
})
