import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getUsers,
  getWarehouseStaff,
  resetUserPassword,
  setUserLock,
  updateUser,
} from '../services/user'
import type { ResetPasswordDto, UpdateUserDto } from '../types/user'

export function useUsers(filters?: { role?: string; search?: string; status?: string }) {
  return useQuery({ queryKey: ['users', filters], queryFn: () => getUsers(filters) })
}

export function useWarehouseStaff() {
  return useQuery({ queryKey: ['warehouseStaff'], queryFn: () => getWarehouseStaff() })
}

export function useUpdateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateUserDto }) => updateUser(id, dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  })
}

export function useResetUserPassword() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: ResetPasswordDto }) => resetUserPassword(id, dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  })
}

export function useSetUserLock() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, locked }: { id: string; locked: boolean }) => setUserLock(id, locked),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  })
}
