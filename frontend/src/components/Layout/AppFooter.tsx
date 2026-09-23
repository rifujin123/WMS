import {
  EnvironmentOutlined,
  FacebookFilled,
  InstagramFilled,
  MailOutlined,
  PhoneOutlined,
  TikTokOutlined,
} from '@ant-design/icons'
import { Layout } from 'antd'
import Logo from '../Logo'

const { Footer } = Layout

export function AppFooter() {
  return (
    <Footer
      style={{
        marginTop: 24,
        padding: '32px 24px 16px',
        background: '#0B1420',
        color: 'rgba(255,255,255,0.72)',
      }}
    >
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
          gap: 32,
          maxWidth: 1200,
          margin: '0 auto',
        }}
      >
        <div>
          <Logo size={28} withWordmark />
          <p style={{ margin: '12px 0 0', fontSize: 13, lineHeight: 1.7 }}>
            Hệ thống quản lý kho hàng doanh nghiệp, theo dõi tồn kho,
            nhập xuất và điều chuyển theo thời gian thực.
          </p>
        </div>

        <div>
          <div
            style={{
              color: '#fff',
              fontWeight: 600,
              fontSize: 14,
              marginBottom: 12,
            }}
          >
            Liên hệ
          </div>
          <ul
            style={{
              listStyle: 'none',
              margin: 0,
              padding: 0,
              fontSize: 13,
              lineHeight: 2,
            }}
          >
            <li>
              <EnvironmentOutlined style={{ marginRight: 8 }} />
              123 Nguyễn Văn Linh, Quận 7, TP. Hồ Chí Minh
            </li>
            <li>
              <PhoneOutlined style={{ marginRight: 8 }} />
              028 1234 5678
            </li>
            <li>
              <MailOutlined style={{ marginRight: 8 }} />
              hotro@wms.vn
            </li>
          </ul>
        </div>

        <div>
          <div
            style={{
              color: '#fff',
              fontWeight: 600,
              fontSize: 14,
              marginBottom: 12,
            }}
          >
            Kết nối
          </div>
          <div style={{ display: 'flex', gap: 16 }}>
            <a
              href="https://facebook.com"
              target="_blank"
              rel="noreferrer"
              aria-label="Facebook"
              style={{ color: '#fff', fontSize: 24 }}
            >
              <FacebookFilled />
            </a>
            <a
              href="https://instagram.com"
              target="_blank"
              rel="noreferrer"
              aria-label="Instagram"
              style={{ color: '#fff', fontSize: 24 }}
            >
              <InstagramFilled />
            </a>
            <a
              href="https://tiktok.com"
              target="_blank"
              rel="noreferrer"
              aria-label="TikTok"
              style={{ color: '#fff', fontSize: 24 }}
            >
              <TikTokOutlined />
            </a>
          </div>
        </div>
      </div>
    </Footer>
  )
}
