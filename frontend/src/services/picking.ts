import api from '../lib/axios'
import type {
  AssignPickingDto,
  CompletePickingDto,
  CreatePickingDto,
  PickingDto,
} from '../types/picking'

export const getPickings = (params?: { assignToId?: string }): Promise<PickingDto[]> =>
  api.get('/Pickings', { params })

export const getPicking = (id: string): Promise<PickingDto> =>
  api.get(`/Pickings/${id}`)

export const createPicking = (dto: CreatePickingDto): Promise<PickingDto> =>
  api.post('/Pickings', dto)

export const assignPicking = (id: string, dto: AssignPickingDto): Promise<PickingDto> =>
  api.post(`/Pickings/${id}/assign`, dto)

export const startPicking = (id: string): Promise<PickingDto> =>
  api.post(`/Pickings/${id}/start`)

export const completePicking = (id: string, dto: CompletePickingDto): Promise<PickingDto> =>
  api.post(`/Pickings/${id}/complete`, dto)

export const deletePicking = (id: string): Promise<void> =>
  api.delete(`/Pickings/${id}`)
