import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { App as AntApp, ConfigProvider } from 'antd'
import App from './App'
import { themeConfig } from './theme/tokens'
import './index.css'
import { AuthProvider } from './contexts/AuthContext'

const queryClient = new QueryClient()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <ConfigProvider theme={themeConfig}>
        <AntApp>
          <AuthProvider>
            <App />
          </AuthProvider>
        </AntApp>
      </ConfigProvider>
    </QueryClientProvider>
  </StrictMode>,
)