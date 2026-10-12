import type { PurchaseOrderDetailDto } from '../types/purchaseOrder'
import type { ProductMatch, ProductSuggestion } from '../types/invoiceScan'
import type { ProductCondition, ReceivingDetailDto } from '../types/receiving'
import type { DiscrepancyAction } from '../types/discrepancy'

export interface ThreeWayMatchingRow {
  key: string
  productId?: string
  sku: string
  productName: string
  poQuantity: number
  poUnitPrice: number
  poTotalAmount: number
  invoiceQuantity: number
  invoiceUnitPrice: number
  invoiceTotalAmount: number
  actualQuantity: number
  actualTotalAmount: number
  condition: ProductCondition
  quantityDeltaInvoice: number
  quantityDeltaActual: number
  priceDelta: number
  confidenceScore: number
  confidenceLevel: 'high' | 'medium' | 'low'
  isMissingInInvoice: boolean
  isUnordered: boolean
  hasQuantityDiscrepancy: boolean
  hasPriceDiscrepancy: boolean
  lotNumber?: string
  expiryDate?: string
  discrepancyAction?: DiscrepancyAction
  discrepancyNotes?: string
  suggestions?: ProductSuggestion[]
}

export interface ThreeWayMatchingSummary {
  totalPoAmount: number
  totalInvoiceAmount: number
  totalActualAmount: number
  hasAnyDiscrepancy: boolean
  discrepancyCount: number
}

export function getConfidenceLevel(score: number, isUnordered = false): 'high' | 'medium' | 'low' {
  if (isUnordered || score < 0.7) return 'low'
  if (score < 0.88) return 'medium'
  return 'high'
}

function getPoRemainingQuantity(detail: PurchaseOrderDetailDto): number {
  return Math.max(detail.orderedQuantity - detail.receivedQuantity, 0)
}

