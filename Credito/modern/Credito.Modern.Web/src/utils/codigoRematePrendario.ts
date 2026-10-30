/** Código que genera CREDITO.usp_RematarPrenda: PRM{prendaId}. */
export function esCodigoRematePrendario(codigo: string | null | undefined): boolean {
  return /^PRM\d+/i.test((codigo ?? '').trim())
}
