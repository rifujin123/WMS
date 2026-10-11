interface LogoProps {
  size?: number
  withWordmark?: boolean
  wordmarkColor?: string
  customLogoUrl?: string
  companyName?: string
}

// Logo WMS: hỗ trợ tùy biến logo riêng của Tenant hoặc fallback về logo chuẩn WMS.
function Logo({
  size = 32,
  withWordmark = false,
  wordmarkColor = '#fff',
  customLogoUrl,
  companyName,
}: LogoProps) {
  if (customLogoUrl) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, maxWidth: '100%' }}>
        <img
          src={customLogoUrl}
          alt={companyName || 'Logo'}
          style={{ height: size, maxWidth: withWordmark ? 120 : size, objectFit: 'contain' }}
        />
        {withWordmark && (
          <span
            style={{
              color: wordmarkColor,
              fontSize: size * 0.52,
              fontWeight: 600,
              letterSpacing: 0.5,
              whiteSpace: 'nowrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              maxWidth: 140,
            }}
          >
            {companyName || 'WMS'}
          </span>
        )}
      </div>
    )
  }

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
      <svg width={size} height={size} viewBox="0 0 32 32" fill="none" aria-hidden="true">
        <rect width="32" height="32" rx="8" fill="#1677FF" />
        <rect x="8" y="9" width="4" height="13" rx="2" fill="#fff" />
        <rect x="14" y="14" width="4" height="8" rx="2" fill="#fff" />
        <rect x="20" y="9" width="4" height="13" rx="2" fill="#fff" />
      </svg>
      {withWordmark && (
        <span
          style={{
            color: wordmarkColor,
            fontSize: size * 0.56,
            fontWeight: 600,
            letterSpacing: 0.5,
          }}
        >
          {companyName || 'WMS'}
        </span>
      )}
    </div>
  )
}

export default Logo
