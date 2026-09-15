import { ApiError } from '../api/errors'

export function getAuthLoginErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const detail = error.problem?.detail?.trim()
    if (detail) {
      return detail
    }
    return error.message
  }
  if (error instanceof Error) {
    return error.message
  }
  return 'No se pudo iniciar sesión.'
}
