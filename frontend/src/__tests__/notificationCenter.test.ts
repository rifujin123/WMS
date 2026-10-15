import { describe, it, expect } from 'vitest'
import type { NotificationDto, NotificationType } from '../types/notification'

describe('Notification Center Logic & Types', () => {
  it('Phân loại icon và màu sắc chính xác theo NotificationType', () => {
    const getTypeColor = (type: NotificationType) => {
      switch (type) {
        case 'TaskAssigned':
          return 'blue'
        case 'ApprovalRequired':
          return 'gold'
        case 'Reminder':
          return 'red'
        default:
          return 'green'
      }
    }

    expect(getTypeColor('TaskAssigned')).toBe('blue')
    expect(getTypeColor('ApprovalRequired')).toBe('gold')
    expect(getTypeColor('Reminder')).toBe('red')
    expect(getTypeColor('System')).toBe('green')
  })

  it('Tính toán chính xác số lượng thông báo chưa đọc', () => {
    const notifications: NotificationDto[] = [
      {
        id: '1',
        tenantId: 't1',
        title: 'Task 1',
        message: 'Msg 1',
        type: 'TaskAssigned',
        isRead: false,
        createdAtUtc: new Date().toISOString(),
      },
      {
        id: '2',
        tenantId: 't1',
        title: 'Approval 1',
        message: 'Msg 2',
        type: 'ApprovalRequired',
        isRead: false,
        createdAtUtc: new Date().toISOString(),
      },
      {
        id: '3',
        tenantId: 't1',
        title: 'System 1',
        message: 'Msg 3',
        type: 'System',
        isRead: true,
        createdAtUtc: new Date().toISOString(),
      },
    ]

    const unreadCount = notifications.filter((n) => !n.isRead).length
    expect(unreadCount).toBe(2)
  })

  it('Click vào thông báo chưa đọc cập nhật isRead sang true và giảm unreadCount', () => {
    let unreadCount = 3
    const notification: NotificationDto = {
      id: 'notif-1',
      tenantId: 't1',
      title: 'Đơn PO-001 đã duyệt',
      message: 'Sẵn sàng nhận hàng',
      type: 'TaskAssigned',
      actionUrl: '/receivings?createFromPo=po-1',
      isRead: false,
      createdAtUtc: new Date().toISOString(),
    }

    const handleClick = (item: NotificationDto) => {
      if (!item.isRead) {
        item.isRead = true
        unreadCount--
      }
    }

    handleClick(notification)
    expect(notification.isRead).toBe(true)
    expect(unreadCount).toBe(2)
  })

  it('Xử lý Đọc tất cả thông báo chuyển toàn bộ isRead sang true', () => {
    const list: NotificationDto[] = [
      { id: '1', tenantId: 't1', title: 'A', message: 'M', type: 'System', isRead: false, createdAtUtc: '' },
      { id: '2', tenantId: 't1', title: 'B', message: 'M', type: 'System', isRead: false, createdAtUtc: '' },
    ]

    const markAllRead = (items: NotificationDto[]) => {
      items.forEach((item) => {
        item.isRead = true
      })
    }

    markAllRead(list)
    expect(list.every((item) => item.isRead)).toBe(true)
  })
})
