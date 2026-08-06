import type { ThemeConfig } from 'antd'

// Design token dùng chung cho toàn app. Giữ Ant Blue làm accent duy nhất.
export const themeConfig: ThemeConfig = {
  token: {
    colorPrimary: '#1677FF',
    colorBgLayout: '#F5F7FA', // nền xám lạnh, không dùng #fff phẳng
    colorText: '#141A21', // off-black, không dùng #000
    colorTextSecondary: '#5A6672',
    colorBorder: '#D9E0E8',
    colorBorderSecondary: '#EDF1F5',
    colorError: '#DC2626',
    colorSuccess: '#16A34A',
    borderRadius: 8, // shape lock: 8px cho input/card/button
    borderRadiusLG: 12, // chỉ dùng cho panel lớn
    fontFamily:
      "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
    fontSize: 14,
    controlHeight: 40, // input/button mặc định 40px
    controlHeightLG: 44, // nút submit chính
  },
  components: {
    Input: { paddingBlock: 9 },
    Layout: { headerBg: '#FFFFFF', bodyBg: '#F5F7FA', siderBg: '#0B1420' },
    Menu: { darkItemBg: '#0B1420', darkItemSelectedBg: '#1677FF' },
    Card: { boxShadow: '0 1px 2px rgba(20,26,33,0.05)' },
  },
}

export const ui = {
  formWidth: 400, // bề rộng cột form đăng nhập
  brandNavy: '#0B1420', // nền panel brand (cùng tông siderBg)
  easing: 'cubic-bezier(0.16, 1, 0.3, 1)',
}