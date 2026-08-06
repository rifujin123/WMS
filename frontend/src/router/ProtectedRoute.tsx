import { Navigate, Outlet } from 'react-router-dom'
import { useAuthContext } from '../contexts/AuthContext'

function ProtectedRoute() {
  const { user } = useAuthContext()
  if (!user) return <Navigate to="/login" replace />
  return <Outlet />
}

export default ProtectedRoute