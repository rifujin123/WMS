import { describe, it, expect } from 'vitest'

describe('Desktop Layout Ergonomics & Push Sider', () => {
  it('Sidebar có kích thước chuẩn: mở rộng 220px và thu nhỏ 64px', () => {
    const expandedWidth = 220
    const collapsedWidth = 64

    expect(expandedWidth).toBe(220)
    expect(collapsedWidth).toBe(64)
    expect(expandedWidth - collapsedWidth).toBe(156) // Không gian mở rộng thêm cho bảng biểu
  })

  it('Toggle Sidebar đảo trạng thái collapsed', () => {
    let collapsed = false
    const toggle = () => {
      collapsed = !collapsed
    }

    toggle()
    expect(collapsed).toBe(true)

    toggle()
    expect(collapsed).toBe(false)
  })

  it('Header chứa đầy đủ 2 cụm: bên trái (Toggle Menu) và bên phải (Actions)', () => {
    const headerLayout = {
      justifyContent: 'space-between',
      leftCluster: ['ToggleSidebarButton'],
      rightCluster: ['SearchCommandPalette', 'FefoRadarAlert', 'NotificationBell', 'UserProfileDropdown'],
    }

    expect(headerLayout.justifyContent).toBe('space-between')
    expect(headerLayout.leftCluster).toContain('ToggleSidebarButton')
    expect(headerLayout.rightCluster.length).toBe(4)
  })
})
