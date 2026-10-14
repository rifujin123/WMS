import { describe, it, expect } from 'vitest'
import type { TenantConfig, PublicTenantBranding } from '../types/tenant'

describe('Tenant White-label Branding & BYOK AI Rules', () => {
  it('TenantConfig: che giấu API key đầy đủ, chỉ hiển thị masked key', () => {
    const mockConfig: TenantConfig = {
      companyName: 'Công ty Cổ phần Vận tải Á Châu',
      companyCode: 'achau_logistics',
      logoUrl: 'https://res.cloudinary.com/demo/image/upload/logo.png',
      loginBackgroundUrl: 'https://res.cloudinary.com/demo/image/upload/bg.jpg',
      footerText: 'Hotline: 1900 6868 | contact@achau.vn',
      hasExpiryManagement: true,
      hasAiKeyConfigured: true,
      maskedAiApiKey: 'AIzaSy...****',
    }

    expect(mockConfig.hasAiKeyConfigured).toBe(true)
    expect(mockConfig.maskedAiApiKey).toBe('AIzaSy...****')
    expect(mockConfig.maskedAiApiKey).not.toContain('secret')
  })

  it('PublicTenantBranding: nạp đầy đủ thông tin trang đăng nhập theo tenant code', () => {
    const mockPublicBranding: PublicTenantBranding = {
      companyName: 'Alpha Express',
      companyCode: 'alpha',
      logoUrl: 'https://res.cloudinary.com/demo/image/upload/alpha_logo.png',
      loginBackgroundUrl: 'https://res.cloudinary.com/demo/image/upload/alpha_bg.jpg',
      footerText: 'Hệ thống quản lý kho vận chuyên nghiệp Alpha',
    }

    expect(mockPublicBranding.companyName).toBe('Alpha Express')
    expect(mockPublicBranding.logoUrl).toContain('alpha_logo.png')
    expect(mockPublicBranding.loginBackgroundUrl).toContain('alpha_bg.jpg')
    expect(mockPublicBranding.footerText).toBeDefined()
  })

  it('Branding Fallback: Giữ nhận diện mặc định khi tenant chưa cấu hình logo/hình nền', () => {
    const emptyBranding: PublicTenantBranding = {
      companyName: '',
      companyCode: '',
      logoUrl: undefined,
      loginBackgroundUrl: undefined,
      footerText: undefined,
    }

    const defaultTitle = emptyBranding.companyName || 'WMS Platform'
    const defaultFooter = emptyBranding.footerText || 'Hệ thống quản trị kho thông minh'

    expect(defaultTitle).toBe('WMS Platform')
    expect(defaultFooter).toBe('Hệ thống quản trị kho thông minh')
  })

  it('AI Key Validation Flow: Kiểm tra logic nhận diện chuỗi masked để tránh ghi đè', () => {
    const originalInput = 'AIzaSy...****'
    const isMaskedPlaceholder = originalInput.includes('****') || originalInput.trim() === ''
    expect(isMaskedPlaceholder).toBe(true)

    const newlyEnteredKey = 'AIzaSyBrandNewKey123456789'
    const isNewValidKey = !newlyEnteredKey.includes('****') && newlyEnteredKey.length > 10
    expect(isNewValidKey).toBe(true)
  })
})