import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { App as AntApp, message, notification } from 'antd'
import dayjs from 'dayjs'
import 'dayjs/locale/es'
import { AuthProvider } from './auth/AuthContext'
import App from './App'
import { CredixStaticDialogHost } from './components/credix/CredixStaticDialogHost'
import { CredixColorSchemeProvider } from './theme/CredixColorSchemeProvider'
import './index.css'
import './styles/credix-design-system.css'
import './styles/credix-antd-bridge.css'
import './styles/credix-module-layout.css'
import './styles/credix-list-toolbar.css'
import './styles/credix-crud-toolbar.css'
import './styles/credix-responsive-global.css'
import './styles/credix-mobile-shell.css'
import './styles/credix-oleada-a.css'
import './styles/credix-intranet.css'
import './styles/credix-structure-lock.css'
import './styles/credix-color-scheme.css'
// CSS de módulos se importa en la página lazy correspondiente (menos CSS en /inicio).

dayjs.locale('es')

message.config({
  top: 56,
  duration: 3.2,
  maxCount: 3,
})

notification.config({
  placement: 'topRight',
  duration: 4.5,
  maxCount: 4,
})

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: true,
      refetchOnReconnect: true,
      staleTime: 30_000,
      networkMode: 'online',
    },
    mutations: {
      networkMode: 'online',
    },
  },
})

if (typeof window !== 'undefined') {
  window.addEventListener('online', () => {
    void queryClient.invalidateQueries()
    void queryClient.refetchQueries({ type: 'active' })
  })
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <CredixColorSchemeProvider>
        <AntApp>
          <CredixStaticDialogHost />
          <BrowserRouter
            basename={
              import.meta.env.BASE_URL.replace(/\/$/, '') || undefined
            }
          >
            <AuthProvider>
              <App />
            </AuthProvider>
          </BrowserRouter>
        </AntApp>
      </CredixColorSchemeProvider>
    </QueryClientProvider>
  </StrictMode>,
)
