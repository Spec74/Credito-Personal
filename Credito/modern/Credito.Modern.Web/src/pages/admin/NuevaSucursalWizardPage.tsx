import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button, Checkbox, Steps } from 'antd'
import { fetchOficinasGestion } from '../../api/maestrosCrud'
import { CredixPage, CredixPanel } from '../../components/credix'
import { OficinaFormModal } from './OficinaFormModal'
import { WIZARD_SUCURSAL_PASOS } from '../../utils/sucursalRed'

const DONE_KEY = 'credix.wizard.sucursal.done'

function readDone(): string[] {
  try {
    const raw = localStorage.getItem(DONE_KEY)
    const parsed = raw ? (JSON.parse(raw) as string[]) : []
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}

function writeDone(ids: string[]) {
  try {
    localStorage.setItem(DONE_KEY, JSON.stringify(ids))
  } catch {
    /* ignore */
  }
}

export function NuevaSucursalWizardPage() {
  const [done, setDone] = useState<string[]>(readDone)
  const [modalOpen, setModalOpen] = useState(false)
  const [step, setStep] = useState(0)

  const oficinas = useQuery({
    queryKey: ['oficinas-gestion', true, ''],
    queryFn: () => fetchOficinasGestion({ incluirInactivos: true, buscar: '' }),
  })

  const sucursales = useMemo(
    () => (oficinas.data ?? []).filter((o) => !o.indPrincipal && o.estado),
    [oficinas.data],
  )

  const toggle = (id: string, checked: boolean) => {
    const next = checked ? [...new Set([...done, id])] : done.filter((x) => x !== id)
    setDone(next)
    writeDone(next)
  }

  const current = WIZARD_SUCURSAL_PASOS[step]

  return (
    <CredixPage
      title="Nueva sucursal"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/mantenimiento/oficinas">Oficinas</Link> },
        { title: 'Nueva sucursal' },
      ]}
      actions={
        <Button type="primary" onClick={() => setModalOpen(true)}>
          Crear oficina
        </Button>
      }
    >
      <CredixPanel title="Lista de sucursales">
        {sucursales.length === 0 ? (
          <p style={{ margin: 0 }}>Aún no hay sucursales activas. Empiece por crear la oficina.</p>
        ) : (
          <ul className="credix-wizard-list">
            {sucursales.map((o) => (
              <li key={o.oficinaId}>
                {o.denominacion} <span>#{o.oficinaId}</span>
              </li>
            ))}
          </ul>
        )}
      </CredixPanel>

      <Steps
        current={step}
        onChange={setStep}
        size="small"
        items={WIZARD_SUCURSAL_PASOS.map((p) => ({
          title: p.title,
          status: done.includes(p.id) ? 'finish' : undefined,
        }))}
      />

      {current ? (
        <CredixPanel title={current.title}>
          <Checkbox
            checked={done.includes(current.id)}
            onChange={(e) => toggle(current.id, e.target.checked)}
          >
            Hecho
          </Checkbox>
          <div style={{ marginTop: 12 }}>
            {current.id === 'oficina' ? (
              <Button type="primary" onClick={() => setModalOpen(true)}>
                Abrir alta de oficina
              </Button>
            ) : (
              <Link to={current.to}>
                <Button type="primary">Ir</Button>
              </Link>
            )}
          </div>
        </CredixPanel>
      ) : null}

      <OficinaFormModal
        open={modalOpen}
        editing={null}
        onClose={() => setModalOpen(false)}
        onSaved={() => {
          void oficinas.refetch()
          toggle('oficina', true)
        }}
      />
    </CredixPage>
  )
}
