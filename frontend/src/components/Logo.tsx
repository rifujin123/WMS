interface LogoProps {
  size?: number
  withWordmark?: boolean
  wordmarkColor?: string
}

// Logo WMS: tile Ant Blue bo góc 8px, glyph trắng 3 thanh dọc tạo chữ W
// (ý tưởng kệ kho / xếp kiện). Dùng ở BrandPanel, Sider, LoginForm, favicon.
function Logo({
  size = 32,
  withWordmark = false,
  wordmarkColor = '#fff',
}: LogoProps) {
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
          WMS
        </span>
      )}
    </div>
  )
}

export default Logo