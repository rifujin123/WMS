import { useEffect, useRef, useState } from 'react'

/**
 * Làm cho việc chuyển trang bảng bớt "giật": mỗi khi `trigger` thay đổi
 * (thường là `page`), ép spinner hiển thị ít nhất `minVisibleMs` — tránh
 * trường hợp fetch quá nhanh khiến trang nhảy liền không có trạng thái chờ.
 * Trả về:
 *  - `loading`: true khi đang trong khoảng chờ tối thiểu hoặc đang fetch thật.
 *  - `transitionKey`: giá trị để đặt `key` lên vùng nội dung bảng, giúp
 *    replay hiệu ứng fade khi dữ liệu mới vào.
 */
export function useTableTransition(trigger: unknown, isFetching: boolean, minVisibleMs = 300) {
  const [forcedLoading, setForcedLoading] = useState(false)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    // Mỗi lần trigger đổi (vd: đổi trang), bật spinner tối thiểu một khoảng.
    // Cố ý setState đồng bộ trong effect: đây là pattern hiển thị "đang tải"
    // tối thiểu, được phép disable rule này.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setForcedLoading(true)
    if (timerRef.current) clearTimeout(timerRef.current)
    timerRef.current = setTimeout(() => setForcedLoading(false), minVisibleMs)
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current)
    }
  }, [trigger, minVisibleMs])

  return {
    loading: forcedLoading || isFetching,
    transitionKey: String(trigger),
  }
}