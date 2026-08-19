import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuthContext } from '../contexts/useAuthContext'
import {
  changePassword as changePasswordRequest,
  getProfile,
  updateProfile as updateProfileRequest,
  uploadAvatar as uploadAvatarRequest,
} from '../services/user'

export function useProfile() {
  return useQuery({
    queryKey: ['profile'],
    queryFn: getProfile,
  })
}

export function useUpdateProfile() {
  const queryClient = useQueryClient()
  const { updateUser } = useAuthContext()
  return useMutation({
    mutationFn: updateProfileRequest,
    onSuccess: (data) => {
      queryClient.setQueryData(['profile'], data)
      updateUser({ fullName: data.fullName })
    },
  })
}

export function useChangePassword() {
  return useMutation({
    mutationFn: changePasswordRequest,
  })
}

export function useUploadAvatar() {
  const queryClient = useQueryClient()
  const { updateUser } = useAuthContext()
  return useMutation({
    mutationFn: uploadAvatarRequest,
    onSuccess: (data) => {
      updateUser({ avatarUrl: data.avatarUrl })
      queryClient.invalidateQueries({ queryKey: ['profile'] })
    },
  })
}