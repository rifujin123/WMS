import { describe, it, expect } from 'vitest'
import { getMenuItems, getGroupKeyForPath } from '../components/Layout/navConfig'

describe('StockTransfer Nav & RBAC Routing', () => {
  it('includes /transfers menu item for Admin and WarehouseManager', () => {
    const adminItems = getMenuItems('Admin')
    const invGroup = adminItems?.find((item) => item?.key === 'inventory') as { children?: { key: string }[] }
    expect(invGroup).toBeDefined()
    const transferItem = invGroup?.children?.find((c) => c.key === '/transfers')
    expect(transferItem).toBeDefined()

    const mgrItems = getMenuItems('WarehouseManager')
    const mgrInv = mgrItems?.find((item) => item?.key === 'inventory') as { children?: { key: string }[] }
    expect(mgrInv?.children?.find((c) => c.key === '/transfers')).toBeDefined()
  })

  it('excludes /transfers menu item for WarehouseStaff', () => {
    const staffItems = getMenuItems('WarehouseStaff')
    const invGroup = staffItems?.find((item) => item?.key === 'inventory') as { children?: { key: string }[] }
    expect(invGroup?.children?.find((c) => c.key === '/transfers')).toBeUndefined()
  })

  it('maps /transfers to inventory group in accordion nav', () => {
    const groupKey = getGroupKeyForPath('/transfers')
    expect(groupKey).toBe('inventory')
  })
})
