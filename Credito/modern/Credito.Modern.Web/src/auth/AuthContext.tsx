import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { fetchMe, login as apiLogin } from '../api/auth'
import { refreshSessionTokens } from '../api/client'
import type { LoginRequest, SessionInfo } from '../types/api'
import {
  clearTokens,
  getAccessExpiresAt,
  hasStoredSession,
  saveTokens,
} from './tokenStorage'
import { clearLoginProfile } from './sessionProfile'
import { AuthContext, type AuthContextValue } from './authStore'

async function loadSession(): Promise<SessionInfo | null> {
  if (!hasStoredSession()) {
    return null
  }
  try {
    const me = await fetchMe()
    return {
      usuarioId: me.usuarioId,
      oficinaId: me.oficinaId,
      usuarioOficinaId: me.usuarioOficinaId ?? 0,
      roles: me.roles ?? [],
    }
  } catch {
    clearTokens()
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<SessionInfo | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const refreshSession = useCallback(async () => {
    const next = await loadSession()
    setSession(next)
  }, [])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      const next = await loadSession()
      if (!cancelled) {
        setSession(next)
        setIsLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!session) {
      return
    }
    const expiresAt = getAccessExpiresAt()
    if (!expiresAt) {
      return
    }
    const msUntilRefresh = expiresAt - Date.now() - 60_000
    const runRefresh = async () => {
      await refreshSessionTokens()
      await refreshSession()
    }
    if (msUntilRefresh <= 0) {
      void runRefresh()
      return
    }
    const timer = window.setTimeout(() => {
      void runRefresh()
    }, msUntilRefresh)
    return () => window.clearTimeout(timer)
  }, [session, refreshSession])

  const login = useCallback(async (request: LoginRequest) => {
    const tokens = await apiLogin(request)
    saveTokens(
      tokens.accessToken,
      tokens.refreshToken,
      tokens.expiresInSeconds,
      request.recordarSesion === true,
    )
    setSession({
      usuarioId: tokens.usuarioId,
      oficinaId: tokens.oficinaId,
      usuarioOficinaId: tokens.usuarioOficinaId,
      roles: [],
    })
    const me = await fetchMe()
    setSession({
      usuarioId: me.usuarioId,
      oficinaId: me.oficinaId,
      usuarioOficinaId: me.usuarioOficinaId ?? tokens.usuarioOficinaId,
      roles: me.roles ?? [],
    })
  }, [])

  const logout = useCallback(() => {
    clearTokens()
    clearLoginProfile()
    setSession(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      isLoading,
      isAuthenticated: session !== null,
      login,
      logout,
      refreshSession,
    }),
    [session, isLoading, login, logout, refreshSession],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
