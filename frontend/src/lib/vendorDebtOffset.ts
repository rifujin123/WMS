import type { CreatePurchaseOrderDetailDto } from '../types/purchaseOrder'
import type { VendorDebtLedgerDto } from '../types/vendorDebt'

export interface DebtOffsetCalculationResult {
  rawTotal: number
  creditOffset: number
  finalTotal: number
  appliedDebtIds: string[]
}

/**
 * Tính tổng giá trị gốc của đơn hàng trước khi cấn trừ công nợ
 */
export function calculateRawTotal(
  details: Array<{ orderedQuantity?: number; unitPrice?: number; taxRate?: number }>
): number {
  if (!details || details.length === 0) return 0
  return details.reduce((sum, d) => {
    const qty = Number(d.orderedQuantity ?? 0)
    const price = Number(d.unitPrice ?? 0)
    const tax = Number(d.taxRate ?? 0)
    return sum + qty * price * (1 + tax / 100)
  }, 0)
}

/**
 * Tính số tiền cấn trừ an toàn không vượt quá tổng tiền đơn
 */
export function calculateSafeCreditOffset(
  totalCreditAvailable: number,
  rawTotal: number
): number {
  if (totalCreditAvailable <= 0 || rawTotal <= 0) return 0
  return Math.min(totalCreditAvailable, rawTotal)
}

/**
 * Tính tổng thanh toán thực tế sau khi cấn trừ công nợ
 */
export function calculateFinalTotal(rawTotal: number, creditOffset: number): number {
  return Math.max(0, rawTotal - creditOffset)
}

/**
 * Chuẩn bị các dòng hàng nợ giao bù vào PO với đơn giá 0đ
 */
export function prepareBacklogOrderItems(
  debts: VendorDebtLedgerDto[]
): {
  items: CreatePurchaseOrderDetailDto[]
  appliedDebtIds: string[]
} {
  const items: CreatePurchaseOrderDetailDto[] = []
  const appliedDebtIds: string[] = []

  for (const debt of debts) {
    if (
      (debt.debtType === 'GoodsBacklog' || debt.debtType === 0) &&
      !debt.isResolved &&
      debt.productId &&
      (debt.quantity ?? 0) > 0
    ) {
      items.push({
        productId: debt.productId,
        orderedQuantity: debt.quantity!,
        unitPrice: 0,
        taxRate: 0,
      })
      appliedDebtIds.push(debt.id)
    }
  }

  return { items, appliedDebtIds }
}

/**
 * Kiểm tra tính hợp lệ của số tiền giảm trừ công nợ
 */
export function validateCreditOffset(
  creditOffset: number,
  rawTotal: number
): { isValid: boolean; errorMessage?: string } {
  if (creditOffset < 0) {
    return {
      isValid: false,
      errorMessage: 'Số tiền giảm trừ công nợ không được nhỏ hơn 0.',
    }
  }
  if (creditOffset > rawTotal) {
    return {
      isValid: false,
      errorMessage: 'Số tiền giảm trừ công nợ không được vượt quá tổng giá trị đơn hàng.',
    }
  }
  return { isValid: true }
}
