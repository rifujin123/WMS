import { Col, Row, Skeleton, Typography } from 'antd'
import AvatarCard from './AvatarCard'
import ChangePasswordForm from './ChangePasswordForm'
import ProfileInfoForm from './ProfileInfoForm'
import { useProfile } from '../../hooks/useUserProfile'

function Profile() {
  const { data: profile, isLoading } = useProfile()

  return (
    <div className="wms-rise">
      <div style={{ marginBottom: 24 }}>
        <Typography.Title level={4} style={{ margin: 0 }}>
          Thông tin cá nhân
        </Typography.Title>
        <Typography.Text type="secondary" style={{ fontSize: 13 }}>
          Quản lý thông tin tài khoản và mật khẩu của bạn.
        </Typography.Text>
      </div>

      {isLoading || !profile ? (
        <Skeleton active paragraph={{ rows: 8 }} />
      ) : (
        <Row gutter={[16, 16]}>
          <Col xs={24} lg={8}>
            <AvatarCard profile={profile} />
          </Col>
          <Col xs={24} lg={16}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              <ProfileInfoForm profile={profile} />
              <ChangePasswordForm />
            </div>
          </Col>
        </Row>
      )}
    </div>
  )
}

export default Profile