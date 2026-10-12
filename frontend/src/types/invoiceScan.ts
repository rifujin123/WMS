export interface InvoiceLineItem {
  sku: string
  name: string
  quantity: number
  unitPrice?: number
  totalAmount?: number
}

export interface ProductSuggestion {
  productId: string
  sku: string
  name: string
  inPo: boolean
}

export interface ProductMatch {
  // undefined = chưa khớp tự động, cần chọn từ suggestions
  productId?: string
  sku: string
  name: string
  quantity: number
  unitPrice?: number
  totalAmount?: number
  suggestions: ProductSuggestion[]
  confidenceScore?: number
}

export interface InvoiceScanResult {
  invoiceNumber: string
  vendorName: string
  invoiceDate?: string
  imageUrl: string
  products: ProductMatch[]
  confidenceScore?: number
}
