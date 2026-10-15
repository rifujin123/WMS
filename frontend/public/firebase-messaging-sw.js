// Firebase Cloud Messaging Service Worker (WMS Web Push Handler)

self.addEventListener('install', (event) => {
  self.skipWaiting()
})

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim())
})

// Lắng nghe sự kiện Push từ Firebase FCM
self.addEventListener('push', (event) => {
  let data = {
    title: 'Hệ thống Quản lý kho WMS',
    body: 'Bạn có thông báo mới.',
    actionUrl: '/dashboard',
  }

  if (event.data) {
    try {
      const payload = event.data.json()
      data = {
        title: payload.notification?.title || payload.title || data.title,
        body: payload.notification?.body || payload.body || data.body,
        actionUrl: payload.data?.actionUrl || payload.actionUrl || data.actionUrl,
      }
    } catch {
      data.body = event.data.text()
    }
  }

  const options = {
    body: data.body,
    icon: '/logo.png',
    badge: '/logo.png',
    data: {
      actionUrl: data.actionUrl,
    },
    requireInteraction: false,
    vibrate: [200, 100, 200],
  }

  event.waitUntil(self.registration.showNotification(data.title, options))
})

// Xử lý khi người dùng click vào popup native của hệ điều hành
self.addEventListener('notificationclick', (event) => {
  event.notification.close()

  const actionUrl = event.notification.data?.actionUrl || '/dashboard'

  event.waitUntil(
    self.clients
      .matchAll({ type: 'window', includeUncontrolled: true })
      .then((clientList) => {
        // Nếu có tab WMS đang mở, focus tab đó và chuyển trang
        for (const client of clientList) {
          if ('focus' in client) {
            client.focus()
            if ('navigate' in client) {
              return client.navigate(actionUrl)
            }
            return
          }
        }
        // Nếu chưa mở tab nào, mở tab mới
        if (self.clients.openWindow) {
          return self.clients.openWindow(actionUrl)
        }
      })
  )
})
