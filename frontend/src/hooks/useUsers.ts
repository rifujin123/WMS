import { useQuery } from '@tanstack/react-query'
import { getUsers } from '../services/user'

export function useUsers() {
  return useQuery({ queryKey: ['users'], queryFn: getUsers })
}
