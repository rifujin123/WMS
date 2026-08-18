import api from '../lib/axios'
import type { ReceivingDto, CreateReceivingDto } from '../types/receiving'

export const getReceivings = (): Promise<ReceivingDto[]> =>
  api.get('/Receivings')

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