export function buildThreeWayMatchingMatrix(
  poDetails: PurchaseOrderDetailDto[] = [],
  invoiceProducts: ProductMatch[] = [],
  actualDetails: Array<{
    productId?: string
    actualQuantity?: number
    condition?: ProductCondition
    unitPrice?: number
    expectedQuantity?: number
    discrepancyAction?: DiscrepancyAction
    discrepancyNotes?: string
  }> = [],
): ThreeWayMatchingRow[] {
  const rows: ThreeWayMatchingRow[] = []
  const matchedInvoiceIndices = new Set<number>()

  // 1. Process PO details (Matched + Missing in Invoice)
  poDetails.forEach((po) => {
    const poQty = getPoRemainingQuantity(po)
    const poPrice = po.unitPrice ?? 0
    const normPoSku = (po.productSku ?? '').trim().toLowerCase()

    const invIdx = invoiceProducts.findIndex((inv, idx) => {
      if (matchedInvoiceIndices.has(idx)) return false
      if (inv.productId && inv.productId === po.productId) return true
      return inv.sku && inv.sku.trim().toLowerCase() === normPoSku
    })

    const actual = po.productId ? actualDetails.find((a) => a.productId === po.productId) : undefined

    if (invIdx >= 0) {
      matchedInvoiceIndices.add(invIdx)
      const inv = invoiceProducts[invIdx]
      const invQty = inv.quantity ?? 0
      const invPrice = inv.unitPrice ?? poPrice
      const actualQty = actual?.actualQuantity ?? invQty
      const condition = actual?.condition ?? 'Ok'
      const confidence = inv.confidenceScore ?? 1.0 // Short-circuit exact SKU match
      const confLevel = getConfidenceLevel(confidence, false)

      const qDeltaInv = invQty - poQty
      const qDeltaAct = actualQty - invQty
      const pDelta = poPrice > 0 && invPrice > 0 ? invPrice - poPrice : 0
      const hasQDisc = poQty !== invQty || actualQty !== poQty
      const hasPDisc = poPrice > 0 && invPrice > 0 && poPrice !== invPrice

      rows.push({
        key: po.productId || po.productSku,
        productId: po.productId,
        sku: po.productSku,
        productName: po.productName,
        poQuantity: poQty,
        poUnitPrice: poPrice,
        poTotalAmount: poQty * poPrice,
        invoiceQuantity: invQty,
        invoiceUnitPrice: invPrice,
        invoiceTotalAmount: inv.totalAmount ?? invQty * invPrice,
        actualQuantity: actualQty,
        actualTotalAmount: actualQty * (poPrice || invPrice),
        condition,
        quantityDeltaInvoice: qDeltaInv,
        quantityDeltaActual: qDeltaAct,
        priceDelta: pDelta,
        confidenceScore: confidence,
        confidenceLevel: confLevel,
        isMissingInInvoice: false,
        isUnordered: false,
        hasQuantityDiscrepancy: hasQDisc,
        hasPriceDiscrepancy: hasPDisc,
        discrepancyAction: actual?.discrepancyAction,
        discrepancyNotes: actual?.discrepancyNotes,
        suggestions: inv.suggestions,
      })
    } else {
      // In PO but missing on invoice
      const actualQty = actual?.actualQuantity ?? 0
      const condition = actual?.condition ?? 'Missing'

      rows.push({
        key: po.productId || po.productSku,
        productId: po.productId,
        sku: po.productSku,
        productName: po.productName,
        poQuantity: poQty,
        poUnitPrice: poPrice,
        poTotalAmount: poQty * poPrice,
        invoiceQuantity: 0,
        invoiceUnitPrice: 0,
        invoiceTotalAmount: 0,
        actualQuantity: actualQty,
        actualTotalAmount: actualQty * poPrice,
        condition,
        quantityDeltaInvoice: -poQty,
        quantityDeltaActual: actualQty,
        priceDelta: 0,
        confidenceScore: 0,
        confidenceLevel: 'low',
        isMissingInInvoice: true,
        isUnordered: false,
        hasQuantityDiscrepancy: true,
        hasPriceDiscrepancy: false,
        discrepancyAction: actual?.discrepancyAction,
        discrepancyNotes: actual?.discrepancyNotes,
      })
    }
  })

  // 2. Process Invoice items not in PO (Ngoài PO)
  invoiceProducts.forEach((inv, idx) => {
    if (matchedInvoiceIndices.has(idx)) return

    const actual = actualDetails.find((a) => a.productId && a.productId === inv.productId)
    const invQty = inv.quantity ?? 0
    const invPrice = inv.unitPrice ?? 0
    const actualQty = actual?.actualQuantity ?? invQty
    const condition = actual?.condition ?? 'Ok'
    const confidence = inv.confidenceScore ?? (inv.suggestions?.length ? 0.75 : 0.5)

    rows.push({
      key: `inv-unordered-${idx}-${inv.sku}`,
      productId: inv.productId,
      sku: inv.sku || 'N/A',
      productName: inv.name || 'Sản phẩm ngoài PO',
      poQuantity: 0,
      poUnitPrice: 0,
      poTotalAmount: 0,
      invoiceQuantity: invQty,
      invoiceUnitPrice: invPrice,
      invoiceTotalAmount: inv.totalAmount ?? invQty * invPrice,
      actualQuantity: actualQty,
      actualTotalAmount: actualQty * invPrice,
      condition,
      quantityDeltaInvoice: invQty,
      quantityDeltaActual: actualQty - invQty,
      priceDelta: 0,
      confidenceScore: confidence,
      confidenceLevel: 'low',
      isMissingInInvoice: false,
      isUnordered: true,
      hasQuantityDiscrepancy: true,
      hasPriceDiscrepancy: false,
      discrepancyAction: actual?.discrepancyAction,
      discrepancyNotes: actual?.discrepancyNotes,
      suggestions: inv.suggestions,
    })
  })

  return rows
}

