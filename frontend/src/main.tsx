import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { App as AntApp, ConfigProvider } from 'antd'
import { Toaster } from 'sonner'
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
            <Toaster
              position="top-right"
              richColors
              closeButton
              expand={false}
              duration={4000}
              toastOptions={{
                style: {
                  fontFamily: themeConfig.token?.fontFamily,
                  borderRadius: `${themeConfig.token?.borderRadius}px`,
                },
              }}
            />
          </AuthProvider>
        </AntApp>
      </ConfigProvider>
    </QueryClientProvider>
  </StrictMode>,
)
