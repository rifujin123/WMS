import { describe, it, expect } from 'vitest'
import {
  aggregateStockByProduct,
  selectProductsWithStockAtLocation,
  searchProductRows,
  getLocationDetailsForProduct,
} from '../lib/stockLogic'
import type { StockDto } from '../types/stock'

describe('stockLogic - Inventory Aggregation and Location Details', () => {
  const mockStocks: StockDto[] = [
    {
      id: 'stock-1',
      productId: 'prod-1',
      productSku: 'SKU-01',
      productName: 'Sản phẩm 1',
      warehouseId: 'wh-1',
      warehouseName: 'Kho Chính',
      locationId: 'loc-1',
      locationCode: 'A-01',
      onhandQty: 100,
      reservedQty: 20,
      availableQty: 80,
      lotNumber: 'LOT-A',
      expiryDate: '2026-12-31',
    },
    {
      id: 'stock-2',
      productId: 'prod-1',
      productSku: 'SKU-01',
      productName: 'Sản phẩm 1',
      warehouseId: 'wh-1',
      warehouseName: 'Kho Chính',
      locationId: 'loc-2',
      locationCode: 'A-02',
      onhandQty: 50,
      reservedQty: 0,
      availableQty: 50,
      lotNumber: 'LOT-B',
      expiryDate: '2027-06-30',
    },
    {
      id: 'stock-3',
      productId: 'prod-2',
      productSku: 'SKU-02',
      productName: 'Hàng Điện Tử',
      warehouseId: 'wh-1',
      warehouseName: 'Kho Chính',
      locationId: 'loc-1',
      locationCode: 'A-01',
      onhandQty: 30,
      reservedQty: 10,
      availableQty: 20,
    },
  ]

  it('aggregateStockByProduct: nên gộp đúng tổng Onhand, Reserved và số vị trí', () => {
    const aggregated = aggregateStockByProduct(mockStocks)

    expect(aggregated).toHaveLength(2)

    const prod1 = aggregated.find((p) => p.productId === 'prod-1')
    expect(prod1).toBeDefined()
    expect(prod1?.totalOnhand).toBe(150)
    expect(prod1?.totalReserved).toBe(20)
    expect(prod1?.locationCount).toBe(2)

    const prod2 = aggregated.find((p) => p.productId === 'prod-2')
    expect(prod2?.totalOnhand).toBe(30)
    expect(prod2?.totalReserved).toBe(10)
    expect(prod2?.locationCount).toBe(1)
  })

  it('selectProductsWithStockAtLocation: lọc sản phẩm có mặt tại vị trí chỉ định', () => {
    const aggregated = aggregateStockByProduct(mockStocks)

    // loc-2 chỉ có prod-1
    const atLoc2 = selectProductsWithStockAtLocation(aggregated, mockStocks, 'loc-2')
    expect(atLoc2).toHaveLength(1)
    expect(atLoc2[0].productId).toBe('prod-1')

    // loc-1 có cả prod-1 và prod-2
    const atLoc1 = selectProductsWithStockAtLocation(aggregated, mockStocks, 'loc-1')
    expect(atLoc1).toHaveLength(2)
  })

  it('searchProductRows: tìm kiếm theo SKU hoặc Tên không phân biệt hoa thường', () => {
    const aggregated = aggregateStockByProduct(mockStocks)

    const bySku = searchProductRows(aggregated, 'sku-01')
    expect(bySku).toHaveLength(1)
    expect(bySku[0].productSku).toBe('SKU-01')

    const byName = searchProductRows(aggregated, 'điện tử')
    expect(byName).toHaveLength(1)
    expect(byName[0].productName).toBe('Hàng Điện Tử')
  })

  it('getLocationDetailsForProduct: tính đúng availableQty và giữ nguyên Lot/Expiry', () => {
    const warehouseMap = new Map<string, string>([
      ['loc-1', 'Kho Chính'],
      ['loc-2', 'Kho Chính'],
    ])

    const details = getLocationDetailsForProduct(mockStocks, 'prod-1', warehouseMap)
    expect(details).toHaveLength(2)

    // Sắp theo locationCode: A-01 trước A-02
    expect(details[0].locationCode).toBe('A-01')
    expect(details[0].availableQty).toBe(80) // 100 - 20
    expect(details[0].lotNumber).toBe('LOT-A')
    expect(details[0].expiryDate).toBe('2026-12-31')

    expect(details[1].locationCode).toBe('A-02')
    expect(details[1].availableQty).toBe(50) // 50 - 0
    expect(details[1].lotNumber).toBe('LOT-B')
  })
})
