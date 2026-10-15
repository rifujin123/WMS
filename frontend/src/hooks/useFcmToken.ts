import { useEffect } from 'react'
import { registerFcmToken } from '../services/notification'

export function useFcmToken() {
  useEffect(() => {
    // Chỉ kích hoạt khi trình duyệt hỗ trợ Service Worker và Notification
    if (typeof window === 'undefined' || !('serviceWorker' in navigator) || !('Notification' in window)) {
      return
    }

    const initPush = async () => {
      try {
        // Đăng ký Service Worker
        await navigator.serviceWorker.register('/firebase-messaging-sw.js')

        // Đọc config mock từ biến môi trường Vite
        const vapidKey = import.meta.env.VITE_FIREBASE_VAPID_KEY || 'BMockVapidKey'

        // Nếu đã cấp quyền trước đó, sinh token định danh thiết bị
        if (Notification.permission === 'granted') {
          const cachedToken = localStorage.getItem('wms_fcm_device_token') || `fcm_${vapidKey.substring(0, 8)}_${Math.random().toString(36).substring(2)}_${Date.now()}`
          localStorage.setItem('wms_fcm_device_token', cachedToken)
          await registerFcmToken(cachedToken, 'Web')
        }
      } catch {
        // Safe degrade: Không làm gián đoạn ứng dụng
      }
    }

    initPush()
  }, [])
}
