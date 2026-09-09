import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

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
    plugins: [
      react(),
      VitePWA({
        registerType: 'autoUpdate',
        includeAssets: ['favicon.ico', 'apple-touch-icon.png', 'mask-icon.svg'],
        manifest: {
          name: 'Crédito Moderno',
          short_name: 'CreditoMod',
          description: 'Sistema Modernizado de Gestión de Créditos',
          theme_color: '#1677ff', // Color azul por defecto de Ant Design
          background_color: '#ffffff',
          display: 'standalone',
          start_url: base, // Se sincroniza dinámicamente con tu /app/ o / configurado
          icons: [
            {
              src: 'pwa-192x192.png',
              sizes: '192x192',
              type: 'image/png'
            },
            {
              src: 'pwa-512x512.png',
              sizes: '512x512',
              type: 'image/png'
            },
            {
              src: 'pwa-512x512.png',
              sizes: '512x512',
              type: 'image/png',
              purpose: 'any maskable'
            }
          ]
        }
      })
    ],
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
        '/Reporte': {
          target: legacyMvcTarget,
          changeOrigin: true,
          headers: { Host: new URL(legacyMvcTarget).host },
        },
      },
    },
  }
})
