/** Paridad del botón Enviar WhatsApp en Prendario/Gestionar.cshtml (chat libre, no plantilla). */
export function normalizarCelularPeru(celular: string | null | undefined): string | null {
  const limpio = (celular ?? '').replace(/\D/g, '')
  if (limpio.length === 9) {
    return `51${limpio}`
  }
  if (limpio.length === 11 && limpio.startsWith('51')) {
    return limpio
  }
  return limpio.length >= 9 ? limpio : null
}

export function celularPrendarioEsValido(celular: string | null | undefined): boolean {
  return normalizarCelularPeru(celular) !== null
}

export function urlWhatsAppPrendario(
  celular: string | null | undefined,
  nombreCliente: string,
  creditoId: number,
): string | null {
  const e164 = normalizarCelularPeru(celular)
  if (!e164) {
    return null
  }
  const mensaje =
    `Hola ${nombreCliente}, le recordamos su crédito prendario N° ${creditoId}. ` +
    'Le invitamos a acercarse a nuestra oficina para conocer el monto exacto a cancelar y evitar el remate de su bien.'
  return `https://wa.me/${e164}?text=${encodeURIComponent(mensaje)}`
}

export function abrirWhatsAppPrendario(
  celular: string | null | undefined,
  nombreCliente: string,
  creditoId: number,
): boolean {
  const url = urlWhatsAppPrendario(celular, nombreCliente, creditoId)
  if (!url) {
    return false
  }
  window.open(url, '_blank', 'noopener,noreferrer')
  return true
}
