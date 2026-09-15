import { message } from 'antd'

/** Ejecuta apertura en pestaña (API con JWT o legacy) con feedback breve. */
export function runOpenReport(label: string, open: () => void | Promise<void>): void {
  try {
    const result = open()
    if (result instanceof Promise) {
      void result
        .then(() => message.success(`${label}: se abrió en una nueva pestaña`))
        .catch((e: unknown) =>
          message.error(e instanceof Error ? e.message : `No se pudo abrir ${label}`),
        )
      return
    }
    message.success(`${label}: se abrió en una nueva pestaña`)
  } catch (e: unknown) {
    message.error(e instanceof Error ? e.message : `No se pudo abrir ${label}`)
  }
}
