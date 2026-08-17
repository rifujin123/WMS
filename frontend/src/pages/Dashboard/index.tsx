import { Empty } from 'antd'
import { useAuthContext } from '../../contexts/AuthContext'
import AdminDashboard from './AdminDashboard'
import ManagerDashboard from './ManagerDashboard'
import StaffDashboard from './StaffDashboard'

function Dashboard() {
  const { user } = useAuthContext()
  const role = user?.role

  if (role === 'Admin') return <AdminDashboard />
  if (role === 'WarehouseManager') return <ManagerDashboard />
  if (role === 'WarehouseStaff') return <StaffDashboard />

  return <Empty image={null} description="Không xác định được vai trò" />
}

export default Dashboard
