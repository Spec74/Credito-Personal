import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ConfigProvider } from 'antd'
import esES from 'antd/locale/es_ES'
import { AuthProvider } from './auth/AuthContext'
import App from './App'
import { credixLegacyTheme } from './theme/credixLegacyTheme'
import './index.css'
import './styles/credix-design-system.css'
import './styles/credix-module-layout.css'
import './styles/credix-list-toolbar.css'
import './styles/credix-crud-toolbar.css'
import './styles/credix-responsive-global.css'
import './styles/caja-diario.css'
import './styles/caja-list-toolbar.css'
import './styles/caja-saldos-module.css'
import './styles/caja-verificar-pagos-module.css'
import './styles/caja-chica-module.css'
import './styles/caja-maestro-module.css'
import './styles/comprobantes-caja-chica-module.css'
import './styles/credito-consulta.css'
import './styles/credito-tareas.css'
import './styles/credito-aprobacion-module.css'
import './styles/clientes-module.css'
import './styles/cliente-form.css'
import './styles/boveda-module.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 30_000,
    },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <ConfigProvider locale={esES} theme={credixLegacyTheme}>
        <BrowserRouter
          basename={
            import.meta.env.BASE_URL.replace(/\/$/, '') || undefined
          }
        >
          <AuthProvider>
            <App />
          </AuthProvider>
        </BrowserRouter>
      </ConfigProvider>
    </QueryClientProvider>
  </StrictMode>,
)
