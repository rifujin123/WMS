import dayjs from 'dayjs'

export type PeriodKey = 'today' | '7d' | '30d' | 'month'

export interface PeriodRange {
  fromUtc?: string
  toUtc?: string
}

export const PERIOD_OPTIONS: { value: PeriodKey; label: string }[] = [
  { value: 'today', label: 'Hôm nay' },
  { value: '7d', label: '7 ngày' },
  { value: '30d', label: '30 ngày' },
  { value: 'month', label: 'Tháng này' },
]

/** Tính khoảng [fromUtc, toUtc] cho một kỳ (mặc định 30 ngày). */
export function getPeriodRange(key: PeriodKey): PeriodRange {
  const now = dayjs()
  switch (key) {
    case 'today':
      return { fromUtc: now.startOf('day').toISOString(), toUtc: now.toISOString() }
    case '7d':
      return { fromUtc: now.subtract(7, 'day').toISOString(), toUtc: now.toISOString() }
    case 'month':
      return { fromUtc: now.startOf('month').toISOString(), toUtc: now.toISOString() }
    case '30d':
    default:
      return { fromUtc: now.subtract(30, 'day').toISOString(), toUtc: now.toISOString() }
  }
}
