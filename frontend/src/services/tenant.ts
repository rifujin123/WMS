import api from '../lib/axios'
import type {
  TenantConfig,
  UpdateTenantConfigInput,
  TestAiKeyInput,
  TestAiKeyResult,
  TestEmailInput,
  TestEmailResult,
  PublicTenantBranding,
} from '../types/tenant'

export async function getTenantConfig(): Promise<TenantConfig> {
  return await api.get('/tenants/config')
}

export async function updateTenantConfig(
  input: UpdateTenantConfigInput,
): Promise<{ message: string }> {
  return await api.put('/tenants/config', input)
}

export async function testAiKey(input: TestAiKeyInput): Promise<TestAiKeyResult> {
  return await api.post('/tenants/config/test-ai-key', input)
}

export async function testEmailConnection(input: TestEmailInput): Promise<TestEmailResult> {
  return await api.post('/tenants/test-email', input)
}

export async function getPublicBranding(
  tenantCode: string,
): Promise<PublicTenantBranding | null> {
  return await api.get(`/tenants/public-branding?tenant=${encodeURIComponent(tenantCode)}`)
}

export async function uploadTenantAsset(
  file: File,
  type: 'logo' | 'background',
): Promise<{ url: string }> {
  const form = new FormData()
  form.append('file', file)
  return await api.post(`/tenants/upload-asset?type=${type}`, form)
}
