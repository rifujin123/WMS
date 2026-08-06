import { useState } from 'react'
import { MailOutlined, PhoneOutlined, UserOutlined } from '@ant-design/icons'
import { App, Button, Card, Col, Divider, Form, Input, Row } from 'antd'
import type { UserProfile } from '../../types/user'
import { useAuthContext } from '../../contexts/AuthContext'
import { useUpdateProfile } from '../../hooks/useUserProfile'

interface ProfileInfoFormProps {
  profile: UserProfile
}

interface FormValues {
  fullName: string
  phoneNumber?: string
}

function ProfileInfoForm({ profile }: ProfileInfoFormProps) {
  const { message } = App.useApp()
  const { updateUser } = useAuthContext()
  const updateMutation = useUpdateProfile()
  const [form] = Form.useForm<FormValues>()
  const [dirty, setDirty] = useState(false)

  const onFinish = (values: FormValues) => {
    updateMutation.mutate(values, {
      onSuccess: () => {
        updateUser({ fullName: values.fullName })
        setDirty(false)
        message.success('Cập nhật thông tin thành công.')
      },
      onError: (err) => {
        message.error(
          err instanceof Error ? err.message : 'Cập nhật thông tin thất bại.',
        )
      },
    })
  }

  return (
    <Card title="Thông tin cơ bản" style={{ borderRadius: 12 }}>
      <Form<FormValues>
        form={form}
        layout="vertical"
        size="large"
        initialValues={{ fullName: profile.fullName, phoneNumber: profile.phoneNumber }}
        onFinish={onFinish}
        onValuesChange={() => setDirty(true)}
      >
        <Row gutter={16}>
          <Col xs={24} sm={12}>
            <Form.Item label="Tên đăng nhập" extra="Không thể thay đổi tên đăng nhập.">
              <Input
                value={profile.username}
                disabled
                prefix={<UserOutlined style={{ color: '#8C99A6' }} />}
              />
            </Form.Item>
          </Col>
          <Col xs={24} sm={12}>
            <Form.Item label="Email" extra="Liên hệ quản trị viên để đổi email.">
              <Input
                value={profile.email}
                disabled
                prefix={<MailOutlined style={{ color: '#8C99A6' }} />}
              />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item
          name="fullName"
          label="Họ và tên"
          rules={[
            { required: true, message: 'Vui lòng nhập họ và tên.' },
            { max: 100, message: 'Họ và tên tối đa 100 ký tự.' },
          ]}
        >
          <Input placeholder="vd: Nguyễn Văn An" />
        </Form.Item>

        <Form.Item
          name="phoneNumber"
          label="Số điện thoại"
          rules={[
            { pattern: /^0\d{9}$/, message: 'Số điện thoại không hợp lệ.' },
          ]}
        >
          <Input placeholder="vd: 0901234567" prefix={<PhoneOutlined style={{ color: '#8C99A6' }} />} />
        </Form.Item>

        <Divider style={{ margin: '4px 0 16px' }} />

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
          <Button onClick={() => form.resetFields()}>Đặt lại</Button>
          <Button
            type="primary"
            htmlType="submit"
            loading={updateMutation.isPending}
            disabled={!dirty}
            style={{ minWidth: 120 }}
          >
            Lưu thay đổi
          </Button>
        </div>
      </Form>
    </Card>
  )
}

export default ProfileInfoForm