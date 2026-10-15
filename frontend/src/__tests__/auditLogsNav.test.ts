import { describe, it, expect } from 'vitest'
import { getMenuItems, getGroupKeyForPath } from '../components/Layout/navConfig'

describe('AuditLogs Nav & RBAC Routing', () => {
  it('includes AuditLogs menu item for Admin', () => {
    const items = getMenuItems('Admin')
    const inventoryGroup = items?.find((item) => item?.key === 'inventory') as { children?: { key: string }[] }
    expect(inventoryGroup).toBeDefined()
    const auditItem = inventoryGroup?.children?.find((child) => child.key === '/audit-logs')
    expect(auditItem).toBeDefined()
  })

  it('includes AuditLogs menu item for WarehouseManager', () => {
    const items = getMenuItems('WarehouseManager')
    const inventoryGroup = items?.find((item) => item?.key === 'inventory') as { children?: { key: string }[] }
    expect(inventoryGroup).toBeDefined()
    const auditItem = inventoryGroup?.children?.find((child) => child.key === '/audit-logs')
    expect(auditItem).toBeDefined()
  })

  it('includes AuditLogs menu item for ChiefAccountant', () => {
    const items = getMenuItems('ChiefAccountant')
    const inventoryGroup = items?.find((item) => item?.key === 'inventory') as { children?: { key: string }[] }
    expect(inventoryGroup).toBeDefined()
    const auditItem = inventoryGroup?.children?.find((child) => child.key === '/audit-logs')
    expect(auditItem).toBeDefined()
  })

  it('excludes AuditLogs menu item for WarehouseStaff', () => {
    const items = getMenuItems('WarehouseStaff')
    const inventoryGroup = items?.find((item) => item?.key === 'inventory') as { children?: { key: string }[] }
    expect(inventoryGroup).toBeDefined()
    const auditItem = inventoryGroup?.children?.find((child) => child.key === '/audit-logs')
    expect(auditItem).toBeUndefined()
  })

  it('maps /audit-logs to inventory group in accordion nav', () => {
    const groupKey = getGroupKeyForPath('/audit-logs')
    expect(groupKey).toBe('inventory')
  })
})
