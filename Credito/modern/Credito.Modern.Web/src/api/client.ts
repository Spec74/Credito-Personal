import {
  clearTokens,
  getAccessToken,
  getRefreshToken,
  saveTokens,
} from '../auth/tokenStorage'
import type { LoginTokenResponse, RefreshRequest } from '../types/api'
import { openReportVisorInTab, isPdfReportApiPath } from '../utils/reportVisor'
import { ApiError, parseApiError } from './errors'

const baseUrl = import.meta.env.VITE_API_BASE_URL as string

if (!baseUrl) {
  throw new Error('VITE_API_BASE_URL no está definida')
}

let refreshInFlight: Promise<boolean> | null = null

async function refreshAccessToken(): Promise<boolean> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) {
    return false
  }

  const body: RefreshRequest = { refreshToken }
  const res = await fetch(`${baseUrl}/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(body),
  })

  if (!res.ok) {
    clearTokens()
    return false
  }

  const data = (await res.json()) as LoginTokenResponse
  saveTokens(data.accessToken, data.refreshToken, data.expiresInSeconds)
  return true
}

async function ensureRefreshed(): Promise<boolean> {
  if (!refreshInFlight) {
    refreshInFlight = refreshAccessToken().finally(() => {
      refreshInFlight = null
    })
  }
  return refreshInFlight
}

export async function apiFetch<T>(
  path: string,
  init?: RequestInit,
  retried = false,
): Promise<T> {
  const headers = new Headers(init?.headers)
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json')
  }
  const token = getAccessToken()
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const res = await fetch(`${baseUrl}${path}`, { ...init, headers })

  const hadAuth = headers.has('Authorization')
  if (res.status === 401 && !retried && hadAuth && getRefreshToken()) {
    const ok = await ensureRefreshed()
    if (ok) {
      return apiFetch<T>(path, init, true)
    }
    clearTokens()
    throw new ApiError('Sesión expirada. Inicie sesión de nuevo.', 401)
  }

  if (!res.ok) {
    throw await parseApiError(res)
  }

  if (res.status === 204) {
    return undefined as T
  }

  const text = await res.text()
  if (!text) {
    return undefined as T
  }
  return JSON.parse(text) as T
}

export function getApiBaseUrl(): string {
  return baseUrl
}

export async function refreshSessionTokens(): Promise<boolean> {
  return ensureRefreshed()
}

async function authorizedFetch(
  path: string,
  init?: RequestInit,
  retried = false,
): Promise<Response> {
  const headers = new Headers(init?.headers)
  const token = getAccessToken()
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const res = await fetch(`${baseUrl}${path}`, { ...init, headers })
  const hadAuth = headers.has('Authorization')

  if (res.status === 401 && !retried && hadAuth && getRefreshToken()) {
    const ok = await ensureRefreshed()
    if (ok) {
      return authorizedFetch(path, init, true)
    }
    clearTokens()
    throw new ApiError('Sesión expirada. Inicie sesión de nuevo.', 401)
  }

  if (!res.ok) {
    throw await parseApiError(res)
  }

  return res
}

function assertBinaryPayload(fileName: string, buffer: ArrayBuffer): void {
  const lower = fileName.toLowerCase()
  const bytes = new Uint8Array(buffer)
  if (bytes.length < 4) {
    throw new ApiError('El archivo descargado está vacío o incompleto.', 502)
  }

  const isZip = bytes[0] === 0x50 && bytes[1] === 0x4b
  const looksText =
    bytes[0] === 0x3c ||
    (bytes[0] === 0xef && bytes[1] === 0xbb && bytes[2] === 0xbf && bytes[3] === 0x3c)

  if (lower.endsWith('.xlsx') && !isZip) {
    const hint = looksText
      ? ' La API devolvió HTML o JSON en lugar de Excel. Detenga Credito.Modern.Api, recompile (dotnet build) y vuelva a ejecutar la API antes de exportar.'
      : ' Reinicie Credito.Modern.Api con la última compilación.'
    throw new ApiError(`El archivo no es un Excel .xlsx válido.${hint}`, 502)
  }

  if (lower.endsWith('.xls') && !isZip && !looksText) {
    throw new ApiError('El archivo .xls no tiene formato reconocido.', 502)
  }
}

export async function apiDownload(path: string, fileName: string): Promise<void> {
  // PDFs de informe/ticket: visor SPA con rid opaco (sin blob: ni filtros en la URL).
  if (fileName.toLowerCase().endsWith('.pdf') || isPdfReportApiPath(path)) {
    openReportVisorInTab(path)
    return
  }

  await apiDownloadFile(path, fileName)
}

async function apiDownloadFile(path: string, fileName: string): Promise<void> {
  const res = await authorizedFetch(path, {
    method: 'GET',
    headers: {
      Accept:
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, text/csv, text/plain, application/octet-stream, */*',
    },
  })
  const buffer = await res.arrayBuffer()
  const downloadName =
    parseContentDispositionFileName(res.headers.get('content-disposition')) ?? fileName
  assertBinaryPayload(downloadName, buffer)
  const mime =
    res.headers.get('content-type')?.split(';')[0]?.trim() ||
    (fileName.toLowerCase().endsWith('.xlsx')
      ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
      : fileName.toLowerCase().endsWith('.csv')
        ? 'text/csv'
        : fileName.toLowerCase().endsWith('.txt')
          ? 'text/plain'
          : 'application/octet-stream')
  const blob = new Blob([buffer], { type: mime })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = downloadName
  a.click()
  URL.revokeObjectURL(url)
}

function parseContentDispositionFileName(header: string | null): string | null {
  if (!header) return null
  const star = /filename\*=UTF-8''([^;]+)/i.exec(header)
  if (star?.[1]) {
    try {
      return decodeURIComponent(star[1].trim())
    } catch {
      return star[1].trim()
    }
  }
  const plain = /filename="?([^";]+)"?/i.exec(header)
  return plain?.[1]?.trim() ?? null
}

function guessExportFileName(apiPath: string): string {
  const pathOnly = apiPath.split('?', 1)[0] ?? apiPath
  const leaf = pathOnly.split('/').pop() ?? 'informe'
  if (leaf.endsWith('-csv')) return `${leaf.slice(0, -4)}.csv`
  if (leaf.endsWith('-txt')) return `${leaf.slice(0, -4)}.txt`
  if (leaf.endsWith('-pdf')) return `${leaf.slice(0, -4)}.pdf`
  return leaf
}

/**
 * PDF → visor opaco en pestaña nueva.
 * CSV/TXT → descarga autenticada (sin URL con filtros en la barra).
 */
export async function apiOpenInTab(path: string): Promise<void> {
  if (isPdfReportApiPath(path)) {
    openReportVisorInTab(path)
    return
  }
  await apiDownloadFile(path, guessExportFileName(path))
}
