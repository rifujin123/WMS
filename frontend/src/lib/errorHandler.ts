import { isAxiosError } from 'axios'

/**
 * Từ điển ánh xạ các thông điệp lỗi tiếng Anh từ backend sang Tiếng Việt thân thiện.
 */
const ERROR_TRANSLATIONS: Record<string, string> = {
  // Authentication & Authorization
  'Invalid username or password.': 'Tên đăng nhập hoặc mật khẩu không chính xác.',
  'User not found.': 'Không tìm thấy người dùng.',
  'Can only assign to WarehouseStaff.': 'Chỉ có thể phân công cho nhân viên kho (WarehouseStaff).',
  'Access denied.': 'Bạn không có quyền thực hiện thao tác này.',
  'Unauthorized': 'Hết phiên đăng nhập. Vui lòng đăng nhập lại.',

  // Purchase Order & Receiving
  'PurchaseOrder not found.': 'Không tìm thấy đơn đặt hàng (PO).',
  'PurchaseOrder must be Approved before receiving.': 'Đơn đặt hàng phải ở trạng thái "Đã duyệt" mới có thể tạo phiếu nhận.',
  'PurchaseOrder already has a confirmed receiving.': 'Đơn đặt hàng này đã có phiếu nhận hàng được xác nhận.',
  'Cannot confirm receiving until all PurchaseOrder quantities are processed.': 'Không thể xác nhận: Phiếu nhận phải xử lý đủ số lượng của đơn hàng (PO).',
  'InvoiceImageUrl must be an absolute HTTP or HTTPS URL.': 'Đường dẫn ảnh hóa đơn phải là URL hợp lệ (http/https).',

  // PutAway Tasks
  'PutAway task not found.': 'Không tìm thấy nhiệm vụ cất hàng.',
  'ToLocation must be set before starting putaway.': 'Vui lòng chọn vị trí đích trước khi bắt đầu cất hàng.',
  'ToLocation must be set to complete putaway.': 'Vui lòng chọn vị trí đích để hoàn thành cất hàng.',
  'Destination location not found.': 'Không tìm thấy vị trí lưu trữ đích.',
  'You can only start a task assigned to you.': 'Bạn chỉ có thể bắt đầu nhiệm vụ được phân công cho mình.',
  'You can only complete a task assigned to you.': 'Bạn chỉ có thể hoàn thành nhiệm vụ được phân công cho mình.',

  // Picking & Sale Orders
  'SaleOrder not found.': 'Không tìm thấy đơn bán hàng.',
  'Warehouse not found.': 'Không tìm thấy kho hàng.',
  'No allocatable lines in this SaleOrder.': 'Đơn bán này không còn mặt hàng nào có thể phân bổ lấy hàng.',
  'Picking must be assigned before starting.': 'Phiếu lấy hàng phải được phân công cho nhân viên trước khi bắt đầu.',
  'You can only start a picking assigned to you.': 'Bạn chỉ có thể bắt đầu phiếu lấy hàng được phân công cho mình.',
  'You can only complete a picking assigned to you.': 'Bạn chỉ có thể hoàn thành phiếu lấy hàng được phân công cho mình.',
  'Stock not found for picked location.': 'Không tìm thấy tồn kho tại vị trí đã lấy.',
  'Shipment already exists for this SaleOrder.': 'Đơn bán này đã được tạo phiếu xuất kho.',
  'Shipment has already been marked as shipped.': 'Đơn hàng này đã được đánh dấu là đã giao trước đó.',

  // Stock Adjustment
  'Stock adjustment not found.': 'Không tìm thấy phiếu điều chỉnh tồn kho.',
}

/**
 * Trích xuất thông điệp lỗi chi tiết, dễ hiểu từ error object (Axios / Error / String)
 */
export function getErrorMessage(error: unknown, fallbackMessage = 'Đã có lỗi xảy ra. Vui lòng thử lại.'): string {
  if (!error) return fallbackMessage

  // 1. Trường hợp là Axios Error
  if (isAxiosError(error)) {
    const data = error.response?.data

    // Nếu Backend trả về dạng bọc gói: { success: false, message: "..." }
    if (data?.message && typeof data.message === 'string') {
      const trimmed = data.message.trim()
      // Tra từ điển nếu có ánh xạ
      if (ERROR_TRANSLATIONS[trimmed]) {
        return ERROR_TRANSLATIONS[trimmed]
      }
      // Dịch các lỗi động dạng pattern
      if (trimmed.includes('does not have enough capacity')) {
        return trimmed
          .replace('Location', 'Vị trí')
          .replace('does not have enough capacity. Available:', 'không đủ sức chứa. Còn trống:')
          .replace('Required:', 'Cần cất:')
          .replace('Adjustment delta:', 'Độ lệch điều chỉnh:')
      }
      if (trimmed.includes('Insufficient stock for product')) {
        return trimmed
          .replace('Insufficient stock for product', 'Không đủ tồn kho cho sản phẩm')
          .replace('Required:', 'Cần:')
          .replace('Available:', 'Hiện có:')
          .replace('Available at:', 'Khả dụng tại:')
          .replace('No stock in this warehouse.', 'Hết hàng trong kho này.')
      }
      if (trimmed.includes('already exists')) {
        return trimmed.replace('already exists', 'đã tồn tại trong hệ thống.')
      }
      return trimmed
    }

    // Nếu Backend trả về dạng Validation Errors (ASP.NET ModelState): { errors: { FieldName: ["..."] } }
    if (data?.errors && typeof data.errors === 'object') {
      const errorList = Object.values(data.errors).flat().filter(Boolean)
      if (errorList.length > 0) {
        return errorList.join('; ')
      }
    }

    // Nếu Backend trả về string thuần
    if (typeof data === 'string' && data.trim()) {
      return data
    }

    // Theo HTTP Status code nếu không có response body chi tiết
    switch (error.response?.status) {
      case 400:
        return 'Dữ liệu gửi lên không hợp lệ.'
      case 401:
        return 'Hết phiên đăng nhập. Vui lòng đăng nhập lại.'
      case 403:
        return 'Bạn không có quyền thực hiện thao tác này.'
      case 404:
        return 'Không tìm thấy dữ liệu yêu cầu.'
      case 409:
        return 'Dữ liệu bị xung đột hoặc đã tồn tại.'
      case 500:
      case 502:
      case 503:
        return 'Lỗi máy chủ nội bộ. Vui lòng thử lại sau.'
      default:
        break
    }

    // Lỗi mạng hoặc timeout
    if (error.code === 'ECONNABORTED' || error.message.includes('timeout')) {
      return 'Yêu cầu quá thời gian chờ (Timeout). Vui lòng thử lại.'
    }
    if (error.message === 'Network Error') {
      return 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.'
    }
  }

  // 2. Trường hợp là Error object chuẩn
  if (error instanceof Error && error.message) {
    const trimmed = error.message.trim()
    return ERROR_TRANSLATIONS[trimmed] || trimmed
  }

  // 3. Trường hợp là chuỗi
  if (typeof error === 'string' && error.trim()) {
    return error
  }

  return fallbackMessage
}
