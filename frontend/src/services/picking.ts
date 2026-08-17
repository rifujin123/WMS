import api from '../lib/axios'
import type { PickingDto } from '../types/picking'

export const getPickings = (): Promise<PickingDto[]> => api.get('/Pickings')
