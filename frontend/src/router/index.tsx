import { createBrowserRouter, Navigate } from 'react-router-dom'
import AppLayout from '../components/Layout/AppLayout'
import ProtectedRoute from './ProtectedRoute'
import PublicOnlyRoute from './PublicOnlyRoute'
import Dashboard from '../pages/Dashboard'
import Login from '../pages/Login'
import Products from '../pages/Products'
import Categories from '../pages/Categories'
import Profile from '../pages/Profile'
import Users from '../pages/Users'
import Warehouses from '../pages/Warehouses'
import WarehouseLocations from '../pages/Warehouses/WarehouseDetail/locations'
import PurchaseOrders from '../pages/PurchaseOrders'
import Receivings from '../pages/Receivings'
import ReceivingDetail from '../pages/Receivings/detail'
import PutAwayTasks from '../pages/PutAwayTasks'
import Stocks from '../pages/Stocks'

export const router = createBrowserRouter([
  {
    element: <PublicOnlyRoute />,
    children: [{ path: '/login', element: <Login /> }],
  },
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      {
        element: <ProtectedRoute />,
        children: [
          { path: 'dashboard', element: <Dashboard /> },
          { path: 'products', element: <Products /> },
          { path: 'categories', element: <Categories /> },
          { path: 'purchase-orders', element: <PurchaseOrders /> },
          { path: 'receivings', element: <Receivings /> },
          { path: 'receivings/:id', element: <ReceivingDetail /> },
          { path: 'putaway-tasks', element: <PutAwayTasks /> },
          { path: 'stock', element: <Stocks /> },
          { path: 'warehouses', element: <Warehouses /> },
          { path: 'warehouses/:id/locations', element: <WarehouseLocations /> },
          { path: 'users', element: <Users /> },
          { path: 'profile', element: <Profile /> },
        ],
      },
    ],
  },
])