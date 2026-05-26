import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const base = env.VITE_BASE_URL?.trim() || '/'
  const legacyMvcTarget =
    env.VITE_LEGACY_MVC_TARGET?.trim() || 'http://localhost:52099'
  const apiProxyTarget =
    env.VITE_API_PROXY_TARGET?.trim() || 'http://localhost:9080'

  const normId = (id: string) => id.replace(/\\/g, '/')

  const isAntDesignDep = (id: string) => {
    const n = normId(id)
    if (!n.includes('/node_modules/')) return false
    return /\/node_modules\/(antd|@ant-design|@rc-component|rc-|dayjs)(\/|$)/.test(n)
  }

  return {
  base,
  plugins: [react()],
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          const n = normId(id)
          if (n.includes('/node_modules/')) {
            if (isAntDesignDep(n)) return 'vendor-antd'
            if (n.includes('/node_modules/@tanstack/')) return 'vendor-query'
            if (
              n.includes('/node_modules/react-dom/') ||
              n.includes('/node_modules/react-router')
            ) {
              return 'vendor-react'
            }
            return 'vendor-misc'
          }
          // Rutas: un chunk por import() lazy (sin agrupar carpetas — evita megachunks).
        },
      },
    },
    chunkSizeWarningLimit: 600,
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: apiProxyTarget,
        changeOrigin: true,
      },
      '/css': {
        target: apiProxyTarget,
        changeOrigin: true,
      },
      '/img': {
        target: apiProxyTarget,
        changeOrigin: true,
      },
      /** ReportViewer RDLC directo al MVC (evita redirect nginx /Reporte → /app/informes). */
      '/Reporte': {
        target: legacyMvcTarget,
        changeOrigin: true,
        headers: { Host: new URL(legacyMvcTarget).host },
      },
    },
  },
  }
})
