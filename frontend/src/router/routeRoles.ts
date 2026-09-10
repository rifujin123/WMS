export const allRoles: string[] = ['Admin', 'WarehouseManager', 'WarehouseStaff']

export function isValidRole(role: string): boolean {
  return allRoles.includes(role)
}

export function hasRole(role: string | undefined, allowedRoles: string[]): boolean {
  return role !== undefined && allowedRoles.includes(role)
}
