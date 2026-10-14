import type { WorkingCapitalRadarDto } from '../types/workingCapital'
import { toast } from './toast'

function formatVnd(amount: number): string {
  return new Intl.NumberFormat('vi-VN').format(Math.round(amount)) + ' đ'
}

function formatDate(dateStr?: string): string {
  if (!dateStr) return '—'
  const d = new Date(dateStr)
  if (isNaN(d.getTime())) return dateStr
  return d.toLocaleDateString('vi-VN')
}

const escCsv = (val?: string | number) => `"${String(val ?? '').replace(/"/g, '""')}"`
const escHtml = (str?: string) =>
  String(str ?? '').replace(/[&<>"']/g, (m) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[m]!)

export function exportWorkingCapitalToExcel(data: WorkingCapitalRadarDto) {
  const todayStr = new Date().toISOString().slice(0, 10).replace(/-/g, '')
  const fileName = `BaoCao_HangDong_CanDate_${todayStr}.csv`

  const lines: string[] = []

  // Header info
  lines.push('BÁO CÁO PHÂN TÍCH VỐN LƯU ĐỘNG & CẢNH BÁO HÀNG ĐỌNG - CẬN DATE')
  lines.push(`Ngày kết xuất:;${new Date().toLocaleDateString('vi-VN')} ${new Date().toLocaleTimeString('vi-VN')}`)
  lines.push(`Phạm vi kho:;${data.warehouseName || 'Toàn bộ hệ thống'}`)
  lines.push(`Tổng vốn lưu động tồn kho:;${data.totalWorkingCapital}`)
  lines.push(`Tổng vốn đóng băng (>${data.frozenCapitalDays} ngày):;${data.totalFrozenCapital}`)
  lines.push(`Hàng đọng vốn Dead Stock (>${data.deadStockDays} ngày):;${data.deadStockCapital}`)
  lines.push(`Số lô cận hạn sử dụng (<=${data.nearExpiryDays} ngày):;${data.nearExpiryStockCount}`)
  lines.push('')

  // Section 1: Phân bổ tuổi hàng (Aging Buckets)
  lines.push('1. PHÂN BỔ TUỔI ĐỜI TỒN KHO (AGING BUCKETS)')
  lines.push('Nhóm tuổi;Số lượng tồn;Giá trị tồn kho (VNĐ);Tỷ lệ (%)')
  for (const b of data.agingBuckets) {
    lines.push(`${escCsv(b.range)};${b.quantity};${b.totalValue};${b.percentage}%`)
  }
  lines.push('')

  // Section 2: Top Dead Stock
  lines.push('2. DANH SÁCH TOP SẢN PHẨM ĐỌNG VỐN (DEAD STOCK)')
  lines.push('Mã SKU;Tên sản phẩm;Danh mục;Số lượng tồn;Giá vốn / Hòa vốn (VNĐ);Vốn bị chôn (VNĐ);Số ngày tồn;Vị trí kệ;Đề xuất hành động')
  if (data.topDeadStocks.length === 0) {
    lines.push('Không có sản phẩm nào thuộc diện Dead Stock;;;;;;;;')
  } else {
    for (const d of data.topDeadStocks) {
      const locs = d.locations.join(', ')
      lines.push(`${escCsv(d.productSku)};${escCsv(d.productName)};${escCsv(d.categoryName || '')};${d.quantity};${d.purchasePrice};${d.tiedUpCapital};${d.daysInStock};${escCsv(locs)};${escCsv(d.actionSuggestion)}`)
    }
  }
  lines.push('')

  // Section 3: FEFO Alerts
  lines.push('3. DANH SÁCH CẢNH BÁO LÔ HÀNG CẬN HẠN SỬ DỤNG (FEFO ALERTS)')
  lines.push('Mã SKU;Tên sản phẩm;Số lô;Vị trí kệ;Kho;Số lượng tồn;Giá vốn (VNĐ);Tổng giá trị (VNĐ);Hạn sử dụng;Số ngày còn lại;Mức cảnh báo;Trạng thái')
  if (data.fefoAlerts.length === 0) {
    lines.push('Không có lô hàng nào cận hạn sử dụng;;;;;;;;;;;')
  } else {
    for (const f of data.fefoAlerts) {
      lines.push(`${escCsv(f.productSku)};${escCsv(f.productName)};${escCsv(f.lotNumber || '')};${escCsv(f.locationCode)};${escCsv(f.warehouseName || '')};${f.onhandQty};${f.unitCost};${f.totalValue};${escCsv(formatDate(f.expiryDate))};${f.daysRemaining};${escCsv(f.alertLevel)};${escCsv(f.statusText)}`)
    }
  }

  const csvContent = '﻿' + lines.join('\r\n')
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.setAttribute('href', url)
  link.setAttribute('download', fileName)
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

export function printWorkingCapitalPdfReport(data: WorkingCapitalRadarDto, companyName?: string) {
  const printWindow = window.open('', '_blank')
  if (!printWindow) {
    toast.error('Vui lòng cho phép mở cửa sổ popup để in báo cáo.')
    return
  }

  const todayStr = new Date().toLocaleDateString('vi-VN')
  const company = companyName || 'HỆ THỐNG QUẢN LÝ KHO WMS'

  const agingRows = data.agingBuckets.map(b => `
    <tr>
      <td style="padding: 6px 10px; border: 1px solid #ddd;">${escHtml(b.range)}</td>
      <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: right;">${b.quantity}</td>
      <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: right; font-weight: 500;">${formatVnd(b.totalValue)}</td>
      <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: right;">${b.percentage}%</td>
    </tr>
  `).join('')

  const deadStockRows = data.topDeadStocks.length === 0
    ? '<tr><td colspan="7" style="padding: 10px; text-align: center; border: 1px solid #ddd; color: #888;">Không có sản phẩm nào thuộc diện Dead Stock</td></tr>'
    : data.topDeadStocks.map(d => `
      <tr>
        <td style="padding: 6px 10px; border: 1px solid #ddd; font-family: monospace; font-weight: bold;">${escHtml(d.productSku)}</td>
        <td style="padding: 6px 10px; border: 1px solid #ddd;">${escHtml(d.productName)}</td>
        <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: right;">${d.quantity}</td>
        <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: right;">${formatVnd(d.purchasePrice)}</td>
        <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: right; font-weight: bold; color: #cf1322;">${formatVnd(d.tiedUpCapital)}</td>
        <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: center;">${d.daysInStock} ngày</td>
        <td style="padding: 6px 10px; border: 1px solid #ddd;">${escHtml(d.actionSuggestion)}</td>
      </tr>
    `).join('')

  const fefoRows = data.fefoAlerts.length === 0
    ? '<tr><td colspan="7" style="padding: 10px; text-align: center; border: 1px solid #ddd; color: #888;">Không có lô hàng cận hạn sử dụng</td></tr>'
    : data.fefoAlerts.map(f => {
      const color = f.alertLevel === 'Red' ? '#cf1322' : f.alertLevel === 'Orange' ? '#d46b08' : '#d48806'
      return `
        <tr>
          <td style="padding: 6px 10px; border: 1px solid #ddd; font-family: monospace; font-weight: bold;">${escHtml(f.productSku)}</td>
          <td style="padding: 6px 10px; border: 1px solid #ddd;">${escHtml(f.productName)}</td>
          <td style="padding: 6px 10px; border: 1px solid #ddd; font-family: monospace;">${escHtml(f.lotNumber || '—')}</td>
          <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: right;">${f.onhandQty}</td>
          <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: center;">${formatDate(f.expiryDate)}</td>
          <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: center; font-weight: bold; color: ${color};">${escHtml(f.statusText)}</td>
          <td style="padding: 6px 10px; border: 1px solid #ddd; text-align: right; font-weight: 500;">${formatVnd(f.totalValue)}</td>
        </tr>
      `
    }).join('')

  const html = `
    <!DOCTYPE html>
    <html lang="vi">
    <head>
      <meta charset="utf-8" />
      <title>Báo cáo Đề xuất Xử lý Hàng Đọng & Cận Date</title>
      <style>
        @page {
          size: A4;
          margin: 15mm;
        }
        body {
          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
          color: #262626;
          font-size: 12px;
          line-height: 1.5;
          margin: 0;
          padding: 10px;
        }
        h1 {
          font-size: 18px;
          text-align: center;
          margin: 10px 0 4px;
          text-transform: uppercase;
          color: #141414;
        }
        .subtitle {
          text-align: center;
          color: #595959;
          font-size: 12px;
          margin-bottom: 20px;
        }
        .header-table {
          width: 100%;
          border-bottom: 2px solid #333;
          padding-bottom: 8px;
          margin-bottom: 16px;
        }
        .kpi-grid {
          display: flex;
          gap: 12px;
          margin-bottom: 20px;
        }
        .kpi-box {
          flex: 1;
          border: 1px solid #d9d9d9;
          border-radius: 4px;
          padding: 8px 12px;
          background: #fafafa;
        }
        .kpi-title {
          font-size: 11px;
          color: #8c8c8c;
          text-transform: uppercase;
        }
        .kpi-value {
          font-size: 16px;
          font-weight: bold;
          margin-top: 4px;
        }
        table {
          width: 100%;
          border-collapse: collapse;
          margin-bottom: 16px;
        }
        th {
          background: #f0f2f5;
          border: 1px solid #ddd;
          padding: 8px 10px;
          font-size: 11px;
          text-align: left;
        }
        .section-title {
          font-size: 13px;
          font-weight: bold;
          margin: 16px 0 8px;
          color: #1f1f1f;
        }
        .signatures {
          display: flex;
          justifyContent: space-between;
          margin-top: 40px;
          page-break-inside: avoid;
        }
        .sig-block {
          width: 30%;
          text-align: center;
        }
        .sig-role {
          font-weight: bold;
          font-size: 12px;
          text-transform: uppercase;
        }
        .sig-note {
          font-style: italic;
          color: #8c8c8c;
          font-size: 11px;
          margin-top: 2px;
        }
        .sig-space {
          height: 70px;
        }
        @media print {
          body {
            padding: 0;
          }
          .no-print {
            display: none;
          }
        }
      </style>
    </head>
    <body>
      <table class="header-table">
        <tr>
          <td>
            <div style="font-size: 14px; font-weight: bold;">${escHtml(company)}</div>
            <div style="color: #666;">Kho: ${escHtml(data.warehouseName || 'Toàn hệ thống')}</div>
          </td>
          <td style="text-align: right; color: #666;">
            <div>Ngày lập: ${todayStr}</div>
            <div>Mã biểu mẫu: BM-WMS-RADAR-01</div>
          </td>
        </tr>
      </table>

      <h1>BÁO CÁO ĐỀ XUẤT XỬ LÝ HÀNG TỒN ĐỌNG & CẬN DATE</h1>
      <div class="subtitle">Phân tích dòng tiền đóng băng, tuổi đời tồn kho và đề xuất xả hàng hòa vốn</div>

      <div class="kpi-grid">
        <div class="kpi-box">
          <div class="kpi-title">Tổng vốn tồn kho</div>
          <div class="kpi-value" style="color: #1677ff;">${formatVnd(data.totalWorkingCapital)}</div>
        </div>
        <div class="kpi-box">
          <div class="kpi-title">Vốn đóng băng (&gt;${data.frozenCapitalDays}d)</div>
          <div class="kpi-value" style="color: #faad14;">${formatVnd(data.totalFrozenCapital)}</div>
        </div>
        <div class="kpi-box">
          <div class="kpi-title">Vốn đọng Dead Stock (&gt;${data.deadStockDays}d)</div>
          <div class="kpi-value" style="color: #cf1322;">${formatVnd(data.deadStockCapital)}</div>
        </div>
        <div class="kpi-box">
          <div class="kpi-title">Lô cận date (&le;${data.nearExpiryDays}d)</div>
          <div class="kpi-value" style="color: #d46b08;">${data.nearExpiryStockCount} lô</div>
        </div>
      </div>

      <div class="section-title">1. Phân bổ Tuổi đời Tồn kho (Aging Buckets)</div>
      <table>
        <thead>
          <tr>
            <th>Nhóm tuổi hàng</th>
            <th style="text-align: right;">Số lượng tồn</th>
            <th style="text-align: right;">Giá trị tồn kho</th>
            <th style="text-align: right;">Tỷ lệ (%)</th>
          </tr>
        </thead>
        <tbody>
          ${agingRows}
        </tbody>
      </table>

      <div class="section-title">2. Danh sách Top 5 Hàng Đọng Vốn (Dead Stock &gt; ${data.deadStockDays} ngày)</div>
      <table>
        <thead>
          <tr>
            <th>Mã SKU</th>
            <th>Tên sản phẩm</th>
            <th style="text-align: right;">SL tồn</th>
            <th style="text-align: right;">Giá vốn / Hòa vốn</th>
            <th style="text-align: right;">Vốn chôn</th>
            <th style="text-align: center;">Tuổi kho</th>
            <th>Đề xuất xử lý</th>
          </tr>
        </thead>
        <tbody>
          ${deadStockRows}
        </tbody>
      </table>

      <div class="section-title">3. Danh sách Lô hàng Cận Date (FEFO Alert &le; ${data.nearExpiryDays} ngày)</div>
      <table>
        <thead>
          <tr>
            <th>Mã SKU</th>
            <th>Tên sản phẩm</th>
            <th>Số lô</th>
            <th style="text-align: right;">SL tồn</th>
            <th style="text-align: center;">Hạn sử dụng</th>
            <th style="text-align: center;">Tình trạng</th>
            <th style="text-align: right;">Giá trị</th>
          </tr>
        </thead>
        <tbody>
          ${fefoRows}
        </tbody>
      </table>

      <div class="signatures">
        <div class="sig-block">
          <div class="sig-role">Người lập báo cáo</div>
          <div class="sig-note">(Ký, ghi rõ họ tên)</div>
          <div class="sig-space"></div>
          <div><b>Quản lý kho</b></div>
        </div>
        <div class="sig-block">
          <div class="sig-role">Kế toán trưởng đối soát</div>
          <div class="sig-note">(Ký, ghi rõ họ tên)</div>
          <div class="sig-space"></div>
          <div><b>Phòng Kế toán</b></div>
        </div>
        <div class="sig-block">
          <div class="sig-role">Giám đốc phê duyệt</div>
          <div class="sig-note">(Ký, đóng dấu)</div>
          <div class="sig-space"></div>
          <div><b>Ban Giám đốc</b></div>
        </div>
      </div>

      <div class="no-print" style="margin-top: 30px; text-align: center;">
        <button onclick="window.print()" style="padding: 10px 24px; font-size: 14px; background: #1677ff; color: #fff; border: none; border-radius: 4px; cursor: pointer;">
          In báo cáo ngay
        </button>
      </div>
    </body>
    </html>
  `

  printWindow.document.open()
  printWindow.document.write(html)
  printWindow.document.close()
  printWindow.onafterprint = () => {
    printWindow.close()
  }
  setTimeout(() => {
    printWindow.print()
  }, 350)
}
