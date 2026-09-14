import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Alert, Typography, message } from 'antd'
import { useAuth } from '../../auth/useAuth'
import { getLoginProfile } from '../../auth/sessionProfile'
import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { esLecturaSaldoCaja } from '../../utils/cajaSaldosPermisos'
import { AsignarCajaForm } from './components/asignarcajaform'

const { Paragraph } = Typography

export function AsignarCajaPage() {
  const navigate = useNavigate()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const lectura = esLecturaSaldoCaja(session?.roles ?? [])
  const oficinaLabel = getLoginProfile().oficinaLabel ?? `Oficina #${oficinaId}`

  const stats = useMemo((): CredixStatItem[] => {
    if (lectura) {
      return []
    }
    return [{ value: oficinaLabel, label: 'Oficina sesión' }]
  }, [lectura, oficinaLabel])

  return (
    <CredixPage
      title="Asignar caja"
      subtitle="Abre una caja cerrada con el cajero configurado en el maestro. El saldo inicial sale de la bóveda."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: <Link to="/caja/saldos">Saldos caja</Link> },
        { title: 'Asignar caja' },
      ]}
    >
      {lectura ? (
        <Alert
          type="warning"
          showIcon
          message="Su perfil es LECTURA_SALDO: no puede asignar cajas."
          description={
            <Link to="/caja/saldos">Volver a Saldos caja</Link>
          }
        />
      ) : (
        <CredixPanel title="Datos de apertura">
          <AsignarCajaForm
            oficinaId={oficinaId}
            submitLabel="Asignar y abrir caja"
            onCancel={() => navigate('/caja/saldos')}
            onSuccess={(r) => {
              message.success(
                r.esCajaChica
                  ? 'Caja chica asignada.'
                  : `Caja diario abierta${r.cajaDiarioId ? ` (ID ${r.cajaDiarioId})` : ''}.`,
              )
              navigate('/caja/saldos')
            }}
          />
        </CredixPanel>
      )}

      <Paragraph type="secondary" style={{ marginTop: 16 }}>
        Tras asignar permanece en{' '}
        <Link to="/caja/saldos">Saldos caja</Link> (paridad MVC). La caja diario
        del cajero se abre en{' '}
        <Link to="/caja/diario">Caja diario</Link>.
      </Paragraph>
    </CredixPage>
  )
}
