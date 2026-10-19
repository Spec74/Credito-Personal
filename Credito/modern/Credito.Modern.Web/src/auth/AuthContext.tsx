import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { fetchMe, login as apiLogin } from '../api/auth'
import { refreshSessionTokens } from '../api/client'
import type { AuthMeResponse, LoginRequest, SessionInfo } from '../types/api'
import {
  clearTokens,
  getAccessExpiresAt,
  hasStoredSession,
  saveTokens,
} from './tokenStorage'
import { clearLoginProfile, getLoginProfile, saveLoginProfile } from './sessionProfile'
import {
  readRequiereCambioClave,
  writeRequiereCambioClave,
} from './passwordChangeGate'
import { AuthContext, type AuthContextValue } from './authStore'

function sessionFromMe(me: AuthMeResponse, fallbackUsuarioOficinaId = 0): SessionInfo {
  const nombreUsuario = me.nombreUsuario?.trim() || null
  const oficinaDenominacion = me.oficinaDenominacion?.trim() || null
  if (nombreUsuario || oficinaDenominacion) {
    const prev = getLoginProfile()
    saveLoginProfile(
      nombreUsuario || prev.nombreUsuario || '',
      oficinaDenominacion || prev.oficinaLabel || '',
    )
  }
  return {
    usuarioId: me.usuarioId,
    oficinaId: me.oficinaId,
    usuarioOficinaId: me.usuarioOficinaId ?? fallbackUsuarioOficinaId,
    roles: me.roles ?? [],
    nombreUsuario,
    oficinaDenominacion,
  }
}

async function loadSession(): Promise<SessionInfo | null> {
  if (!hasStoredSession()) {
    return null
  }
  try {
    // Refresh alinea claims de rol del access JWT con MAESTRO.UsuarioRol.
    await refreshSessionTokens()
    const me = await fetchMe()
    return sessionFromMe(me)
  } catch {
    clearTokens()
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<SessionInfo | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [requiereCambioClave, setRequiereCambioClave] = useState(() =>
    readRequiereCambioClave(),
  )

  const refreshSession = useCallback(async () => {
    const next = await loadSession()
    setSession(next)
    if (!next) {
      writeRequiereCambioClave(false)
      setRequiereCambioClave(false)
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      const next = await loadSession()
      if (!cancelled) {
        setSession(next)
        if (!next) {
          writeRequiereCambioClave(false)
          setRequiereCambioClave(false)
        } else {
          setRequiereCambioClave(readRequiereCambioClave())
        }
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
    const forceChange = tokens.requiereCambioClave === true
    writeRequiereCambioClave(forceChange)
    setRequiereCambioClave(forceChange)
    setSession({
      usuarioId: tokens.usuarioId,
      oficinaId: tokens.oficinaId,
      usuarioOficinaId: tokens.usuarioOficinaId,
      roles: [],
    })
    const me = await fetchMe()
    setSession(sessionFromMe(me, tokens.usuarioOficinaId))
  }, [])

  const clearRequiereCambioClave = useCallback(() => {
    writeRequiereCambioClave(false)
    setRequiereCambioClave(false)
  }, [])

  const logout = useCallback(() => {
    clearTokens()
    clearLoginProfile()
    writeRequiereCambioClave(false)
    setRequiereCambioClave(false)
    setSession(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      isLoading,
      isAuthenticated: session !== null,
      requiereCambioClave,
      clearRequiereCambioClave,
      login,
      logout,
      refreshSession,
    }),
    [
      session,
      isLoading,
      requiereCambioClave,
      clearRequiereCambioClave,
      login,
      logout,
      refreshSession,
    ],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
