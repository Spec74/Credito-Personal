import { apiFetch } from './client'
import type {
  AuthMeResponse,
  LoginRequest,
  LoginTokenResponse,
} from '../types/api'

export function login(request: LoginRequest): Promise<LoginTokenResponse> {
  const { recordarSesion: _recordar, ...body } = request
  return apiFetch<LoginTokenResponse>('/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchMe(): Promise<AuthMeResponse> {
  return apiFetch<AuthMeResponse>('/auth/me')
}
