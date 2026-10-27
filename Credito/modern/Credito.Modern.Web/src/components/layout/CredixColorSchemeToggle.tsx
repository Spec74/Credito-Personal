import { Button } from 'antd'
import { MoonOutlined, SunOutlined } from '@ant-design/icons'
import { useCredixColorScheme } from '../../theme/CredixColorSchemeProvider'

export function CredixColorSchemeToggle({ compact = false }: { compact?: boolean }) {
  const { resolved, toggleResolved } = useCredixColorScheme()
  const dark = resolved === 'dark'
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
