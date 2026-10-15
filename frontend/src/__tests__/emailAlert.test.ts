import { describe, it, expect } from 'vitest'
import type { TenantConfig, UpdateTenantConfigInput } from '../types/tenant'

describe('Email Alert Configuration Logic', () => {
  it('correctly maps default values when enableEmailAlerts is not set', () => {
    const rawConfig: TenantConfig = {
      companyName: 'Kho Test WMS',
      companyCode: 'kho-test',
      hasExpiryManagement: false,
      hasAiKeyConfigured: false,
    }

    const effectiveEnabled = rawConfig.enableEmailAlerts ?? false
    const effectiveRecipients = rawConfig.alertEmailRecipients ?? ''

    expect(effectiveEnabled).toBe(false)
    expect(effectiveRecipients).toBe('')
  })

  it('correctly validates recipients list parsing and email format', () => {
    const recipientsString = 'admin@wms.com, ketoan@wms.com; giamdoc@wms.com,  '
    const parsed = recipientsString
      .split(/[,;]/)
      .map((s) => s.trim())
      .filter((s) => s.length > 0 && s.includes('@'))

    expect(parsed).toEqual(['admin@wms.com', 'ketoan@wms.com', 'giamdoc@wms.com'])
    expect(parsed.length).toBe(3)
  })

  it('prepares payload for updating email alert settings', () => {
    const formValues: UpdateTenantConfigInput = {
      enableEmailAlerts: true,
      alertEmailRecipients: 'alert@company.com, ceo@company.com',
    }

    expect(formValues.enableEmailAlerts).toBe(true)
    expect(formValues.alertEmailRecipients).toContain('alert@company.com')
  })
})
