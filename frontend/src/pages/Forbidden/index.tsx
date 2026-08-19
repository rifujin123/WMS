import { Button, Result } from 'antd'
import { useNavigate } from 'react-router-dom'

function Forbidden() {
  const navigate = useNavigate()

  return (
    <Result
      status="403"
      title="Bạn không có quyền truy cập trang này"
      extra={
        <Button type="primary" onClick={() => navigate('/dashboard', { replace: true })}>
          Về dashboard
        </Button>
      }
    />
  )
}

export default Forbidden
