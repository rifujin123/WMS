import api from '../lib/axios'
import type { WorkingCapitalRadarDto } from '../types/workingCapital'

export async function getWorkingCapitalRadar(warehouseId?: string): Promise<WorkingCapitalRadarDto> {
  const params = warehouseId ? { warehouseId } : undefined
  return await api.get('/working-capital-radar', { params })
}
