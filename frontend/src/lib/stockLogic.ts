import type { StockDto } from '../types/stock'

// Dòng sản phẩm đã gộp (danh sách chính của màn hình Tồn kho)
export interface StockProductRow {
  productId: string
  productSku: string
  productName: string
  totalOnhand: number
  totalReserved: number
  locationCount: number
}

// Dòng chi tiết theo vị trí (trong drawer)
export interface StockLocationRow {
  stockId: string
  locationId: string
  locationCode: string
  warehouseName: string
  onhandQty: number
  reservedQty: number
  availableQty: number
}

/** Gộp các dòng tồn (sản phẩm × vị trí) thành danh sách theo sản phẩm. */
export function aggregateStockByProduct(stocks: StockDto[]): StockProductRow[] {
  const map = new Map<string, StockProductRow>()
  for (const stock of stocks) {
    const existing = map.get(stock.productId)
    if (existing) {
      existing.totalOnhand += stock.onhandQty
      existing.totalReserved += stock.reservedQty
      existing.locationCount += 1
    } else {
      map.set(stock.productId, {
        productId: stock.productId,
        productSku: stock.productSku,
        productName: stock.productName,
        totalOnhand: stock.onhandQty,
        totalReserved: stock.reservedQty,
        locationCount: 1,
      })
    }
  }
  return [...map.values()]
}

/**
 * Chọn các sản phẩm CÓ tồn tại vị trí đã chọn (quyết định sản phẩm nào xuất hiện).
 * Không có locationId thì giữ nguyên (không lọc).
 * Số liệu của dòng vẫn là tổng toàn kho — filter chỉ quyết định sản phẩm nào xuất hiện.
 */
export function selectProductsWithStockAtLocation(
  rows: StockProductRow[],
  stocks: StockDto[],
  locationId: string | undefined,
): StockProductRow[] {
  if (!locationId) return rows
  const productIdsAtLocation = new Set(
    stocks.filter((s) => s.locationId === locationId).map((s) => s.productId),
  )
  return rows.filter((r) => productIdsAtLocation.has(r.productId))
}

/** Tìm theo SKU hoặc tên sản phẩm (chứa chuỗi, không phân biệt hoa/thường). */
export function searchProductRows(rows: StockProductRow[], keyword: string): StockProductRow[] {
  const normalized = keyword.trim().toLowerCase()
  if (!normalized) return rows
  return rows.filter(
    (r) =>
      r.productSku.toLowerCase().includes(normalized) ||
      r.productName.toLowerCase().includes(normalized),
  )
}

/** Chi tiết các vị trí chứa 1 sản phẩm, sắp theo mã vị trí. */
export function getLocationDetailsForProduct(
  stocks: StockDto[],
  productId: string,
  warehouseNameByLocationId: Map<string, string>,
): StockLocationRow[] {
  return stocks
    .filter((s) => s.productId === productId)
    .map((s) => ({
      stockId: s.id,
      locationId: s.locationId,
      locationCode: s.locationCode,
      warehouseName: warehouseNameByLocationId.get(s.locationId) ?? '—',
      onhandQty: s.onhandQty,
      reservedQty: s.reservedQty,
      availableQty: s.onhandQty - s.reservedQty,
    }))
    .sort((a, b) => a.locationCode.localeCompare(b.locationCode))
}
