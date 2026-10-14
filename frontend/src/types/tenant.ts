export interface TenantConfig {
  companyName: string
  companyCode: string
  address?: string
  contactPhone?: string
  contactEmail?: string
  hasExpiryManagement: boolean
  logoUrl?: string
  loginBackgroundUrl?: string
  footerText?: string
  hasAiKeyConfigured: boolean
  maskedAiApiKey?: string
  aiModel?: string
  nearExpiryDays?: number
  deadStockDays?: number
  frozenCapitalDays?: number
  enableExpiryAlertBanner?: boolean
}

export interface UpdateTenantConfigInput {
  companyName?: string
  address?: string
  contactPhone?: string
  logoUrl?: string
  loginBackgroundUrl?: string
  footerText?: string
  aiApiKey?: string
  aiModel?: string
  nearExpiryDays?: number
  deadStockDays?: number
  frozenCapitalDays?: number
  enableExpiryAlertBanner?: boolean
}

export interface TestAiKeyInput {
  apiKey?: string
  model?: string
}

export interface TestAiKeyResult {
  success: boolean
  message: string
  details?: string
}

export interface PublicTenantBranding {
  companyName: string
  companyCode: string
  logoUrl?: string
  loginBackgroundUrl?: string
  footerText?: string
}
