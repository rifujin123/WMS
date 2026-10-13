import type { BackorderLinkDto, CrossDockSplitResult } from '../types/crossDock'

/**
 * Calculates allocation of received OK goods to waiting BackorderLinks.
 * Priority follows FIFO order of link creation.
 */
export function calculateCrossDockAllocation(
  actualReceivedQty: number,
  links: BackorderLinkDto[]
): CrossDockSplitResult {
  const eligibleLinks = links
    .filter((l) => !l.isFulfilled && l.quantity > l.fulfilledQuantity)
    .sort((a, b) => new Date(a.createdDate).getTime() - new Date(b.createdDate).getTime())

  let remainingReceived = Math.max(0, actualReceivedQty)
  let totalCrossDockQty = 0
  const crossDockAllocations = []

  for (const link of eligibleLinks) {
    if (remainingReceived <= 0) break

    const unfulfilled = link.quantity - link.fulfilledQuantity
    const allocatable = Math.min(remainingReceived, unfulfilled)

    if (allocatable > 0) {
      const newFulfilledQty = link.fulfilledQuantity + allocatable
      crossDockAllocations.push({
        linkId: link.id,
        saleOrderId: link.saleOrderId,
        saleOrderNo: link.saleOrderNo,
        productId: link.productId,
        productSku: link.productSku,
        allocatedQty: allocatable,
        isFulfilledAfter: newFulfilledQty >= link.quantity,
      })
      totalCrossDockQty += allocatable
      remainingReceived -= allocatable
    }
  }

  return {
    crossDockAllocations,
    totalCrossDockQty,
    remainingStorageQty: remainingReceived,
  }
}
