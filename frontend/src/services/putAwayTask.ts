import api from '../lib/axios'
import type { PutAwayTaskDto, UpdatePutAwayTaskDto, AssignPutAwayDto } from '../types/putAwayTask'

export const getPutAwayTasks = (params?: { assignToId?: string }): Promise<PutAwayTaskDto[]> =>
  api.get('/PutAwayTasks', { params })

export const getPutAwayTask = (id: string): Promise<PutAwayTaskDto> =>
  api.get(`/PutAwayTasks/${id}`)

export const updatePutAwayTask = (id: string, dto: UpdatePutAwayTaskDto): Promise<PutAwayTaskDto> =>
  api.put(`/PutAwayTasks/${id}`, dto)

export const assignPutAwayTask = (id: string, dto: AssignPutAwayDto): Promise<PutAwayTaskDto> =>
  api.post(`/PutAwayTasks/${id}/assign`, dto)

export const startPutAwayTask = (id: string): Promise<PutAwayTaskDto> =>
  api.post(`/PutAwayTasks/${id}/start`)

export const completePutAwayTask = (id: string): Promise<PutAwayTaskDto> =>
  api.post(`/PutAwayTasks/${id}/complete`)

export const deletePutAwayTask = (id: string): Promise<void> =>
  api.delete(`/PutAwayTasks/${id}`)