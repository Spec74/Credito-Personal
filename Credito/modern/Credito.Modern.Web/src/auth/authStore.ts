import { createContext } from 'react'
import type { LoginRequest, SessionInfo } from '../types/api'

export interface AuthContextValue {
  session: SessionInfo | null
  isLoading: boolean
  isAuthenticated: boolean
  login: (request: LoginRequest) => Promise<void>
  logout: () => void
  refreshSession: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
