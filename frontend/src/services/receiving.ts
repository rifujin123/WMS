import api from '../lib/axios'
import type { ReceivingDto, CreateReceivingDto, ReceivingStatus } from '../types/receiving'
import type { PagedResponse } from '../types/pagination'

export interface ReceivingListParams {
  page: number
  search?: string
  status?: ReceivingStatus
}

export const getReceivings = (): Promise<ReceivingDto[]> =>
  api.get('/Receivings/lookup')

export const getReceivingsPage = (params: ReceivingListParams): Promise<PagedResponse<ReceivingDto>> =>
  api.get('/Receivings', { params })

export const getReceiving = (id: string): Promise<ReceivingDto> =>
  api.get(`/Receivings/${id}`)

export const createReceiving = (dto: CreateReceivingDto): Promise<ReceivingDto> =>
  api.post('/Receivings', dto)

export const updateReceiving = (id: string, dto: CreateReceivingDto): Promise<ReceivingDto> =>
  api.put(`/Receivings/${id}`, dto)

export const deleteReceiving = (id: string): Promise<void> =>
  api.delete(`/Receivings/${id}`)

export const confirmReceiving = (id: string): Promise<ReceivingDto> =>
  api.post(`/Receivings/${id}/confirm`)