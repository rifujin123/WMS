import { Navigate, Outlet } from 'react-router-dom'
import { useAuthContext } from '../contexts/useAuthContext'

// Chặn người dùng đã đăng nhập truy cập trang login — ngược lại với ProtectedRoute
function PublicOnlyRoute() {
  const { user } = useAuthContext()
  if (user) return <Navigate to="/dashboard" replace />
  return <Outlet />
}

export default PublicOnlyRoute