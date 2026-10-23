/** Acepta `{ id }` o `{ Id }` para no tratar un aviso ya insertado como fallo. */
export function idDeRespuesta(r: { id?: number; Id?: number } | null | undefined): number {
  const n = Number(r?.id ?? r?.Id ?? 0)
  return Number.isFinite(n) && n > 0 ? Math.trunc(n) : 0
}

export function archivosDeUpload(
  list?: { originFileObj?: File | Blob }[] | null,
): File[] {
  const out: File[] = []
  for (const item of list ?? []) {
    const f = item.originFileObj
    if (f instanceof File) out.push(f)
    else if (f instanceof Blob) out.push(new File([f], 'adjunto', { type: f.type }))
  }
  return out
}
