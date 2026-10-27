import { Button, Popover, Typography } from 'antd'
import { UserOutlined } from '@ant-design/icons'
import { branding } from '../../config/branding'

const { Text } = Typography

/** Sesión de sede: oficina visible; nombre y roles en el panel. No es el toolbar MVC. */
export function CredixSessionControl({
  usuarioNombre,
  oficinaNombre,
  roles,
}: {
  usuarioNombre: string
  oficinaNombre: string | null
  roles: string[]
}) {
  const oficina = oficinaNombre?.trim() || 'Sin oficina'
  const usuario = usuarioNombre.trim() || 'Sesión'

  const panel = (
    <div className="credix-session__panel" translate="no">
      <p className="credix-session__row">
        <span className="credix-session__k">Usuario</span>
        <Text strong>{usuario}</Text>
      </p>
      <p className="credix-session__row">
        <span className="credix-session__k">Oficina</span>
        <Text>{oficina}</Text>
      </p>
      {roles.length > 0 ? (
        <p className="credix-session__row">
          <span className="credix-session__k">Rol</span>
          <Text>{roles.join(' · ')}</Text>
        </p>
      ) : null}
      <p className="credix-session__hint">
        Soporte{' '}
        <a href={`tel:${branding.supportPhone}`} translate="no">
          {branding.supportPhone}
        </a>
        . Salir para cambiar de sede.
      </p>
    </div>
  )

  return (
    <Popover
      trigger="click"
      placement="bottomLeft"
      content={panel}
      overlayClassName="credix-session__overlay"
    >
      <Button
        type="text"
        className="credix-session"
        aria-haspopup="dialog"
        aria-label={`Sesión: ${usuario}, ${oficina}`}
      >
        <UserOutlined aria-hidden />
        <span className="credix-session__office">{oficina}</span>
      </Button>
    </Popover>
  )
}
