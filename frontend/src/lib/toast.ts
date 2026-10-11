import { toast as sonnerToast } from 'sonner'
import { getErrorMessage } from './errorHandler'

export interface ToastOptions {
  description?: string
  duration?: number
}

/**
 * Toast utility bọc ngoài sonner để chuẩn hóa thông báo toàn hệ thống WMS.
 * Tự động bóc tách lỗi từ Error / AxiosError / string.
 */
export const toast = {
  success: (message: string, descriptionOrOptions?: string | ToastOptions) => {
    const description = typeof descriptionOrOptions === 'string' ? descriptionOrOptions : descriptionOrOptions?.description
    const duration = typeof descriptionOrOptions === 'object' ? descriptionOrOptions?.duration : 3500
    sonnerToast.success(message, {
      description,
      duration: duration ?? 3500,
    })
  },

  error: (errorOrMessage: unknown, fallbackOrOptions?: string | ToastOptions) => {
    let title: string
    let description: string | undefined = undefined
    let duration = 6000 // Lỗi mặc định hiện 6s để kịp đọc

    if (typeof fallbackOrOptions === 'object') {
      duration = fallbackOrOptions.duration ?? 6000
    }

    if (typeof errorOrMessage === 'string') {
      title = errorOrMessage
      if (typeof fallbackOrOptions === 'string') {
        description = fallbackOrOptions
      }
    } else {
      const fallbackMsg = typeof fallbackOrOptions === 'string' ? fallbackOrOptions : 'Thao tác không thành công. Vui lòng thử lại.'
      const extracted = getErrorMessage(errorOrMessage, fallbackMsg)

      // Nếu có fallback title truyền vào dạng object description
      if (typeof fallbackOrOptions === 'object' && fallbackOrOptions.description) {
        title = fallbackOrOptions.description
        description = extracted
      } else if (typeof fallbackOrOptions === 'string') {
        title = fallbackOrOptions
        description = extracted !== fallbackOrOptions ? extracted : undefined
      } else {
        title = extracted
      }
    }

    sonnerToast.error(title, {
      description,
      duration,
    })
  },

  warning: (message: string, descriptionOrOptions?: string | ToastOptions) => {
    const description = typeof descriptionOrOptions === 'string' ? descriptionOrOptions : descriptionOrOptions?.description
    const duration = typeof descriptionOrOptions === 'object' ? descriptionOrOptions?.duration : 4500
    sonnerToast.warning(message, {
      description,
      duration: duration ?? 4500,
    })
  },

  info: (message: string, descriptionOrOptions?: string | ToastOptions) => {
    const description = typeof descriptionOrOptions === 'string' ? descriptionOrOptions : descriptionOrOptions?.description
    const duration = typeof descriptionOrOptions === 'object' ? descriptionOrOptions?.duration : 4000
    sonnerToast.info(message, {
      description,
      duration: duration ?? 4000,
    })
  },
}
