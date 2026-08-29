/* eslint-disable react-refresh/only-export-components */
import { lazy, Suspense } from 'react'
import { createBrowserRouter, Navigate } from 'react-router-dom'
import { Spin } from 'antd'
import AppLayout from '../components/Layout/AppLayout'
import ProtectedRoute from './ProtectedRoute'
import PublicOnlyRoute from './PublicOnlyRoute'

// Lazy load từng trang — mỗi route thành chunk riêng, giảm bundle chính.
const Dashboard = lazy(() => import('../pages/Dashboard'))
const Login = lazy(() => import('../pages/Login'))
const Products = lazy(() => import('../pages/Products'))
const Categories = lazy(() => import('../pages/Categories'))
const Profile = lazy(() => import('../pages/Profile'))
const Users = lazy(() => import('../pages/Users'))
const Warehouses = lazy(() => import('../pages/Warehouses'))
const WarehouseLocations = lazy(() => import('../pages/Warehouses/WarehouseDetail/locations'))
const Customers = lazy(() => import('../pages/Customers'))
const Vendors = lazy(() => import('../pages/Vendors'))
const PurchaseOrders = lazy(() => import('../pages/PurchaseOrders'))
const Receivings = lazy(() => import('../pages/Receivings'))
const ReceivingDetail = lazy(() => import('../pages/Receivings/detail'))
const PutAwayTasks = lazy(() => import('../pages/PutAwayTasks'))
const Stocks = lazy(() => import('../pages/Stocks'))
const StockAdjustments = lazy(() => import('../pages/StockAdjustments'))
const SaleOrders = lazy(() => import('../pages/SaleOrders'))
const Pickings = lazy(() => import('../pages/Pickings'))
const Forbidden = lazy(() => import('../pages/Forbidden'))

function PageFallback() {
  return (
    <div style={{ display: 'flex', justifyContent: 'center', padding: 48 }}>
      <Spin size="large" />
    </div>
  )
}

export const router = createBrowserRouter([
  {
    element: <PublicOnlyRoute />,
    children: [
      {
        path: '/login',
        element: (
          <Suspense fallback={<PageFallback />}>
            <Login />
          </Suspense>
        ),
      },
    ],
  },
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      {
        element: <ProtectedRoute />,
        children: [
          { path: '403', element: <Suspense fallback={<PageFallback />}><Forbidden /></Suspense> },
          { path: 'dashboard', element: <Suspense fallback={<PageFallback />}><Dashboard /></Suspense> },
          { path: 'profile', element: <Suspense fallback={<PageFallback />}><Profile /></Suspense> },
          {
            element: <ProtectedRoute allowedRoles={['Admin']} />,
            children: [
              { path: 'users', element: <Suspense fallback={<PageFallback />}><Users /></Suspense> },
              { path: 'products', element: <Suspense fallback={<PageFallback />}><Products /></Suspense> },
              { path: 'categories', element: <Suspense fallback={<PageFallback />}><Categories /></Suspense> },
              { path: 'warehouses', element: <Suspense fallback={<PageFallback />}><Warehouses /></Suspense> },
              {
                path: 'warehouses/:id/locations',
                element: <Suspense fallback={<PageFallback />}><WarehouseLocations /></Suspense>,
              },
              { path: 'customers', element: <Suspense fallback={<PageFallback />}><Customers /></Suspense> },
              { path: 'vendors', element: <Suspense fallback={<PageFallback />}><Vendors /></Suspense> },
            ],
          },
          {
            element: <ProtectedRoute allowedRoles={['Admin', 'WarehouseManager']} />,
            children: [
              { path: 'sale-orders', element: <Suspense fallback={<PageFallback />}><SaleOrders /></Suspense> },
              {
                path: 'stock-adjustments',
                element: <Suspense fallback={<PageFallback />}><StockAdjustments /></Suspense>,
              },
            ],
          },
          {
            element: <ProtectedRoute allowedRoles={['Admin', 'WarehouseManager', 'WarehouseStaff']} />,
            children: [
              {
                path: 'purchase-orders',
                element: <Suspense fallback={<PageFallback />}><PurchaseOrders /></Suspense>,
              },
              { path: 'receivings', element: <Suspense fallback={<PageFallback />}><Receivings /></Suspense> },
              {
                path: 'receivings/:id',
                element: <Suspense fallback={<PageFallback />}><ReceivingDetail /></Suspense>,
              },
              {
                path: 'putaway-tasks',
                element: <Suspense fallback={<PageFallback />}><PutAwayTasks /></Suspense>,
              },
              { path: 'pickings', element: <Suspense fallback={<PageFallback />}><Pickings /></Suspense> },
              { path: 'stock', element: <Suspense fallback={<PageFallback />}><Stocks /></Suspense> },
            ],
          },
          { path: '*', element: <Navigate to="/dashboard" replace /> },
        ],
      },
    ],
  },
])