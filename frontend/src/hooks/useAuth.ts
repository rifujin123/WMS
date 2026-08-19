
import { useMutation } from '@tanstack/react-query'
import { login as loginRequest, register as registerRequest } from '../services/auth'
import { useAuthContext } from '../contexts/useAuthContext'

export function useLogin(){
    const {login} = useAuthContext()
    return useMutation({
        mutationFn: loginRequest,
        onSuccess: (res) => login(res)
    })
}

export function useRegister(){
    return useMutation({
        mutationFn: registerRequest,
    })
}