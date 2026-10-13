import { describe, expect, it } from 'vitest'
import { calculateCrossDockAllocation } from '../lib/crossDockAllocation'
import type { BackorderLinkDto } from '../types/crossDock'

describe('calculateCrossDockAllocation', () => {
  const mockBaseLink: BackorderLinkDto = {
    id: 'link-1',
    saleOrderDetailId: 'sod-1',
    saleOrderId: 'so-1',
    saleOrderNo: 'SO-001',
    purchaseOrderDetailId: 'pod-1',
    purchaseOrderId: 'po-1',
    purchaseOrderNo: 'PO-001',
    productId: 'prod-1',
    productSku: 'SKU-01',
    productName: 'Sản phẩm 01',
    quantity: 5,
    fulfilledQuantity: 0,
    isFulfilled: false,
    createdDate: '2026-10-13T08:00:00Z',
  }

  it('allocates exact quantity when received equals backorder quantity', () => {
    const result = calculateCrossDockAllocation(5, [mockBaseLink])

    expect(result.totalCrossDockQty).toBe(5)
    expect(result.remainingStorageQty).toBe(0)
    expect(result.crossDockAllocations).toHaveLength(1)
    expect(result.crossDockAllocations[0]).toMatchObject({
      linkId: 'link-1',
      saleOrderNo: 'SO-001',
      allocatedQty: 5,
      isFulfilledAfter: true,
    })
  })

  it('splits between CrossDock and Storage when received exceeds backorder quantity', () => {
    const result = calculateCrossDockAllocation(12, [mockBaseLink])

    expect(result.totalCrossDockQty).toBe(5)
    expect(result.remainingStorageQty).toBe(7)
    expect(result.crossDockAllocations).toHaveLength(1)
    expect(result.crossDockAllocations[0]).toMatchObject({
      linkId: 'link-1',
      allocatedQty: 5,
      isFulfilledAfter: true,
    })
  })

  it('handles partial receipt where received quantity is less than backorder demand', () => {
    const result = calculateCrossDockAllocation(3, [mockBaseLink])

    expect(result.totalCrossDockQty).toBe(3)
    expect(result.remainingStorageQty).toBe(0)
    expect(result.crossDockAllocations).toHaveLength(1)
    expect(result.crossDockAllocations[0]).toMatchObject({
      linkId: 'link-1',
      allocatedQty: 3,
      isFulfilledAfter: false,
    })
  })

  it('allocates in FIFO order across multiple backorder links', () => {
    const linkEarly: BackorderLinkDto = {
      ...mockBaseLink,
      id: 'link-early',
      saleOrderId: 'so-early',
      saleOrderNo: 'SO-EARLY',
      quantity: 4,
      fulfilledQuantity: 0,
      isFulfilled: false,
      createdDate: '2026-10-13T07:00:00Z',
    }
    const linkLate: BackorderLinkDto = {
      ...mockBaseLink,
      id: 'link-late',
      saleOrderId: 'so-late',
      saleOrderNo: 'SO-LATE',
      quantity: 6,
      fulfilledQuantity: 0,
      isFulfilled: false,
      createdDate: '2026-10-13T09:00:00Z',
    }

    // Received 7 items: 4 to early (fully fulfilled), 3 to late (partially fulfilled), 0 to storage
    const result = calculateCrossDockAllocation(7, [linkLate, linkEarly])

    expect(result.totalCrossDockQty).toBe(7)
    expect(result.remainingStorageQty).toBe(0)
    expect(result.crossDockAllocations).toHaveLength(2)

    expect(result.crossDockAllocations[0]).toMatchObject({
      linkId: 'link-early',
      saleOrderNo: 'SO-EARLY',
      allocatedQty: 4,
      isFulfilledAfter: true,
    })

    expect(result.crossDockAllocations[1]).toMatchObject({
      linkId: 'link-late',
      saleOrderNo: 'SO-LATE',
      allocatedQty: 3,
      isFulfilledAfter: false,
    })
  })

  it('skips already fulfilled links', () => {
    const fulfilledLink: BackorderLinkDto = {
      ...mockBaseLink,
      isFulfilled: true,
      fulfilledQuantity: 5,
    }

    const result = calculateCrossDockAllocation(10, [fulfilledLink])

    expect(result.totalCrossDockQty).toBe(0)
    expect(result.remainingStorageQty).toBe(10)
    expect(result.crossDockAllocations).toHaveLength(0)
  })
})
