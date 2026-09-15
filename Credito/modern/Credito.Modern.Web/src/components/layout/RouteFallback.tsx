import { Spin } from 'antd'

/** Carga de rutas lazy (solo el contenido central, el shell ya está visible). */
export function RouteFallback() {
  return (
    <div
      className="credix-route-fallback"
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: 240,
        padding: 48,
      }}
      aria-live="polite"
      aria-busy
    >
      <Spin size="large" tip="Cargando pantalla…" />
    </div>
  )
}
