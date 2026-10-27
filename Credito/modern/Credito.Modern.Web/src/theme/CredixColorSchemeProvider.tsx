import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { ConfigProvider } from 'antd'
import esES from 'antd/locale/es_ES'
import { credixFormValidateMessages } from './credixFormValidateMessages'
import { credixLegacyTheme } from './credixLegacyTheme'
import { credixDarkTheme } from './credixDarkTheme'
import {
  applyCredixColorScheme,
  persistColorSchemePreference,
  prefersDarkColorScheme,
  readColorSchemePreference,
  resolveCredixColorScheme,
  type CredixColorSchemePreference,
  type CredixResolvedColorScheme,
} from './credixColorScheme'

interface ColorSchemeContextValue {
  preference: CredixColorSchemePreference
  resolved: CredixResolvedColorScheme
  setPreference: (preference: CredixColorSchemePreference) => void
  toggleResolved: () => void
}

const ColorSchemeContext = createContext<ColorSchemeContextValue | null>(null)

function initialPreference(): CredixColorSchemePreference {
  return typeof window === 'undefined'
    ? 'system'
    : readColorSchemePreference(window.localStorage)
}

export function CredixColorSchemeProvider({ children }: { children: ReactNode }) {
  const [preference, setPreferenceState] = useState<CredixColorSchemePreference>(initialPreference)
  const [prefersDark, setPrefersDark] = useState(() => prefersDarkColorScheme())

  const resolved = useMemo(
    () => resolveCredixColorScheme({ preference, prefersDark }),
    [preference, prefersDark],
  )

  useEffect(() => {
    applyCredixColorScheme(resolved)
  }, [resolved])

  useEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)')
    const onChange = () => setPrefersDark(media.matches)
    media.addEventListener('change', onChange)
    return () => media.removeEventListener('change', onChange)
  }, [])

  const setPreference = useCallback((next: CredixColorSchemePreference) => {
    setPreferenceState(next)
    persistColorSchemePreference(next, window.localStorage)
  }, [])

  const toggleResolved = useCallback(() => {
    setPreference(resolved === 'dark' ? 'light' : 'dark')
  }, [resolved, setPreference])

  const value = useMemo(
    () => ({ preference, resolved, setPreference, toggleResolved }),
    [preference, resolved, setPreference, toggleResolved],
  )

  return (
    <ColorSchemeContext.Provider value={value}>
      <ConfigProvider
        locale={esES}
        theme={resolved === 'dark' ? credixDarkTheme : credixLegacyTheme}
        form={{
          validateMessages: credixFormValidateMessages,
          requiredMark: true,
        }}
        input={{ autoComplete: 'off' }}
        modal={{
          centered: true,
        }}
        getPopupContainer={() => document.body}
      >
        {children}
      </ConfigProvider>
    </ColorSchemeContext.Provider>
  )
}

export function useCredixColorScheme(): ColorSchemeContextValue {
  const ctx = useContext(ColorSchemeContext)
  if (!ctx) {
    throw new Error('useCredixColorScheme requiere CredixColorSchemeProvider')
  }
  return ctx
}
