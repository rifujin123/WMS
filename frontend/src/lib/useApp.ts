import { App as AntApp } from 'antd'
import { toast } from './toast'

/**
 * Hook thay thế App.useApp() để tự động chuyển toàn bộ lệnh gọi message sang toast (sonner).
 * Các phương thức Modal / Notification của Ant Design vẫn được giữ nguyên.
 */
export function useApp() {
  const antApp = AntApp.useApp()

  const messageProxy = {
    success: (content: string) => {
      toast.success(content)
      return Promise.resolve(true)
    },
    error: (content: unknown) => {
      toast.error(content)
      return Promise.resolve(true)
    },
    warning: (content: string) => {
      toast.warning(content)
      return Promise.resolve(true)
    },
    info: (content: string) => {
      toast.info(content)
      return Promise.resolve(true)
    },
    loading: (content: string) => {
      toast.info(content)
      return () => {}
    },
    open: (config: { content: string; type?: 'success' | 'error' | 'warning' | 'info' }) => {
      if (config.type === 'success') toast.success(config.content)
      else if (config.type === 'error') toast.error(config.content)
      else if (config.type === 'warning') toast.warning(config.content)
      else toast.info(config.content)
      return Promise.resolve(true)
    },
    destroy: () => {},
  }

  return {
    ...antApp,
    message: messageProxy,
  }
}
