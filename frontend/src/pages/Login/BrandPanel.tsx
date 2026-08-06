import Logo from '../../components/Logo'
import { ui } from '../../theme/tokens'

function BrandPanel() {
  return (
    <div
      className="wms-rise"
      style={{
        flex: 1.1,
        position: 'relative',
        overflow: 'hidden',
        background: ui.brandNavy,
        animationDelay: '80ms',
      }}
    >
      <img
        src="https://als.com.vn/api/file-management/file-descriptor/view/d4ef661d-c299-de44-de61-3a07957c88d4"
        alt="Kho hàng"
        style={{
          position: 'absolute',
          inset: 0,
          width: '100%',
          height: '100%',
          objectFit: 'cover',
          opacity: 0.8,
        }}
      />
      {/* Scrim tối dần xuống dưới để chữ trắng đạt contrast */}
      <div
        style={{
          position: 'absolute',
          inset: 0,
          background:
            'linear-gradient(180deg, rgba(11,20,32,0.55) 0%, rgba(11,20,32,0.92) 100%)',
        }}
      />
      <div
        style={{
          position: 'relative',
          zIndex: 1,
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
          padding: 48,
        }}
      >
        <Logo size={36} withWordmark />

        <div
          style={{
            flex: 1,
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
          }}
        >
          <h1
            style={{
              margin: 0,
              color: '#fff',
              fontSize: 36,
              fontWeight: 600,
              lineHeight: 1.25,
              maxWidth: 380,
            }}
          >
            Quản lý kho, một nơi duy nhất.
          </h1>
          <p
            style={{
              margin: '12px 0 0',
              color: 'rgba(255,255,255,0.72)',
              fontSize: 15,
              lineHeight: 1.7,
              maxWidth: 340,
            }}
          >
            Theo dõi tồn kho, nhập xuất và điều chuyển theo thời gian thực.
          </p>
        </div>
      </div>
    </div>
  )
}

export default BrandPanel