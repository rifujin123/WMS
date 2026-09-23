import type { QueryClient } from '@tanstack/react-query'

const inventoryQueryKeyPrefixes = [
  ['stocks'],
  ['stockSummary'],
  ['stockMovements'],
  ['allLocations'],
  ['locations'],
  ['locationsPage'],
] as const

export async function invalidateInventoryQueries(queryClient: QueryClient): Promise<void> {
  await Promise.all(
    inventoryQueryKeyPrefixes.map((queryKey) =>
      queryClient.invalidateQueries({ queryKey }),
    ),
  )
}
