const collator = new Intl.Collator('es', { numeric: true, sensitivity: 'base' })

/** Orden de texto con acentos y números ("Caja 2" antes de "Caja 10"). */
export function compararTexto(a: string | null | undefined, b: string | null | undefined): number {
  return collator.compare(a ?? '', b ?? '')
}

/** Orden numérico tratando null/undefined como 0 (los importes vacíos son ceros). */
export function compararMonto(a: number | null | undefined, b: number | null | undefined): number {
  return (a ?? 0) - (b ?? 0)
}
