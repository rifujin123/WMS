import api from '../lib/axios'
import type { BackorderLinkDto, BackorderLinkQuery, CreateBackorderLinkDto } from '../types/crossDock'

export const getBackorderLinks = (params?: BackorderLinkQuery): Promise<BackorderLinkDto[]> =>
  api.get('/BackorderLinks', { params })

export const getBackorderLinkById = (id: string): Promise<BackorderLinkDto> =>
  api.get(`/BackorderLinks/${id}`)

export const createBackorderLink = (dto: CreateBackorderLinkDto): Promise<BackorderLinkDto> =>
  api.post('/BackorderLinks', dto)

export const deleteBackorderLink = (id: string): Promise<void> =>
  api.delete(`/BackorderLinks/${id}`)
