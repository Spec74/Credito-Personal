import type { AgendaEvento, ComunicadoRow } from '../../api/intranet'
import { formatFecha } from '../../utils/formatFecha'

export function destinoAviso(row: Pick<ComunicadoRow, 'alcance' | 'oficinaNombre' | 'oficinaId'>): string {
  if (row.alcance === 'RED') return 'Todas las sedes'
  return row.oficinaNombre?.trim() || (row.oficinaId ? `Oficina ${row.oficinaId}` : 'Esta sede')
}

export function vigenciaAviso(row: Pick<ComunicadoRow, 'permanente' | 'fechaFin'>): string {
  return row.permanente ? 'Siempre visible' : `Hasta ${formatFecha(row.fechaFin)}`
}

export function previewTexto(texto: string, max = 110): string {
  const t = texto.replace(/\s+/g, ' ').trim()
  if (t.length <= max) return t
  return `${t.slice(0, max).trim()}…`
}

export function recordatorioLabel(minutos: number | null | undefined): string | null {
  if (minutos == null) return null
  if (minutos === 15) return '15 min antes'
  if (minutos === 30) return '30 min antes'
  if (minutos === 60) return '1 hora antes'
  if (minutos === 1440) return '1 día antes'
  if (minutos % 1440 === 0) return `${minutos / 1440} días antes`
  if (minutos % 60 === 0) return `${minutos / 60} h antes`
  return `${minutos} min antes`
}

export function alcanceAgenda(e: Pick<AgendaEvento, 'alcance'>): string {
  return e.alcance === 'OFICINA' ? 'Sede' : 'Personal'
}

export function tamanoArchivo(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function tipoArchivo(contentType: string, nombre: string): string {
  if (contentType.startsWith('image/')) return 'Imagen'
  if (contentType === 'application/pdf' || nombre.toLowerCase().endsWith('.pdf')) return 'PDF'
  return 'Archivo'
}
