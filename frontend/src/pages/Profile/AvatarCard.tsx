import { useState } from 'react'
import { CalendarOutlined, CameraOutlined, UploadOutlined } from '@ant-design/icons'
import { App, Avatar, Button, Card, Divider, Spin, Tag, Typography, Upload } from 'antd'
import dayjs from 'dayjs'
import type { UserProfile } from '../../types/user'
import { useAuthContext } from '../../contexts/useAuthContext'
import { useUploadAvatar } from '../../hooks/useUserProfile'
import { DEFAULT_AVATAR_URL } from '../../lib/avatar'

const MAX_SIZE = 2 * 1024 * 1024
const ACCEPT_TYPES = ['image/jpeg', 'image/png', 'image/webp']

const roleLabel: Record<string, string> = {
  Admin: 'Admin',
  WarehouseManager: 'Quản lý kho',
  WarehouseStaff: 'Nhân viên kho',
}

const roleColor: Record<string, string> = {
  Admin: 'blue',
  WarehouseManager: 'cyan',
  WarehouseStaff: 'default',
}

interface AvatarCardProps {
  profile: UserProfile
}

function AvatarCard({ profile }: AvatarCardProps) {
  const { message } = App.useApp()
  const { updateUser } = useAuthContext()
  const uploadMutation = useUploadAvatar()
  const [preview, setPreview] = useState<string | undefined>(undefined)
  const [hover, setHover] = useState(false)

  const role = profile.roles[0] ?? 'WarehouseStaff'

  const handleBeforeUpload = (file: File) => {
    if (file.size > MAX_SIZE) {
      message.error('Ảnh phải nhỏ hơn 2MB.')
      return false
    }
    if (!ACCEPT_TYPES.includes(file.type)) {
      message.error('Chỉ nhận ảnh JPG, PNG hoặc WEBP.')
      return false
    }

    // Hiện preview ngay bằng object URL, upload thật sẽ nối sau
    setPreview(URL.createObjectURL(file))
    uploadMutation.mutate(file, {
      onSuccess: (data) => {
        updateUser({ avatarUrl: data.avatarUrl })
        message.success('Cập nhật ảnh đại diện thành công.')
      },
      onError: () => {
        setPreview(undefined)
        message.error('Tải ảnh lên thất bại. Vui lòng thử lại.')
      },
    })
    return false
  }

  return (
    <Card
      style={{ borderRadius: 12, textAlign: 'center', height: '100%' }}
      styles={{ body: { padding: 24 } }}
    >
      <Upload
        accept="image/png,image/jpeg,image/webp"
        showUploadList={false}
        beforeUpload={handleBeforeUpload}
      >
        <div
          style={{ position: 'relative', display: 'inline-block', cursor: 'pointer' }}
          onMouseEnter={() => setHover(true)}
          onMouseLeave={() => setHover(false)}
        >
          <Avatar
            size={112}
            src={preview || profile.avatarUrl || DEFAULT_AVATAR_URL}
            style={{ fontSize: 40, backgroundColor: '#1677FF' }}
          />
          <div
            style={{
              position: 'absolute',
              inset: 0,
              borderRadius: '50%',
              background: 'rgba(11,20,32,0.55)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              opacity: uploadMutation.isPending || hover ? 1 : 0,
              transition: 'opacity 0.2s cubic-bezier(0.16,1,0.3,1)',
              transform: 'scale(1.2)',
            }}
          >
            {uploadMutation.isPending ? (
              <Spin style={{ color: '#fff' }} />
            ) : (
              <CameraOutlined style={{ color: '#fff', fontSize: 22 }} />
            )}
          </div>
        </div>
      </Upload>

      <div style={{ marginTop: 16 }}>
        <Upload
          accept="image/png,image/jpeg,image/webp"
          showUploadList={false}
          beforeUpload={handleBeforeUpload}
        >
          <Button icon={<UploadOutlined />} loading={uploadMutation.isPending}>
            Đổi ảnh
          </Button>
        </Upload>
      </div>

      <Typography.Text type="secondary" style={{ fontSize: 12, marginTop: 8 }}>
        JPG, PNG hoặc WEBP. Tối đa 2MB.
      </Typography.Text>

      <Divider style={{ margin: '20px 0' }} />

      <div style={{ textAlign: 'left' }}>
        <div style={{ fontSize: 16, fontWeight: 600, color: '#141A21' }}>
          {profile.fullName}
        </div>
        <div style={{ fontSize: 13, color: '#5A6672', marginTop: 2 }}>
          @{profile.username}
        </div>
        <div style={{ marginTop: 12 }}>
          <Tag color={roleColor[role] ?? 'default'}>{roleLabel[role] ?? role}</Tag>
        </div>
        <div
          style={{
            fontSize: 13,
            color: '#5A6672',
            marginTop: 12,
            display: 'flex',
            alignItems: 'center',
            gap: 6,
          }}
        >
          <CalendarOutlined />
          Tham gia từ {dayjs(profile.createdAt).format('DD/MM/YYYY')}
        </div>
      </div>
    </Card>
  )
}

export default AvatarCard