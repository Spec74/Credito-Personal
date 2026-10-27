import { MoonOutlined, SunOutlined } from '@ant-design/icons'
import { Button, Segmented } from 'antd'
import { useCredixColorScheme } from '../../theme/CredixColorSchemeProvider'

interface CredixColorSchemeToggleProps {
  compact?: boolean
  /** Control de escritorio: Claro | Oscuro, no el icono de teléfono. */
  variant?: 'button' | 'segmented'
}

export function CredixColorSchemeToggle({
  compact = false,
  variant = 'button',
}: CredixColorSchemeToggleProps) {
  const { resolved, setPreference, toggleResolved } = useCredixColorScheme()
  const dark = resolved === 'dark'

  if (variant === 'segmented') {
    return (
      <Segmented<'light' | 'dark'>
        className="credix-theme-segmented"
        size="middle"
        value={resolved}
        aria-label="Apariencia"
        onChange={(value) => setPreference(value)}
        options={[
          { label: 'Claro', value: 'light', icon: <SunOutlined /> },
          { label: 'Oscuro', value: 'dark', icon: <MoonOutlined /> },
        ]}
      />
    )
  }

  const label = dark ? 'Activar modo claro' : 'Activar modo oscuro'

  return (
    <Button
      type="text"
      className="credix-theme-toggle"
      icon={dark ? <SunOutlined /> : <MoonOutlined />}
      aria-label={label}
      title={label}
      aria-pressed={dark}
      onClick={toggleResolved}
    >
      {compact ? null : dark ? 'Claro' : 'Oscuro'}
    </Button>
  )
}
