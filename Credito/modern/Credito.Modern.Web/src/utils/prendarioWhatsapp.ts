/** Paridad del botón Enviar WhatsApp en Prendario/Gestionar.cshtml. */
export function urlWhatsAppPrendario(
  celular: string | null | undefined,
  nombreCliente: string,
  creditoId: number,
): string | null {
  const limpio = (celular ?? '').replace(/\D/g, '')
  if (limpio.length < 9) {
    return null
  }
  const e164 = limpio.length === 9 ? `51${limpio}` : limpio
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
