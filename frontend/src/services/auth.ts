import api from '../lib/axios'
import type {AuthResponse, LoginDto, RegisterDto} from '../types/auth'

export function login(dto: LoginDto): Promise<AuthResponse>{
    return api.post('/Auth/login', dto).then(res => res.data)
}

export function register(dto: RegisterDto): Promise<{message:string}>{
    return api.post('/Auth/register', dto).then(res => res.data)
}