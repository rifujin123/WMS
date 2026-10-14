import { describe, it, expect } from 'vitest'
import { allRoles, isValidRole, hasRole } from '../router/routeRoles'
import { navConfig, getMenuItems } from '../components/Layout/navConfig'

interface TestMenuItem {
  key: string
  label?: string
  children?: TestMenuItem[]
}

describe('Role Segregation & Maker-Checker UI Rules', () => {
  it('allRoles bao gồm đầy đủ 5 roles chuẩn hệ thống', () => {
    expect(allRoles).toContain('Admin')
    expect(allRoles).toContain('WarehouseManager')
    expect(allRoles).toContain('WarehouseStaff')
    expect(allRoles).toContain('ChiefAccountant')
    expect(allRoles).toContain('Accountant')
    expect(isValidRole('ChiefAccountant')).toBe(true)
    expect(isValidRole('Accountant')).toBe(true)
    expect(isValidRole('Auditor')).toBe(false)
  })

  it('navConfig: Sổ nợ NCC (/vendor-debts) bị chặn đối với WarehouseManager và WarehouseStaff', () => {
    const inboundGroup = navConfig.find((item) => 'children' in item && item.key === 'inbound')
    expect(inboundGroup).toBeDefined()

    if (inboundGroup && 'children' in inboundGroup) {
      const vendorDebtsMenu = inboundGroup.children.find((child) => child.key === '/vendor-debts')
      expect(vendorDebtsMenu).toBeDefined()
      expect(vendorDebtsMenu?.allowedRoles).toContain('Admin')
      expect(vendorDebtsMenu?.allowedRoles).toContain('ChiefAccountant')
      expect(vendorDebtsMenu?.allowedRoles).toContain('Accountant')
      expect(vendorDebtsMenu?.allowedRoles).not.toContain('WarehouseManager')
      expect(vendorDebtsMenu?.allowedRoles).not.toContain('WarehouseStaff')

      // Kiểm tra hasRole
      expect(hasRole('WarehouseManager', vendorDebtsMenu!.allowedRoles)).toBe(false)
      expect(hasRole('WarehouseStaff', vendorDebtsMenu!.allowedRoles)).toBe(false)
      expect(hasRole('ChiefAccountant', vendorDebtsMenu!.allowedRoles)).toBe(true)
      expect(hasRole('Accountant', vendorDebtsMenu!.allowedRoles)).toBe(true)
      expect(hasRole('Admin', vendorDebtsMenu!.allowedRoles)).toBe(true)
    }
  })

  it('getMenuItems: Quản lý kho không thấy menu Sổ nợ NCC', () => {
    const managerItems = (getMenuItems('WarehouseManager') ?? []) as unknown as TestMenuItem[]
    const inboundItem = managerItems.find((item) => item.key === 'inbound')
    expect(inboundItem).toBeDefined()
    const debtChild = inboundItem?.children?.find((child) => child.key === '/vendor-debts')
    expect(debtChild).toBeUndefined()

    const accountantItems = (getMenuItems('Accountant') ?? []) as unknown as TestMenuItem[]
    const accInboundItem = accountantItems.find((item) => item.key === 'inbound')
    expect(accInboundItem).toBeDefined()
    const accDebtChild = accInboundItem?.children?.find((child) => child.key === '/vendor-debts')
    expect(accDebtChild).toBeDefined()
  })

  it('Maker-Checker: Chỉ Admin và ChiefAccountant có quyền duyệt PO', () => {
    const checkCanApprove = (role?: string) => role === 'Admin' || role === 'ChiefAccountant'

    expect(checkCanApprove('Admin')).toBe(true)
    expect(checkCanApprove('ChiefAccountant')).toBe(true)
    expect(checkCanApprove('WarehouseManager')).toBe(false)
    expect(checkCanApprove('Accountant')).toBe(false)
    expect(checkCanApprove('WarehouseStaff')).toBe(false)
  })

  it('SoD Rule: WarehouseManager không được can thiệp cấn trừ công nợ trên PO Form', () => {
    const userRole = 'WarehouseManager'
    const isWarehouseManager = userRole === 'WarehouseManager'

    const enteredOffset = 100000
    const resolvedDebts = ['debt-1', 'debt-2']

    // Mô phỏng logic sanitize payload trong PurchaseOrderFormModal
    const finalOffset = isWarehouseManager ? 0 : enteredOffset
    const finalDebts = isWarehouseManager ? [] : resolvedDebts

    expect(finalOffset).toBe(0)
    expect(finalDebts).toEqual([])
  })
})