import { describe, it, expect } from 'vitest'
import {
  buildThreeWayMatchingMatrix,
  buildMatchingMatrixFromReceiving,
  calculateMatchingSummary,
  getConfidenceLevel,
} from '../lib/threeWayMatching'
import type { PurchaseOrderDetailDto } from '../types/purchaseOrder'
import type { ProductMatch } from '../types/invoiceScan'
import type { ReceivingDetailDto } from '../types/receiving'

describe('threeWayMatchingReview - 3-Way Matching Logic and Variance Detection', () => {
  const mockPoDetails: PurchaseOrderDetailDto[] = [
    {
      id: 'pod-1',
      productId: 'prod-1',
      productSku: 'SKU-APPLE',
      productName: 'Táo Envy Mỹ',
      orderedQuantity: 100,
      receivedQuantity: 0,
      unitPrice: 50000,
    },
    {
      id: 'pod-2',
      productId: 'prod-2',
      productSku: 'SKU-ORANGE',
      productName: 'Cam Sành',
      orderedQuantity: 50,
      receivedQuantity: 10, // Còn lại 40
      unitPrice: 30000,
    },
    {
      id: 'pod-3',
      productId: 'prod-3',
      productSku: 'SKU-BANANA',
      productName: 'Chuối Tiêu',
      orderedQuantity: 30,
      receivedQuantity: 0,
      unitPrice: 20000,
    },
  ]

  it('getConfidenceLevel: phân ngưỡng đúng theo spec (>=0.88 xanh, 0.7-0.87 vàng, <0.7 đỏ)', () => {
    expect(getConfidenceLevel(1.0)).toBe('high')
    expect(getConfidenceLevel(0.88)).toBe('high')
    expect(getConfidenceLevel(0.87)).toBe('medium')
    expect(getConfidenceLevel(0.7)).toBe('medium')
    expect(getConfidenceLevel(0.69)).toBe('low')
    expect(getConfidenceLevel(0.95, true)).toBe('low') // Ngoài PO luôn là low
  })

  it('buildThreeWayMatchingMatrix: khớp hoàn toàn khi PO và HĐ bằng nhau', () => {
    const invoiceProducts: ProductMatch[] = [
      {
        productId: 'prod-1',
        sku: 'SKU-APPLE',
        name: 'Táo Envy Mỹ',
        quantity: 100,
        unitPrice: 50000,
        totalAmount: 5000000,
        suggestions: [],
      },
      {
        productId: 'prod-2',
        sku: 'SKU-ORANGE',
        name: 'Cam Sành',
        quantity: 40,
        unitPrice: 30000,
        totalAmount: 1200000,
        suggestions: [],
      },
      {
        productId: 'prod-3',
        sku: 'SKU-BANANA',
        name: 'Chuối Tiêu',
        quantity: 30,
        unitPrice: 20000,
        totalAmount: 600000,
        suggestions: [],
      },
    ]

    const matrix = buildThreeWayMatchingMatrix(mockPoDetails, invoiceProducts)

    expect(matrix).toHaveLength(3)
    const row1 = matrix.find((r) => r.productId === 'prod-1')!
    expect(row1.poQuantity).toBe(100)
    expect(row1.invoiceQuantity).toBe(100)
    expect(row1.actualQuantity).toBe(100)
    expect(row1.quantityDeltaInvoice).toBe(0)
    expect(row1.priceDelta).toBe(0)
    expect(row1.hasQuantityDiscrepancy).toBe(false)
    expect(row1.hasPriceDiscrepancy).toBe(false)
    expect(row1.confidenceLevel).toBe('high')
  })

  it('buildThreeWayMatchingMatrix: phát hiện thiếu trên hóa đơn (Full Outer-Join)', () => {
    // PO có 3 món nhưng hóa đơn chỉ có 2 món (thiếu chuối SKU-BANANA)
    const invoiceProducts: ProductMatch[] = [
      {
        productId: 'prod-1',
        sku: 'SKU-APPLE',
        name: 'Táo Envy Mỹ',
        quantity: 100,
        unitPrice: 50000,
        suggestions: [],
      },
      {
        productId: 'prod-2',
        sku: 'SKU-ORANGE',
        name: 'Cam Sành',
        quantity: 40,
        unitPrice: 30000,
        suggestions: [],
      },
    ]

    const matrix = buildThreeWayMatchingMatrix(mockPoDetails, invoiceProducts)

    expect(matrix).toHaveLength(3)
    const missingRow = matrix.find((r) => r.productId === 'prod-3')!
    expect(missingRow.isMissingInInvoice).toBe(true)
    expect(missingRow.invoiceQuantity).toBe(0)
    expect(missingRow.quantityDeltaInvoice).toBe(-30)
    expect(missingRow.hasQuantityDiscrepancy).toBe(true)
  })

  it('buildThreeWayMatchingMatrix: phát hiện hàng ngoài PO', () => {
    // Hóa đơn có sản phẩm lạ SKU-MANGO không có trong PO
    const invoiceProducts: ProductMatch[] = [
      {
        productId: 'prod-1',
        sku: 'SKU-APPLE',
        name: 'Táo Envy Mỹ',
        quantity: 100,
        unitPrice: 50000,
        suggestions: [],
      },
      {
        sku: 'SKU-MANGO',
        name: 'Xoài Cát Hòa Lộc',
        quantity: 15,
        unitPrice: 45000,
        suggestions: [],
      },
    ]

    const matrix = buildThreeWayMatchingMatrix(mockPoDetails, invoiceProducts)

    const unorderedRow = matrix.find((r) => r.sku === 'SKU-MANGO')!
    expect(unorderedRow).toBeDefined()
    expect(unorderedRow.isUnordered).toBe(true)
    expect(unorderedRow.poQuantity).toBe(0)
    expect(unorderedRow.invoiceQuantity).toBe(15)
    expect(unorderedRow.hasQuantityDiscrepancy).toBe(true)
    expect(unorderedRow.confidenceLevel).toBe('low')
  })

  it('buildThreeWayMatchingMatrix: phát hiện lệch đơn giá và lệch số lượng', () => {
    const invoiceProducts: ProductMatch[] = [
      {
        productId: 'prod-1',
        sku: 'SKU-APPLE',
        name: 'Táo Envy Mỹ',
        quantity: 80, // PO 100, giao 80
        unitPrice: 55000, // PO 50,000, HĐ 55,000
        suggestions: [],
      },
    ]

    const matrix = buildThreeWayMatchingMatrix(mockPoDetails, invoiceProducts)
    const row1 = matrix.find((r) => r.productId === 'prod-1')!

    expect(row1.quantityDeltaInvoice).toBe(-20)
    expect(row1.priceDelta).toBe(5000)
    expect(row1.hasQuantityDiscrepancy).toBe(true)
    expect(row1.hasPriceDiscrepancy).toBe(true)
  })

  it('calculateMatchingSummary: tính tổng tiền và thống kê chênh lệch chuẩn xác', () => {
    const invoiceProducts: ProductMatch[] = [
      {
        productId: 'prod-1',
        sku: 'SKU-APPLE',
        name: 'Táo Envy Mỹ',
        quantity: 80,
        unitPrice: 50000,
        totalAmount: 4000000,
        suggestions: [],
      },
    ]

    const matrix = buildThreeWayMatchingMatrix(mockPoDetails, invoiceProducts)
    const summary = calculateMatchingSummary(matrix)

    expect(summary.hasAnyDiscrepancy).toBe(true)
    expect(summary.discrepancyCount).toBe(3) // prod-1 lệch SL, prod-2 thiếu HĐ, prod-3 thiếu HĐ
    expect(summary.totalInvoiceAmount).toBe(4000000)
  })

  it('buildMatchingMatrixFromReceiving: chuyển đổi từ persisted receiving details ở trang xem lại', () => {
    const receivingDetails: ReceivingDetailDto[] = [
      {
        id: 'rd-1',
        receivingId: 'rc-1',
        productId: 'prod-1',
        productSku: 'SKU-APPLE',
        productName: 'Táo Envy Mỹ',
        expectedQuantity: 100,
        actualQuantity: 95,
        condition: 'Ok',
        unitPrice: 50000,
      },
    ]

    const matrix = buildMatchingMatrixFromReceiving(receivingDetails, mockPoDetails)

    expect(matrix).toHaveLength(1)
    expect(matrix[0].poQuantity).toBe(100)
    expect(matrix[0].actualQuantity).toBe(95)
    expect(matrix[0].hasQuantityDiscrepancy).toBe(true)
  })

  it('buildThreeWayMatchingMatrix: cập nhật đúng actualQuantity và phát hiện lệch khi truyền actualDetails', () => {
    const invoiceProducts: ProductMatch[] = [
      {
        productId: 'prod-1',
        sku: 'SKU-APPLE',
        name: 'Táo Envy Mỹ',
        quantity: 100,
        unitPrice: 50000,
        totalAmount: 5000000,
        suggestions: [],
      },
    ]

    // Form thực nhận đếm được 80 cái (PO 100, HĐ 100, Thực nhận 80)
    const actualDetails = [
      {
        productId: 'prod-1',
        actualQuantity: 80,
        condition: 'Ok' as const,
      },
    ]

    const matrix = buildThreeWayMatchingMatrix(mockPoDetails, invoiceProducts, actualDetails)
    const row1 = matrix.find((r) => r.productId === 'prod-1')!

    expect(row1.actualQuantity).toBe(80)
    expect(row1.hasQuantityDiscrepancy).toBe(true)
    expect(row1.quantityDeltaActual).toBe(-20) // thực nhận 80 - hóa đơn 100 = -20
  })

  it('buildMatchingMatrixFromReceiving: không báo lệch ảo khi đơn PO đã hoàn tất nhận đủ (receivedQuantity = orderedQuantity)', () => {
    const fullyReceivedPoDetails: PurchaseOrderDetailDto[] = [
      {
        id: 'pod-1',
        productId: 'prod-1',
        productSku: 'SKU-APPLE',
        productName: 'Táo Envy Mỹ',
        orderedQuantity: 100,
        receivedQuantity: 100, // Đã nhận đủ
        unitPrice: 50000,
      },
    ]

    const receivingDetails: ReceivingDetailDto[] = [
      {
        id: 'rd-1',
        receivingId: 'rc-1',
        productId: 'prod-1',
        productSku: 'SKU-APPLE',
        productName: 'Táo Envy Mỹ',
        expectedQuantity: 100,
        actualQuantity: 100,
        condition: 'Ok',
        unitPrice: 50000,
      },
    ]

    const matrix = buildMatchingMatrixFromReceiving(receivingDetails, fullyReceivedPoDetails)
    expect(matrix).toHaveLength(1)
    expect(matrix[0].poQuantity).toBe(100)
    expect(matrix[0].actualQuantity).toBe(100)
    expect(matrix[0].hasQuantityDiscrepancy).toBe(false)
  })

  it('buildThreeWayMatchingMatrix: đính kèm DiscrepancyAction trên dòng sai lệch', () => {
    const invoiceProducts: ProductMatch[] = [
      {
        productId: 'prod-1',
        sku: 'SKU-APPLE',
        name: 'Táo Envy Mỹ',
        quantity: 100,
        unitPrice: 50000,
        suggestions: [],
      },
    ]

    const actualDetails = [
      {
        productId: 'prod-1',
        actualQuantity: 80,
        condition: 'Damaged' as const,
        discrepancyAction: 'QuarantineAndHold' as const,
        discrepancyNotes: 'Dập nát 20 hộp',
      },
    ]

    const matrix = buildThreeWayMatchingMatrix(mockPoDetails, invoiceProducts, actualDetails)
    const row1 = matrix.find((r) => r.productId === 'prod-1')!

    expect(row1.condition).toBe('Damaged')
    expect(row1.discrepancyAction).toBe('QuarantineAndHold')
    expect(row1.discrepancyNotes).toBe('Dập nát 20 hộp')

    const summary = calculateMatchingSummary(matrix)
    expect(summary.hasAnyDiscrepancy).toBe(true)
  })

  it('buildMatchingMatrixFromReceiving: lưu vết DiscrepancyAction từ persisted details', () => {
    const receivingDetails: ReceivingDetailDto[] = [
      {
        id: 'rd-1',
        receivingId: 'rc-1',
        productId: 'prod-1',
        productSku: 'SKU-APPLE',
        productName: 'Táo Envy Mỹ',
        expectedQuantity: 100,
        actualQuantity: 0,
        condition: 'Missing',
        discrepancyAction: 'BacklogDeliverLater',
      },
    ]

    const matrix = buildMatchingMatrixFromReceiving(receivingDetails, mockPoDetails)
    expect(matrix).toHaveLength(1)
    expect(matrix[0].discrepancyAction).toBe('BacklogDeliverLater')
    expect(matrix[0].condition).toBe('Missing')
  })
})
