import { Navigate, Outlet } from 'react-router-dom'
import { useAuthContext } from '../contexts/useAuthContext'
import { hasRole } from './routeRoles'

interface ProtectedRouteProps {
  allowedRoles?: string[]
}

function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { user } = useAuthContext()

  if (!user) return <Navigate to="/login" replace />
  if (allowedRoles && !hasRole(user.role, allowedRoles)) {
    return <Navigate to="/403" replace />
  }

  return <Outlet />
}

export default ProtectedRoute