export interface CustomerDto {
  id: string
  name: string
  contactName?: string
  phone?: string
  email?: string
  address?: string
}

export interface CreateCustomerDto {
  name: string
  contactName?: string
  phone?: string
  email?: string
  address?: string
}