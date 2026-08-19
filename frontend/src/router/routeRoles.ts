export type UserRole = 'Admin' | 'WarehouseManager' | 'WarehouseStaff'

export const allRoles: UserRole[] = ['Admin', 'WarehouseManager', 'WarehouseStaff']

export function isValidRole(role: string): role is UserRole {
  return allRoles.includes(role as UserRole)
}

export function hasRole(role: UserRole | undefined, allowedRoles: UserRole[]) {
  return role !== undefined && allowedRoles.includes(role)
}