export function buildMatchingMatrixFromReceiving(
  receivingDetails: ReceivingDetailDto[] = [],
  poDetails: PurchaseOrderDetailDto[] = [],
  includeMissingPoItems = false,
): ThreeWayMatchingRow[] {
  // ponytail: view-mode mapping from persisted receiving details; upgrade when audit history API arrives
  const poMap = new Map<string, PurchaseOrderDetailDto>()
  poDetails.forEach((p) => poMap.set(p.productId, p))

  const rows: ThreeWayMatchingRow[] = receivingDetails.map((rd, idx) => {
    const po = poMap.get(rd.productId)
    // For historical inspection, baseline is original PO ordered quantity or expectedQuantity on receipt
    const poQty = rd.expectedQuantity > 0 ? rd.expectedQuantity : (po?.orderedQuantity ?? 0)
    const poPrice = po?.unitPrice ?? rd.unitPrice ?? 0
    const invQty = rd.actualQuantity
    const invPrice = rd.unitPrice ?? poPrice

    const qDeltaInv = invQty - poQty
    const qDeltaAct = 0
    const pDelta = poPrice > 0 && invPrice > 0 ? invPrice - poPrice : 0
    const hasQDisc = poQty !== rd.actualQuantity
    const hasPDisc = poPrice > 0 && invPrice > 0 && poPrice !== invPrice

    return {
      key: rd.id || `rd-${idx}`,
      productId: rd.productId,
      sku: rd.productSku,
      productName: rd.productName,
      poQuantity: poQty,
      poUnitPrice: poPrice,
      poTotalAmount: poQty * poPrice,
      invoiceQuantity: invQty,
      invoiceUnitPrice: invPrice,
      invoiceTotalAmount: invQty * invPrice,
      actualQuantity: rd.actualQuantity,
      actualTotalAmount: rd.actualQuantity * (rd.unitPrice ?? poPrice),
      condition: rd.condition,
      quantityDeltaInvoice: qDeltaInv,
      quantityDeltaActual: qDeltaAct,
      priceDelta: pDelta,
      confidenceScore: 1.0,
      confidenceLevel: 'high',
      isMissingInInvoice: false,
      isUnordered: false,
      hasQuantityDiscrepancy: hasQDisc,
      hasPriceDiscrepancy: hasPDisc,
      lotNumber: rd.lotNumber,
      expiryDate: rd.expiryDate,
      discrepancyAction: rd.discrepancyAction,
      discrepancyNotes: rd.discrepancyNotes,
    }
  })

  // Full Outer-Join: include PO items not received at all when requested
  if (includeMissingPoItems) {
    const receivedProductIds = new Set(receivingDetails.map((r) => r.productId))
    poDetails.forEach((po) => {
      if (!receivedProductIds.has(po.productId)) {
        const poQty = po.orderedQuantity
        const poPrice = po.unitPrice ?? 0
        rows.push({
          key: `missing-po-${po.productId}`,
          productId: po.productId,
          sku: po.productSku,
          productName: po.productName,
          poQuantity: poQty,
          poUnitPrice: poPrice,
          poTotalAmount: poQty * poPrice,
          invoiceQuantity: 0,
          invoiceUnitPrice: 0,
          invoiceTotalAmount: 0,
          actualQuantity: 0,
          actualTotalAmount: 0,
          condition: 'Missing',
          quantityDeltaInvoice: -poQty,
          quantityDeltaActual: 0,
          priceDelta: 0,
          confidenceScore: 0,
          confidenceLevel: 'low',
          isMissingInInvoice: true,
          isUnordered: false,
          hasQuantityDiscrepancy: true,
          hasPriceDiscrepancy: false,
        })
      }
    })
  }

  return rows
}

export function calculateMatchingSummary(rows: ThreeWayMatchingRow[]): ThreeWayMatchingSummary {
  let totalPoAmount = 0
  let totalInvoiceAmount = 0
  let totalActualAmount = 0
  let discrepancyCount = 0

  rows.forEach((r) => {
    totalPoAmount += r.poTotalAmount
    totalInvoiceAmount += r.invoiceTotalAmount
    totalActualAmount += r.actualTotalAmount
    if (r.hasQuantityDiscrepancy || r.hasPriceDiscrepancy || r.isMissingInInvoice || r.isUnordered || r.condition !== 'Ok' || !!r.discrepancyAction) {
      discrepancyCount += 1
    }
  })

  return {
    totalPoAmount,
    totalInvoiceAmount,
    totalActualAmount,
    hasAnyDiscrepancy: discrepancyCount > 0,
    discrepancyCount,
  }
}
