import axios from 'axios'
import { getErrorMessage } from './errorHandler'

const TOKEN_KEY = 'accessToken'

const api = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL,
})

api.interceptors.request.use((config) => {
    const token = localStorage.getItem(TOKEN_KEY)
    if(token)
    {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

api.interceptors.response.use(
    (response) => {
        // Unwrap API response: { success, code, message, data, timestamp } → return data only
        return response.data?.data ?? response.data
    },
    (error) => {
        const isLoginRequest = error.config?.url?.includes('/Auth/login') || error.config?.url?.includes('/auth/login')
        if(error.response?.status === 401 && !isLoginRequest){
            localStorage.removeItem(TOKEN_KEY)
            localStorage.removeItem('user')
            window.location.href = '/login'
        }
        
        // Gán message đã được dịch/chuẩn hóa từ getErrorMessage vào error.message
        error.message = getErrorMessage(error)
        return Promise.reject(error)
    },
)

export default api