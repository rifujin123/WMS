import { describe, it, expect } from 'vitest'
import {
  SALE_ORDER_STATUS_COLOR,
  SALE_ORDER_STATUS_LABEL,
  computePreOrderStatus,
} from '../lib/statusMaps'

describe('saleOrderStatus - Pre-order & Deposit Status Mapping', () => {
  it('should have Vietnamese labels for AwaitingDeposit, Backorder, and Cancelled', () => {
    expect(SALE_ORDER_STATUS_LABEL.AwaitingDeposit).toBe('Chờ cọc')
    expect(SALE_ORDER_STATUS_LABEL.Backorder).toBe('Đã cọc - Chờ hàng')
    expect(SALE_ORDER_STATUS_LABEL.Cancelled).toBe('Đã hủy')
  })

  it('should have standard status colors mapped', () => {
    expect(SALE_ORDER_STATUS_COLOR.AwaitingDeposit).toBe('orange')
    expect(SALE_ORDER_STATUS_COLOR.Backorder).toBe('cyan')
    expect(SALE_ORDER_STATUS_COLOR.Cancelled).toBe('red')
  })

  it('should correctly determine pre-order status transition based on deposit amounts', () => {
    expect(computePreOrderStatus(200000, 0)).toBe('AwaitingDeposit')
    expect(computePreOrderStatus(200000, 100000)).toBe('AwaitingDeposit')
    expect(computePreOrderStatus(200000, 200000)).toBe('Backorder')
    expect(computePreOrderStatus(200000, 250000)).toBe('Backorder')
  })
})
