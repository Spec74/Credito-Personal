/** Icono de menú legacy: Font Awesome vía `span.icon` + clase en BD (p. ej. icon-list). */
export function LegacyMenuIcon({ icono }: { icono: string | null | undefined }) {
  const raw = icono?.trim()
  if (!raw) {
    return <span className="icon icon-file" aria-hidden />
  }
  const cls = raw.startsWith('icon-') ? raw : raw.replace(/^icon\s+/, '')
  return <span className={`icon ${cls}`} aria-hidden />
}
