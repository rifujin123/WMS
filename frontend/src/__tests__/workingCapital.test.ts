import { describe, expect, it, vi } from 'vitest'
import { exportWorkingCapitalToExcel, printWorkingCapitalPdfReport } from '../lib/workingCapitalExport'
import type { WorkingCapitalRadarDto } from '../types/workingCapital'

describe('Working Capital Radar Frontend Logic & Export', () => {
  const mockRadarData: WorkingCapitalRadarDto = {
    totalWorkingCapital: 10000000,
    totalFrozenCapital: 4000000,
    deadStockCapital: 2500000,
    nearExpiryStockCount: 2,
    nearExpiryStockCapital: 1500000,
    warehouseId: 'wh-1',
    warehouseName: 'Kho Hà Nội',
    nearExpiryDays: 30,
    deadStockDays: 90,
    frozenCapitalDays: 60,
    enableExpiryAlertBanner: true,
    hasEmergencyAlert: true,
    emergencyAlertCount: 1,
    agingBuckets: [
      { range: '0-30 ngày', minDays: 0, maxDays: 30, totalValue: 4000000, quantity: 40, percentage: 40.0 },
      { range: '31-60 ngày', minDays: 31, maxDays: 60, totalValue: 2000000, quantity: 20, percentage: 20.0 },
      { range: '61-90 ngày', minDays: 61, maxDays: 90, totalValue: 1500000, quantity: 15, percentage: 15.0 },
      { range: '> 90 ngày', minDays: 91, maxDays: null, totalValue: 2500000, quantity: 25, percentage: 25.0 },
    ],
    topDeadStocks: [
      {
        productId: 'prod-1',
        productSku: 'SKU-DEAD-1',
        productName: 'Hàng Đọng 1',
        categoryName: 'Điện tử',
        quantity: 10,
        purchasePrice: 150000,
        tiedUpCapital: 1500000,
        breakEvenPrice: 150000,
        daysInStock: 120,
        locations: ['LOC-A1'],
        actionSuggestion: 'Khuyến mãi giảm giá sốc',
      },
    ],
    fefoAlerts: [
      {
        stockId: 'stock-1',
        productId: 'prod-2',
        productSku: 'SKU-FEFO-1',
        productName: 'Thuốc Kháng Sinh',
        lotNumber: 'LOT-01',
        locationCode: 'LOC-B2',
        warehouseName: 'Kho Hà Nội',
        onhandQty: 10,
        expiryDate: '2026-10-20T00:00:00Z',
        daysRemaining: 6,
        alertLevel: 'Red',
        unitCost: 50000,
        totalValue: 500000,
        hasExpired: false,
        statusText: 'Còn 6 ngày',
      },
      {
        stockId: 'stock-2',
        productId: 'prod-3',
        productSku: 'SKU-FEFO-2',
        productName: 'Vắc xin A',
        lotNumber: 'LOT-02',
        locationCode: 'LOC-B3',
        warehouseName: 'Kho Hà Nội',
        onhandQty: 20,
        expiryDate: '2026-11-05T00:00:00Z',
        daysRemaining: 22,
        alertLevel: 'Orange',
        unitCost: 50000,
        totalValue: 1000000,
        hasExpired: false,
        statusText: 'Còn 22 ngày',
      },
    ],
  }

  it('exportWorkingCapitalToExcel downloads CSV file with UTF-8 BOM', () => {
    let capturedBlob: Blob | null = null
    let capturedFileName: string | null = null

    // Mock URL.createObjectURL and HTMLAnchorElement click
    const originalCreateObjectURL = URL.createObjectURL
    const originalRevokeObjectURL = URL.revokeObjectURL
    URL.createObjectURL = vi.fn((blob: Blob) => {
      capturedBlob = blob
      return 'blob:mock-url'
    })
    URL.revokeObjectURL = vi.fn()

    const appendChildSpy = vi.spyOn(document.body, 'appendChild').mockImplementation((node) => {
      if (node instanceof HTMLAnchorElement) {
        capturedFileName = node.getAttribute('download')
      }
      return node
    })
    const removeChildSpy = vi.spyOn(document.body, 'removeChild').mockImplementation((node) => node)

    exportWorkingCapitalToExcel(mockRadarData)

    expect(capturedBlob).not.toBeNull()
    expect(capturedFileName).toMatch(/^BaoCao_HangDong_CanDate_\d{8}\.csv$/)

    appendChildSpy.mockRestore()
    removeChildSpy.mockRestore()
    URL.createObjectURL = originalCreateObjectURL
    URL.revokeObjectURL = originalRevokeObjectURL
  })

  it('printWorkingCapitalPdfReport opens popup window with print layout and 3 signatures', () => {
    let writtenHtml = ''
    const mockPrintWindow = {
      document: {
        open: vi.fn(),
        write: vi.fn((html: string) => {
          writtenHtml += html
        }),
        close: vi.fn(),
      },
      print: vi.fn(),
    }

    vi.spyOn(window, 'open').mockReturnValue(mockPrintWindow as unknown as Window)

    printWorkingCapitalPdfReport(mockRadarData, 'Công ty Cổ phần ABC')

    expect(window.open).toHaveBeenCalledWith('', '_blank')
    expect(writtenHtml).toContain('Công ty Cổ phần ABC')
    expect(writtenHtml).toContain('BÁO CÁO ĐỀ XUẤT XỬ LÝ HÀNG TỒN ĐỌNG & CẬN DATE')
    expect(writtenHtml).toContain('Người lập báo cáo')
    expect(writtenHtml).toContain('Kế toán trưởng đối soát')
    expect(writtenHtml).toContain('Giám đốc phê duyệt')
    expect(writtenHtml).toContain('SKU-DEAD-1')
    expect(writtenHtml).toContain('SKU-FEFO-1')
  })

  it('validates breakEvenPrice equals purchasePrice in dead stock', () => {
    for (const deadStock of mockRadarData.topDeadStocks) {
      expect(deadStock.breakEvenPrice).toBe(deadStock.purchasePrice)
    }
  })

  it('exportWorkingCapitalToExcel properly escapes quotes in SKU, Name and Suggestions', async () => {
    let capturedContent = ''
    const originalCreateObjectURL = URL.createObjectURL
    URL.createObjectURL = vi.fn((blob: Blob) => {
      blob.text().then((text) => {
        capturedContent = text
      })
      return 'blob:mock-url'
    })
    const appendChildSpy = vi.spyOn(document.body, 'appendChild').mockImplementation((node) => node)
    const removeChildSpy = vi.spyOn(document.body, 'removeChild').mockImplementation((node) => node)

    const testData = {
      ...mockRadarData,
      topDeadStocks: [
        {
          ...mockRadarData.topDeadStocks[0],
          productName: 'Màn hình 24" Dell UltraSharp',
        },
      ],
    }
    exportWorkingCapitalToExcel(testData)
    await new Promise((resolve) => setTimeout(resolve, 50))
    expect(capturedContent).toContain('"Màn hình 24"" Dell UltraSharp"')

    appendChildSpy.mockRestore()
    removeChildSpy.mockRestore()
    URL.createObjectURL = originalCreateObjectURL
  })
})
