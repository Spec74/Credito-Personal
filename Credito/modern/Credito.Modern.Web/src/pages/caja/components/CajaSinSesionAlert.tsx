import { useState } from 'react'
import { Alert, Button, Space, Typography } from 'antd'
import { useAuth } from '../../../auth/useAuth'
import { getLoginProfile } from '../../../auth/sessionProfile'
import { puedeAsignarCajaUi } from '../../../utils/cajaSaldosPermisos'
import { AsignarCajaModal } from './asignarcajamodal'
import '../../../styles/caja-sin-sesion-alert.css'

const { Text } = Typography

type Variant = 'diario' | 'chica'

type Props = {
  variant: Variant
  onAssigned?: () => void
  onRefresh?: () => void
  refreshing?: boolean
}

/**
 * Estado sin caja abierta: admin/encargado abre asignación en modal;
 * cajero u otros ven instrucciones sin enlaces a rutas bloqueadas (Sin permiso).
 */
export function CajaSinSesionAlert({
  variant,
  onAssigned,
  onRefresh,
  refreshing,
}: Props) {
  const { session } = useAuth()
  const [asignarOpen, setAsignarOpen] = useState(false)
  const roles = session?.roles ?? []
  const puedeAsignar = puedeAsignarCajaUi(roles)
  const oficinaId = session?.oficinaId ?? 0
  const loginProfile = getLoginProfile()
  const usuarioLabel =
    loginProfile.nombreUsuario ?? `ID ${session?.usuarioId ?? '?'}`

  const isChica = variant === 'chica'
  const message = isChica ? 'No hay caja chica abierta' : 'Sin caja diario abierta'
  const description = puedeAsignar ? (
    isChica ? (
      <>Asigne la caja «CAJA CHICA» para operar gastos, rendición y arqueo.</>
    ) : (
      <>
        El usuario <Text strong>{usuarioLabel}</Text> no tiene caja asignada y
        abierta. Asigne una caja para continuar la operación del día.
      </>
    )
  ) : isChica ? (
    <>
      No hay caja chica abierta para su usuario. Solicite a un administrador o
      encargado que asigne la caja «CAJA CHICA».
    </>
  ) : (
    <>
      El usuario <Text strong>{usuarioLabel}</Text> no tiene caja asignada y
      abierta. Solicite a un administrador o encargado que le asigne caja.
    </>
  )

  return (
    <>
      <Alert
        className="caja-sin-sesion-alert"
        type="warning"
        showIcon
        message={message}
        description={description}
        action={
          <Space wrap className="caja-sin-sesion-alert__actions">
            {puedeAsignar ? (
              <Button type="primary" size="small" onClick={() => setAsignarOpen(true)}>
                Asignar caja
              </Button>
            ) : null}
            {onRefresh ? (
              <Button size="small" loading={refreshing} onClick={onRefresh}>
                Actualizar
              </Button>
            ) : null}
          </Space>
        }
      />
      {puedeAsignar && oficinaId > 0 ? (
        <AsignarCajaModal
          open={asignarOpen}
          oficinaId={oficinaId}
          onClose={() => setAsignarOpen(false)}
          onSuccess={() => {
            setAsignarOpen(false)
            onAssigned?.()
          }}
        />
      ) : null}
    </>
  )
}
