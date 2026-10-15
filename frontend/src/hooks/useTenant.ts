import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getTenantConfig,
  updateTenantConfig,
  testAiKey,
  testEmailConnection,
  getPublicBranding,
} from '../services/tenant'
import type { UpdateTenantConfigInput, TestAiKeyInput, TestEmailInput } from '../types/tenant'

export function useTenantConfig() {
  return useQuery({
    queryKey: ['tenant-config'],
    queryFn: getTenantConfig,
  })
}

export function useUpdateTenantConfig() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: UpdateTenantConfigInput) => updateTenantConfig(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tenant-config'] })
    },
  })
}

export function useTestAiKey() {
  return useMutation({
    mutationFn: (input: TestAiKeyInput) => testAiKey(input),
  })
}

export function useTestEmailConnection() {
  return useMutation({
    mutationFn: (input: TestEmailInput) => testEmailConnection(input),
  })
}

export function usePublicBranding(tenantCode?: string) {
  return useQuery({
    queryKey: ['public-branding', tenantCode],
    queryFn: () => (tenantCode ? getPublicBranding(tenantCode) : Promise.resolve(null)),
    enabled: Boolean(tenantCode && tenantCode.trim().length > 0),
    staleTime: 5 * 60 * 1000,
  })
}
