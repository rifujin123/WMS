import dayjs from 'dayjs'

/**
 * Format ngày giờ theo múi giờ địa phương của trình duyệt.
 * Nếu chuỗi ISO không có timezone indicator (không có Z hay +/-), tự động hiểu là UTC để chuyển sang múi giờ VN (GMT+7).
 */
export function formatDateTime(
  date: string | Date | undefined | null,
  format = 'DD/MM/YYYY HH:mm',
): string {
  if (!date) return '—'
  if (typeof date === 'string') {
    const isIsoWithoutTz = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?$/.test(date)
    const normalized = isIsoWithoutTz ? `${date}Z` : date
    return dayjs(normalized).format(format)
  }
  return dayjs(date).format(format)
}
