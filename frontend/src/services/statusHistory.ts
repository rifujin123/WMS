import api from '../lib/axios'
import type { StatusHistoryDto, StatusHistoryQuery } from '../types/statusHistory'

export const getStatusHistories = (params?: StatusHistoryQuery): Promise<StatusHistoryDto[]> =>
  api.get('/status-histories', { params })
