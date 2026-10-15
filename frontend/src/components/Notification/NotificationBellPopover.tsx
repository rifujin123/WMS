import { useState } from 'react'
import {
  BellOutlined,
  CheckOutlined,
  CarryOutOutlined,
  ExclamationCircleOutlined,
  InfoCircleOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons'
import { Badge, Button, List, Popover, Spin, Typography } from 'antd'
import { useNavigate } from 'react-router-dom'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/vi'
import {
  useMarkAllNotificationsAsRead,
  useMarkNotificationAsRead,
  useNotifications,
  useSeedSampleNotifications,
  useUnreadNotificationCount,
} from '../../hooks/useNotifications'
import type { NotificationDto, NotificationType } from '../../types/notification'
import { toast } from '../../lib/toast'

dayjs.extend(relativeTime)
dayjs.locale('vi')

export function NotificationBellPopover() {
  const [open, setOpen] = useState(false)
  const navigate = useNavigate()

  const { data: unreadData } = useUnreadNotificationCount({
    refetchInterval: 30000,
  })
  const { data: notifData, isPending: notifPending } = useNotifications({
    page: 1,
    pageSize: 10,
  })

  const markReadMutation = useMarkNotificationAsRead()
  const markAllReadMutation = useMarkAllNotificationsAsRead()
  const seedSampleMutation = useSeedSampleNotifications()

  const unreadCount = unreadData?.count ?? 0
  const notifications = notifData?.items ?? []

  const handleNotificationClick = (item: NotificationDto) => {
    if (!item.isRead) {
      markReadMutation.mutate(item.id)
    }
    setOpen(false)
    if (item.actionUrl) {
      navigate(item.actionUrl)
    }
  }

  const handleMarkAllRead = () => {
    markAllReadMutation.mutate(undefined, {
      onSuccess: (data) => {
        toast.success(`Đã đánh dấu ${data.updatedCount} thông báo là đã đọc.`)
      },
    })
  }

  const handleSeedSample = () => {
    seedSampleMutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Đã nạp 4 thông báo mẫu thành công!')
      },
    })
  }

  const renderIcon = (type: NotificationType) => {
    switch (type) {
      case 'TaskAssigned':
        return <CarryOutOutlined style={{ color: '#1677ff', fontSize: 16 }} />
      case 'ApprovalRequired':
        return <ExclamationCircleOutlined style={{ color: '#faad14', fontSize: 16 }} />
      case 'Reminder':
        return <ClockCircleOutlined style={{ color: '#cf1322', fontSize: 16 }} />
      default:
        return <InfoCircleOutlined style={{ color: '#52c41a', fontSize: 16 }} />
    }
  }

  const content = (
    <div style={{ width: 340, maxHeight: 420, display: 'flex', flexDirection: 'column' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          paddingBottom: 8,
          borderBottom: '1px solid #f0f0f0',
        }}
      >
        <Typography.Text strong style={{ fontSize: 14 }}>
          Thông báo ({unreadCount} chưa đọc)
        </Typography.Text>
        <div style={{ display: 'flex', gap: 4 }}>
          {unreadCount > 0 && (
            <Button
              type="link"
              size="small"
              icon={<CheckOutlined />}
              onClick={handleMarkAllRead}
              loading={markAllReadMutation.isPending}
              style={{ padding: 0, fontSize: 12 }}
            >
              Đọc tất cả
            </Button>
          )}
          {notifications.length === 0 && (
            <Button
              type="link"
              size="small"
              onClick={handleSeedSample}
              loading={seedSampleMutation.isPending}
              style={{ padding: 0, fontSize: 12 }}
            >
              Tạo mẫu
            </Button>
          )}
        </div>
      </div>

      <div style={{ overflowY: 'auto', flex: 1, maxHeight: 340, paddingTop: 4 }}>
        {notifPending ? (
          <div style={{ textAlign: 'center', padding: '24px 0' }}>
            <Spin size="small" />
          </div>
        ) : notifications.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '24px 0', color: '#8c8c8c', fontSize: 13 }}>
            Chưa có thông báo nào
          </div>
        ) : (
          <List<NotificationDto>
            size="small"
            dataSource={notifications}
            renderItem={(item: NotificationDto) => (
              <List.Item
                onClick={() => handleNotificationClick(item)}
                style={{
                  padding: '10px 8px',
                  borderRadius: 6,
                  cursor: 'pointer',
                  background: item.isRead ? 'transparent' : '#f0f7ff',
                  borderBottom: '1px solid #f5f5f5',
                  transition: 'background 0.2s',
                  alignItems: 'flex-start',
                }}
              >
                <div style={{ display: 'flex', gap: 10, width: '100%' }}>
                  <div style={{ marginTop: 2 }}>{renderIcon(item.type)}</div>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography.Text
                        strong={!item.isRead}
                        style={{ fontSize: 13, color: item.isRead ? '#595959' : '#262626' }}
                      >
                        {item.title}
                      </Typography.Text>
                      {!item.isRead && (
                        <span
                          style={{
                            width: 6,
                            height: 6,
                            borderRadius: '50%',
                            backgroundColor: '#1677ff',
                            display: 'inline-block',
                          }}
                        />
                      )}
                    </div>
                    <Typography.Text
                      type="secondary"
                      ellipsis
                      style={{ fontSize: 12, display: 'block', marginTop: 2 }}
                    >
                      {item.message}
                    </Typography.Text>
                    <div style={{ fontSize: 11, color: '#8c8c8c', marginTop: 4 }}>
                      {dayjs(item.createdAtUtc).fromNow()}
                    </div>
                  </div>
                </div>
              </List.Item>
            )}
          />
        )}
      </div>
    </div>
  )

  return (
    <Popover
      content={content}
      trigger="click"
      placement="bottomRight"
      open={open}
      onOpenChange={setOpen}
    >
      <Badge count={unreadCount} overflowCount={99} size="small" offset={[-2, 4]}>
        <Button
          type="text"
          shape="circle"
          icon={<BellOutlined style={{ fontSize: 18, color: '#595959' }} />}
          aria-label="Thông báo hệ thống"
        />
      </Badge>
    </Popover>
  )
}
