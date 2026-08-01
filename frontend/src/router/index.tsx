import { createBrowserRouter, Navigate } from 'react-router-dom'
import AppLayout from '../components/Layout/AppLayout'
import Dashboard from '../pages/Dashboard'
import Products from '../pages/Products'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard', element: <Dashboard /> },
      { path: 'products', element: <Products /> },
    ],
  },
])