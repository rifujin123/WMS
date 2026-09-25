import { useNavigate } from 'react-router-dom'
import { Typography, Button, Row, Col, Card } from 'antd'
import {
  RocketOutlined,
  ArrowRightOutlined,
  LoginOutlined,
} from '@ant-design/icons'
import Logo from '../../components/Logo'
import { themeConfig } from '../../theme/tokens'

const { Title, Text, Paragraph } = Typography

export default function LandingPage() {
  const navigate = useNavigate()
  const primaryColor = (themeConfig.token?.colorPrimary as string) || '#1677FF'

  const scrollToSection = (id: string) => {
    const el = document.getElementById(id)
    if (el) {
      el.scrollIntoView({ behavior: 'smooth' })
    }
  }

  return (
    <div style={{ minHeight: '100vh', background: '#F5F7FA', color: '#141A21' }}>
      {/* Top Navigation */}
      <header
        style={{
          position: 'sticky',
          top: 0,
          zIndex: 100,
          background: 'rgba(255, 255, 255, 0.92)',
          backdropFilter: 'blur(10px)',
          borderBottom: '1px solid #E2E8F0',
          padding: '0 24px',
        }}
      >
        <div
          style={{
            maxWidth: 1200,
            margin: '0 auto',
            height: 64,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <div style={{ cursor: 'pointer' }} onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}>
            <Logo size={32} withWordmark wordmarkColor="#141A21" />
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
            <span
              onClick={() => scrollToSection('models')}
              style={{ cursor: 'pointer', color: '#5A6672', fontWeight: 500 }}
            >
              Mô hình kho
            </span>
            <span
              onClick={() => scrollToSection('features')}
              style={{ cursor: 'pointer', color: '#5A6672', fontWeight: 500 }}
            >
              Tính năng
            </span>
            <span
              onClick={() => scrollToSection('architecture')}
              style={{ cursor: 'pointer', color: '#5A6672', fontWeight: 500 }}
            >
              Bảo mật
            </span>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Button
              type="text"
              icon={<LoginOutlined />}
              onClick={() => navigate('/login')}
              style={{ fontWeight: 500 }}
            >
              Đăng nhập
            </Button>
            <Button
              type="primary"
              onClick={() => navigate('/register-tenant')}
              style={{ fontWeight: 500, borderRadius: 8 }}
            >
              Đăng ký doanh nghiệp
            </Button>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section
        style={{
          position: 'relative',
          overflow: 'hidden',
          minHeight: '76vh',
          display: 'flex',
          alignItems: 'center',
          borderBottom: '1px solid #E2E8F0',
          background: '#FFFFFF',
        }}
      >
        {/* Full-bleed Background Warehouse Image with blur */}
        <img
          src="https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=1920&q=85"
          alt="Hệ thống kho vận thông minh"
          style={{
            position: 'absolute',
            inset: 0,
            width: '100%',
            height: '100%',
            objectFit: 'cover',
            filter: 'blur(4px)',
            transform: 'scale(1.04)',
          }}
        />

        {/* Dark Scrim Overlay to ensure white text is 100% crisp & readable */}
        <div
          style={{
            position: 'absolute',
            inset: 0,
            background:
              'radial-gradient(ellipse at center, rgba(11,20,32,0.76) 0%, rgba(11,20,32,0.90) 100%)',
          }}
        />

        <div
          style={{
            position: 'relative',
            zIndex: 1,
            maxWidth: 900,
            margin: '0 auto',
            padding: '100px 24px',
            textAlign: 'center',
            width: '100%',
          }}
        >
          {/* Subtle announcement pill */}
          <div style={{ marginBottom: 24 }}>
            <span
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 8,
                padding: '6px 16px',
                borderRadius: 24,
                background: 'rgba(255, 255, 255, 0.10)',
                border: '1px solid rgba(255, 255, 255, 0.22)',
                backdropFilter: 'blur(8px)',
                WebkitBackdropFilter: 'blur(8px)',
                color: '#E0F2FE',
                fontSize: 13,
                fontWeight: 500,
                letterSpacing: 0.3,
              }}
            >
              <span className="wms-pulse-dot" />
              Nền tảng số hóa quản trị kho vận thế hệ mới
            </span>
          </div>

          <Title
            level={1}
            style={{
              fontSize: 'clamp(36px, 5.2vw, 56px)',
              lineHeight: 1.15,
              fontWeight: 700,
              color: '#FFFFFF',
              marginBottom: 20,
              letterSpacing: '-0.02em',
            }}
          >
            Tối ưu vận hành kho toàn diện cho doanh nghiệp
          </Title>

          <Paragraph
            style={{
              fontSize: 'clamp(16px, 1.8vw, 18px)',
              color: 'rgba(255, 255, 255, 0.88)',
              lineHeight: 1.7,
              marginBottom: 40,
              maxWidth: 700,
              marginInline: 'auto',
            }}
          >
            Tự động hóa luồng nhập kho, lưu trữ, kiểm kê và xuất kho theo thời gian thực.
            Lựa chọn linh hoạt giữa <strong>Kho công nghiệp (FIFO)</strong> hoặc{' '}
            <strong>Kho hạn dùng (FEFO)</strong> theo đặc thù từng doanh nghiệp.
          </Paragraph>

          <div
            style={{
              display: 'flex',
              flexWrap: 'wrap',
              gap: 16,
              justifyContent: 'center',
              marginBottom: 52,
            }}
          >
            <Button
              type="primary"
              size="large"
              icon={<RocketOutlined />}
              onClick={() => navigate('/register-tenant')}
              style={{
                height: 50,
                paddingInline: 34,
                fontSize: 16,
                fontWeight: 600,
                borderRadius: 8,
              }}
            >
              Bắt đầu dùng thử miễn phí
            </Button>
            <Button
              size="large"
              onClick={() => scrollToSection('models')}
              style={{
                height: 50,
                paddingInline: 28,
                fontSize: 15,
                borderRadius: 8,
                color: '#FFFFFF',
                background: 'rgba(255, 255, 255, 0.10)',
                borderColor: 'rgba(255, 255, 255, 0.35)',
                backdropFilter: 'blur(8px)',
                WebkitBackdropFilter: 'blur(8px)',
              }}
            >
              Tìm hiểu mô hình kho
            </Button>
          </div>

          {/* 3 Value Props Capsules */}
          <div
            style={{
              display: 'flex',
              gap: 16,
              justifyContent: 'center',
              flexWrap: 'wrap',
            }}
          >
            <div
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 8,
                padding: '8px 18px',
                borderRadius: 20,
                background: 'rgba(11, 20, 32, 0.60)',
                border: '1px solid rgba(255, 255, 255, 0.18)',
                backdropFilter: 'blur(8px)',
                WebkitBackdropFilter: 'blur(8px)',
              }}
            >
              <span className="wms-pulse-dot" />
              <Text style={{ color: '#F1F5F9', fontSize: 13, fontWeight: 500 }}>
                Dữ liệu cô lập độc lập 100%
              </Text>
            </div>

            <div
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 8,
                padding: '8px 18px',
                borderRadius: 20,
                background: 'rgba(11, 20, 32, 0.60)',
                border: '1px solid rgba(255, 255, 255, 0.18)',
                backdropFilter: 'blur(8px)',
                WebkitBackdropFilter: 'blur(8px)',
              }}
            >
              <span className="wms-pulse-dot" />
              <Text style={{ color: '#F1F5F9', fontSize: 13, fontWeight: 500 }}>
                Không phí triển khai ban đầu
              </Text>
            </div>

            <div
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 8,
                padding: '8px 18px',
                borderRadius: 20,
                background: 'rgba(11, 20, 32, 0.60)',
                border: '1px solid rgba(255, 255, 255, 0.18)',
                backdropFilter: 'blur(8px)',
                WebkitBackdropFilter: 'blur(8px)',
              }}
            >
              <span className="wms-pulse-dot" />
              <Text style={{ color: '#F1F5F9', fontSize: 13, fontWeight: 500 }}>
                Hỗ trợ xuất kho chống trôi date (FEFO)
              </Text>
            </div>
          </div>
        </div>
      </section>

      {/* Section Models: Kho thường vs Kho Date */}
      <section
        id="models"
        style={{
          background: '#FFFFFF',
          padding: '80px 24px',
          borderTop: '1px solid #E2E8F0',
          borderBottom: '1px solid #E2E8F0',
        }}
      >
        <div style={{ maxWidth: 1200, margin: '0 auto' }}>
          <div style={{ textAlign: 'center', maxWidth: 700, margin: '0 auto 56px' }}>
            <span
              style={{
                color: primaryColor,
                fontWeight: 600,
                fontSize: 13,
                letterSpacing: '0.06em',
                textTransform: 'uppercase',
              }}
            >
              Linh hoạt theo đặc thù sản phẩm
            </span>
            <Title level={2} style={{ marginTop: 8, color: '#0B1420', fontSize: 32 }}>
              Lựa chọn mô hình kho phù hợp ngành hàng
            </Title>
            <Paragraph style={{ color: '#5A6672', fontSize: 15 }}>
              Doanh nghiệp lựa chọn 1 lần duy nhất khi đăng ký tài khoản. Hệ thống tự động tối ưu giao diện và luồng nghiệp vụ tương ứng.
            </Paragraph>
          </div>

          <Row gutter={[32, 32]}>
            <Col xs={24} md={12}>
              <Card
                className="wms-card-hover"
                style={{
                  height: '100%',
                  borderRadius: 14,
                  borderColor: '#E2E8F0',
                  boxShadow: '0 4px 12px rgba(0, 0, 0, 0.03)',
                  cursor: 'pointer',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
                  <div
                    style={{
                      padding: '4px 10px',
                      borderRadius: 6,
                      background: '#F0F5FF',
                      border: '1px solid #BAE0FF',
                      color: primaryColor,
                      fontFamily: 'monospace',
                      fontWeight: 700,
                      fontSize: 13,
                      letterSpacing: 0.5,
                    }}
                  >
                    FIFO
                  </div>
                  <div>
                    <Title level={4} style={{ margin: 0, color: '#0B1420' }}>
                      Kho Tiêu chuẩn (Standard FIFO)
                    </Title>
                    <Text style={{ color: '#5A6672', fontSize: 13 }}>
                      Dành cho hàng không quản lý hạn sử dụng
                    </Text>
                  </div>
                </div>

                <Paragraph style={{ color: '#5A6672', fontSize: 14, lineHeight: 1.6 }}>
                  Phù hợp cho các doanh nghiệp: <strong>Gỗ, thép, điện tử, linh kiện máy móc, phụ tùng, thời trang, đồ gia dụng.</strong>
                </Paragraph>

                <ul style={{ paddingLeft: 20, color: '#5A6672', fontSize: 14, lineHeight: 1.8 }}>
                  <li>Xuất kho theo nguyên tắc <strong>FIFO</strong> (Nhập trước xuất trước theo ngày tạo lô hàng).</li>
                  <li>Giao diện tinh giản, không hiển thị trường Lô/Hạn dùng gây phức tạp thao tác.</li>
                  <li>Quy trình nhập kho nhanh gọn, tập trung vào số lượng và kiểm đếm vị trí.</li>
                  <li>Tối ưu tốc độ lấy hàng và quay vòng diện tích lưu kho.</li>
                </ul>
              </Card>
            </Col>

            <Col xs={24} md={12}>
              <Card
                className="wms-card-hover"
                style={{
                  height: '100%',
                  borderRadius: 14,
                  borderColor: '#BAE0FF',
                  background: '#FBFDFF',
                  boxShadow: '0 4px 12px rgba(22, 119, 255, 0.06)',
                  cursor: 'pointer',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
                  <div
                    style={{
                      padding: '4px 10px',
                      borderRadius: 6,
                      background: '#FFFBEB',
                      border: '1px solid #FDE68A',
                      color: '#D97706',
                      fontFamily: 'monospace',
                      fontWeight: 700,
                      fontSize: 13,
                      letterSpacing: 0.5,
                    }}
                  >
                    FEFO
                  </div>
                  <div>
                    <Title level={4} style={{ margin: 0, color: '#0B1420' }}>
                      Kho Quản lý Hạn dùng (Expiry FEFO)
                    </Title>
                    <Text style={{ color: '#5A6672', fontSize: 13 }}>
                      Dành cho hàng có date, kiểm soát theo lô
                    </Text>
                  </div>
                </div>

                <Paragraph style={{ color: '#5A6672', fontSize: 14, lineHeight: 1.6 }}>
                  Phù hợp cho các doanh nghiệp: <strong>Thực phẩm, đồ uống (F&B), dược phẩm, hóa mỹ phẩm, thực phẩm chức năng.</strong>
                </Paragraph>

                <ul style={{ paddingLeft: 20, color: '#5A6672', fontSize: 14, lineHeight: 1.8 }}>
                  <li>Xuất kho theo nguyên tắc <strong>FEFO</strong> (Ưu tiên hàng cận hạn xuất trước).</li>
                  <li>Bắt buộc nhập số Lô (Lot Number) và Ngày hết hạn (Expiry Date) khi nhận hàng.</li>
                  <li>Cảnh báo tự động các lô hàng cận hạn sử dụng trên Dashboard để kịp thời xử lý.</li>
                  <li>Khóa tự động các lô hết hạn, ngăn chặn xuất nhầm cho khách hàng.</li>
                </ul>
              </Card>
            </Col>
          </Row>
        </div>
      </section>

      {/* Section Features */}
      <section
        id="features"
        style={{
          padding: '80px 24px',
          maxWidth: 1200,
          margin: '0 auto',
        }}
      >
        <div style={{ textAlign: 'center', maxWidth: 700, margin: '0 auto 56px' }}>
          <span
            style={{
              color: primaryColor,
              fontWeight: 600,
              fontSize: 13,
              letterSpacing: '0.06em',
              textTransform: 'uppercase',
            }}
          >
            Tính năng cốt lõi
          </span>
          <Title level={2} style={{ marginTop: 8, color: '#0B1420', fontSize: 32 }}>
            Bộ công cụ vận hành kho chuyên nghiệp
          </Title>
          <Paragraph style={{ color: '#5A6672', fontSize: 15 }}>
            Thiết kế trực quan, dễ sử dụng cho mọi vai trò từ Ban quản lý đến nhân viên thủ kho.
          </Paragraph>
        </div>

        <Row gutter={[24, 24]}>
          <Col xs={24} sm={12} lg={6}>
            <Card
              className="wms-card-hover"
              style={{
                height: '100%',
                borderRadius: 12,
                borderColor: '#E2E8F0',
                cursor: 'pointer',
              }}
            >
              <div
                style={{
                  display: 'inline-block',
                  padding: '3px 8px',
                  borderRadius: 4,
                  background: '#E6F4FF',
                  color: primaryColor,
                  fontFamily: 'monospace',
                  fontWeight: 700,
                  fontSize: 12,
                  marginBottom: 14,
                }}
              >
                AI_OCR
              </div>
              <Title level={5} style={{ color: '#0B1420', marginBottom: 8 }}>
                Quét Hóa Đơn Bằng AI
              </Title>
              <Text style={{ color: '#5A6672', fontSize: 13, lineHeight: 1.6, display: 'block' }}>
                Nhận diện nhanh hóa đơn nhà cung cấp, tự động đối soát mã hàng và số lượng mà không cần gõ phím thủ công.
              </Text>
            </Card>
          </Col>

          <Col xs={24} sm={12} lg={6}>
            <Card
              className="wms-card-hover"
              style={{
                height: '100%',
                borderRadius: 12,
                borderColor: '#E2E8F0',
                cursor: 'pointer',
              }}
            >
              <div
                style={{
                  display: 'inline-block',
                  padding: '3px 8px',
                  borderRadius: 4,
                  background: '#F0FDF4',
                  color: '#16A34A',
                  fontFamily: 'monospace',
                  fontWeight: 700,
                  fontSize: 12,
                  marginBottom: 14,
                }}
              >
                MATRIX_LOC
              </div>
              <Title level={5} style={{ color: '#0B1420', marginBottom: 8 }}>
                Sơ Đồ Vị Trí Kệ Kho
              </Title>
              <Text style={{ color: '#5A6672', fontSize: 13, lineHeight: 1.6, display: 'block' }}>
                Trực quan hóa sơ đồ kệ kho theo tầng và dãy. Hướng dẫn nhân viên cất hàng và nhặt hàng theo lộ trình nhanh nhất.
              </Text>
            </Card>
          </Col>

          <Col xs={24} sm={12} lg={6}>
            <Card
              className="wms-card-hover"
              style={{
                height: '100%',
                borderRadius: 12,
                borderColor: '#E2E8F0',
                cursor: 'pointer',
              }}
            >
              <div
                style={{
                  display: 'inline-block',
                  padding: '3px 8px',
                  borderRadius: 4,
                  background: '#FFFBEB',
                  color: '#D97706',
                  fontFamily: 'monospace',
                  fontWeight: 700,
                  fontSize: 12,
                  marginBottom: 14,
                }}
              >
                LIFECYCLE
              </div>
              <Title level={5} style={{ color: '#0B1420', marginBottom: 8 }}>
                Kiểm Soát Vòng Đời Đơn
              </Title>
              <Text style={{ color: '#5A6672', fontSize: 13, lineHeight: 1.6, display: 'block' }}>
                Quản lý liên thông từ Đơn mua hàng (PO) → Nhập kho → Đơn bán (SO) → Lấy hàng → Bàn giao giao vận.
              </Text>
            </Card>
          </Col>

          <Col xs={24} sm={12} lg={6}>
            <Card
              className="wms-card-hover"
              style={{
                height: '100%',
                borderRadius: 12,
                borderColor: '#E2E8F0',
                cursor: 'pointer',
              }}
            >
              <div
                style={{
                  display: 'inline-block',
                  padding: '3px 8px',
                  borderRadius: 4,
                  background: '#FAF5FF',
                  color: '#7C3AED',
                  fontFamily: 'monospace',
                  fontWeight: 700,
                  fontSize: 12,
                  marginBottom: 14,
                }}
              >
                RBAC_ROLES
              </div>
              <Title level={5} style={{ color: '#0B1420', marginBottom: 8 }}>
                Phân Quyền Chặt Chẽ
              </Title>
              <Text style={{ color: '#5A6672', fontSize: 13, lineHeight: 1.6, display: 'block' }}>
                Chủ doanh nghiệp dễ dàng cấp quyền và giao việc chi tiết cho từng quản lý kho và nhân viên tác nghiệp.
              </Text>
            </Card>
          </Col>
        </Row>
      </section>

      {/* Section Security & Privacy */}
      <section
        id="architecture"
        style={{
          background: '#0B1420',
          color: '#FFFFFF',
          padding: '80px 24px',
        }}
      >
        <div style={{ maxWidth: 840, margin: '0 auto', textAlign: 'center' }}>
          <span style={{ color: '#38BDF8', fontSize: 13, fontWeight: 600, letterSpacing: '0.06em' }}>
            AN TÂM & BẢO MẬT DỮ LIỆU
          </span>
          <Title level={2} style={{ color: '#FFFFFF', marginTop: 12, marginBottom: 16 }}>
            Dữ liệu kho hàng của bạn được bảo mật tuyệt đối
          </Title>
          <Paragraph style={{ color: '#94A3B8', fontSize: 16, lineHeight: 1.7, maxWidth: 700, margin: '0 auto 28px' }}>
            Mỗi doanh nghiệp hoạt động trong một không gian quản trị hoàn toàn riêng biệt.
            Toàn bộ thông tin kho bãi, hàng hóa, đối tác và lịch sử xuất nhập đều được phân quyền chặt chẽ,
            cam kết bảo mật và tuyệt đối không rò rỉ hay lẫn lộn giữa các doanh nghiệp.
          </Paragraph>

          <Button
            type="primary"
            size="large"
            onClick={() => navigate('/register-tenant')}
            style={{ borderRadius: 8, height: 48, paddingInline: 32, fontSize: 16, fontWeight: 600 }}
          >
            Tạo tài khoản doanh nghiệp ngay <ArrowRightOutlined />
          </Button>
        </div>
      </section>

      {/* Footer */}
      <footer
        style={{
          background: '#070D14',
          borderTop: '1px solid rgba(255, 255, 255, 0.08)',
          padding: '40px 24px',
          color: '#94A3B8',
          fontSize: 13,
        }}
      >
        <div
          style={{
            maxWidth: 1200,
            margin: '0 auto',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: 16,
          }}
        >
          <Logo size={28} withWordmark wordmarkColor="#FFFFFF" />
          <div>© {new Date().getFullYear()} WMS B2B SaaS Platform. All rights reserved.</div>
          <div style={{ display: 'flex', gap: 20 }}>
            <span style={{ cursor: 'pointer' }} onClick={() => navigate('/login')}>
              Đăng nhập
            </span>
            <span style={{ cursor: 'pointer' }} onClick={() => navigate('/register-tenant')}>
              Đăng ký
            </span>
          </div>
        </div>
      </footer>
    </div>
  )
}
