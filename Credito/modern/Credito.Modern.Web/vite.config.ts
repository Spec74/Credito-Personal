import { loadEnv } from 'vite'
import { defineConfig } from 'vitest/config'
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

  return {
    base,
    plugins: [
      react(),
      VitePWA({
        registerType: 'autoUpdate',
        includeAssets: ['favicon.ico', 'favicon-32.png', 'apple-touch-icon.png'],
        manifest: {
          name: 'CrediConfiable',
          short_name: 'CrediConfiable',
          description: 'Sistema de gestión de créditos — Inversiones CrediConfiable',
          lang: 'es',
          theme_color: '#1e4d7b',
          background_color: '#ffffff',
          display: 'standalone',
          scope: base,
          start_url: base,
          icons: [
            {
              src: 'pwa-192x192.png',
              sizes: '192x192',
              type: 'image/png',
              purpose: 'any',
            },
            {
              src: 'pwa-512x512.png',
              sizes: '512x512',
              type: 'image/png',
              purpose: 'any',
            },
            {
              src: 'pwa-512x512.png',
              sizes: '512x512',
              type: 'image/png',
              purpose: 'maskable',
            },
          ],
        },
      })
    ],
    build: {
      // Vite 8 / Rolldown: codeSplitting.groups (manualChunks está deprecado).
      // Rutas de app ya son lazy; aquí se paraleliza el vendor del design system.
      rolldownOptions: {
        output: {
          strictExecutionOrder: true,
          codeSplitting: {
            groups: [
              {
                name: 'vendor-antd-icons',
                test: /[\\/]node_modules[\\/]@ant-design[\\/]icons[\\/]/,
                priority: 50,
              },
              {
                name: 'vendor-dayjs',
                test: /[\\/]node_modules[\\/]dayjs[\\/]/,
                priority: 45,
              },
              {
                name: 'vendor-rc',
                test: /[\\/]node_modules[\\/](@rc-component|rc-[^\\/]+)[\\/]/,
                priority: 40,
              },
              {
                name: 'vendor-antd',
                test: /[\\/]node_modules[\\/]antd[\\/]/,
                priority: 35,
              },
              {
                name: 'vendor-query',
                test: /[\\/]node_modules[\\/]@tanstack[\\/]/,
                priority: 30,
              },
              {
                name: 'vendor-react',
                test: /[\\/]node_modules[\\/](react-dom|react-router|scheduler|react)[\\/]/,
                priority: 20,
              },
            ],
          },
        },
      },
      // Tras split: antd ~650 kB + rc ~700 kB (paralelos, ~200 kB gzip c/u). Aviso si crecen.
      chunkSizeWarningLimit: 750,
    },
    test: {
      environment: 'node',
      include: ['src/**/*.{test,spec}.{ts,tsx}'],
      coverage: {
        provider: 'v8',
        reporter: ['text', 'text-summary', 'lcov', 'json-summary'],
        reportsDirectory: './coverage',
        include: [
          'src/utils/**/*.{ts,tsx}',
          'src/hooks/**/*.{ts,tsx}',
          'src/auth/**/*.{ts,tsx}',
          'src/api/errors.ts',
          'src/config/**/*.{ts,tsx}',
        ],
        exclude: [
          'src/**/*.{test,spec}.{ts,tsx}',
          'src/**/*.d.ts',
          // UI/icon maps: bajo valor unitario; cubiertos por smoke E2E.
          'src/utils/menuIcons.tsx',
          'src/utils/hubLinkIcons.tsx',
          'src/utils/**/*Breadcrumbs.tsx',
        ],
        // Baseline actual ~24% lineas en utils/auth/hooks. El umbral evita regresion;
        // objetivo de madurez: >=50% (ampliar unit tests de utils sin cubrir paginas UI).
        thresholds: {
          lines: 20,
          functions: 15,
          statements: 20,
          branches: 20,
        },
      },
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
