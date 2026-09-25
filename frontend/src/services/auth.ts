import api from '../lib/axios'
import type { AuthResponse, LoginDto, RegisterDto, RegisterTenantDto, VerifyEmailDto } from '../types/auth'

export function login(dto: LoginDto): Promise<AuthResponse> {
    return api.post('/Auth/login', dto)
}

export function register(dto: RegisterDto): Promise<{ message: string }> {
    return api.post('/Auth/register', dto)
}

export function registerTenant(dto: RegisterTenantDto): Promise<{ message: string }> {
    return api.post('/Tenants/register', dto)
}

export function verifyEmail(dto: VerifyEmailDto): Promise<{ message: string }> {
    return api.post('/Tenants/verify-email', dto)
}