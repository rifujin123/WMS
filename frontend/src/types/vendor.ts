export interface VendorDto {
  id: string
  name: string
  contactName?: string
  phone?: string
  email?: string
  address?: string
}

export interface CreateVendorDto {
  name: string
  contactName?: string
  phone?: string
  email?: string
  address?: string
}