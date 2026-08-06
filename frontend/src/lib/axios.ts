import axios from 'axios'

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
    (response) => response,
    (error) => {
        const isLoginRequest = error.config?.url?.includes('/Auth/login')
        if(error.response?.status === 401 && !isLoginRequest){
            localStorage.removeItem(TOKEN_KEY)
            localStorage.removeItem('user')
            window.location.href = '/login'
        }
        return Promise.reject(error)
    },
)

export default api