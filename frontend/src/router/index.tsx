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
          { path: 'users', element: <Users /> },
          { path: 'profile', element: <Profile /> },
        ],
      },
    ],
  },
])