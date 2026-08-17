import api from '../lib/axios'
import type { SaleOrderDto } from '../types/saleOrder'

export const getSaleOrders = (): Promise<SaleOrderDto[]> => api.get('/SaleOrders')
