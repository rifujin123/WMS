import api from '../lib/axios'
import type { StatusHistoryDto, StatusHistoryQuery } from '../types/statusHistory'
import type { PagedResponse } from '../types/pagination'

export const getStatusHistories = (params?: StatusHistoryQuery): Promise<PagedResponse<StatusHistoryDto>> =>
  api.get('/status-histories', { params })
