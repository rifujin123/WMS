import { describe, it, expect } from 'vitest'
import {
  calculateRawTotal,
  calculateSafeCreditOffset,
  calculateFinalTotal,
  prepareBacklogOrderItems,
  validateCreditOffset,
} from '../lib/vendorDebtOffset'
import type { VendorDebtLedgerDto } from '../types/vendorDebt'

describe('vendorDebt - Vendor Debt Ledger & PO Offset Logic', () => {
  it('calculateRawTotal: tính chính xác tổng tiền hàng theo số lượng, đơn giá và thuế', () => {
    const details = [
      { orderedQuantity: 10, unitPrice: 100000, taxRate: 0 },
      { orderedQuantity: 5, unitPrice: 200000, taxRate: 10 },
    ]
    // 10 * 100,000 + 5 * 200,000 * 1.1 = 1,000,000 + 1,100,000 = 2,100,000
    expect(calculateRawTotal(details)).toBe(2100000)
    expect(calculateRawTotal([])).toBe(0)
  })

  it('calculateSafeCreditOffset: cấn trừ an toàn không vượt quá tổng giá trị đơn hàng', () => {
    // Trường hợp 1: Số dư nợ 500k, tổng đơn 1,000k -> trừ đủ 500k
    expect(calculateSafeCreditOffset(500000, 1000000)).toBe(500000)

    // Trường hợp 2: Số dư nợ 1,500k, tổng đơn 1,000k -> chỉ trừ tối đa 1,000k (tránh âm/lãng phí)
    expect(calculateSafeCreditOffset(1500000, 1000000)).toBe(1000000)

    // Trường hợp 3: Đơn hàng 0đ hoặc nợ 0đ -> không cấn trừ
    expect(calculateSafeCreditOffset(0, 1000000)).toBe(0)
    expect(calculateSafeCreditOffset(500000, 0)).toBe(0)
  })

  it('calculateFinalTotal: tính chính xác số tiền cần thanh toán thực tế', () => {
    const rawTotal = 2000000
    const creditOffset = 500000
    expect(calculateFinalTotal(rawTotal, creditOffset)).toBe(1500000)

    // Không âm khi offset lớn hơn
    expect(calculateFinalTotal(500000, 600000)).toBe(0)
  })

  it('prepareBacklogOrderItems: 1-click chuyển hàng nợ thành dòng PO với đơn giá 0đ', () => {
    const mockDebts: VendorDebtLedgerDto[] = [
      {
        id: 'debt-1',
        vendorId: 'vendor-1',
        sourceReceivingId: 'rec-1',
        debtType: 'GoodsBacklog',
        productId: 'prod-apple',
        productSku: 'SKU-APPLE',
        quantity: 10,
        isResolved: false,
        createdDate: '2026-10-13T00:00:00Z',
      },
      {
        id: 'debt-2',
        vendorId: 'vendor-1',
        sourceReceivingId: 'rec-1',
        debtType: 'CreditBalance',
        amount: 300000,
        isResolved: false,
        createdDate: '2026-10-13T00:00:00Z',
      },
      {
        id: 'debt-3',
        vendorId: 'vendor-1',
        sourceReceivingId: 'rec-2',
        debtType: 'GoodsBacklog',
        productId: 'prod-orange',
        productSku: 'SKU-ORANGE',
        quantity: 5,
        isResolved: true, // Đã bù rồi
        createdDate: '2026-10-12T00:00:00Z',
      },
    ]

    const { items, appliedDebtIds } = prepareBacklogOrderItems(mockDebts)

    // Chỉ chọn debt-1 (GoodsBacklog và chưa bù)
    expect(items).toHaveLength(1)
    expect(items[0]).toEqual({
      productId: 'prod-apple',
      orderedQuantity: 10,
      unitPrice: 0,
      taxRate: 0,
    })
    expect(appliedDebtIds).toEqual(['debt-1'])
  })

  it('validateCreditOffset: kiểm tra tính hợp lệ của số tiền cấn trừ', () => {
    // Hợp lệ
    expect(validateCreditOffset(500000, 1000000).isValid).toBe(true)

    // Không hợp lệ vì nhỏ hơn 0
    const negativeCheck = validateCreditOffset(-100, 1000000)
    expect(negativeCheck.isValid).toBe(false)
    expect(negativeCheck.errorMessage).toContain('không được nhỏ hơn 0')

    // Không hợp lệ vì vượt quá tổng giá trị đơn
    const exceedCheck = validateCreditOffset(1500000, 1000000)
    expect(exceedCheck.isValid).toBe(false)
    expect(exceedCheck.errorMessage).toContain('không được vượt quá tổng giá trị đơn hàng')
  })
})
