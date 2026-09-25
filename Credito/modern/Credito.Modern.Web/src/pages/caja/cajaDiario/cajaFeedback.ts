import { message } from 'antd'

/** Éxitos de negocio (pago, cierre, anulación): breves y con clave estable. */
const SUCCESS_MS = 2.4
/** Avisos contextuales (varios créditos, selección): no apilar. */
const INFO_MS = 2

/**
 * Feedback de Caja Diario: claves estables + duración corta para no apilar
 * toasts rutinarios. La carga de plan / descargas no deben usar success.
 */
export function cajaToastSuccess(content: string, key = 'caja-ok') {
  void message.open({
    type: 'success',
    content,
    key,
    duration: SUCCESS_MS,
  })
}

export function cajaToastInfo(content: string, key = 'caja-info') {
  void message.open({
    type: 'info',
    content,
    key,
    duration: INFO_MS,
  })
}

export function cajaToastWarning(content: string, key = 'caja-warn') {
  void message.open({
    type: 'warning',
    content,
    key,
    duration: 3.2,
  })
}

export function cajaToastError(content: string, key = 'caja-err') {
  void message.open({
    type: 'error',
    content,
    key,
    duration: 4.5,
  })
}
