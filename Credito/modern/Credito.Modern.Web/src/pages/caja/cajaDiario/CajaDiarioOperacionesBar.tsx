import { useState } from 'react'
import { Button } from 'antd'
import {
  FilePdfOutlined,
  EnvironmentOutlined,
  UnorderedListOutlined,
  TeamOutlined,
} from '@ant-design/icons'
import { openCobroDiarioPdfInTab } from '../../../api/creditoPlanes'
import { toCobroDiarioQuery } from '../../../utils/gestorInformeForm'
import type { CajaSession } from './types'
import { MovimientosCajaModal } from './MovimientosCajaModal'
import { RutaCobranzaDrawer } from './RutaCobranzaDrawer'
import { useNavigate } from 'react-router-dom'

/** Botones globales del MVC (PDF, ruta QR, movimientos, cobro bloque). */
export function CajaDiarioOperacionesBar({
  ctx,
  usuarioId,
  variant = 'stack',
}: {
  ctx: CajaSession
  usuarioId: number
  variant?: 'stack' | 'inline'
}) {
  const navigate = useNavigate()
  const [movOpen, setMovOpen] = useState(false)
  const [rutaOpen, setRutaOpen] = useState(false)
  const [pdfLoading, setPdfLoading] = useState(false)

  const cobrosDiaPdf = async () => {
    if (usuarioId < 1) {
      return
    }
    setPdfLoading(true)
    try {
      await openCobroDiarioPdfInTab(
        toCobroDiarioQuery(ctx.oficinaId, usuarioId),
      )
    } finally {
      setPdfLoading(false)
    }
  }

  const cls =
    variant === 'stack'
      ? 'caja-diario-ops-stack'
      : 'caja-diario-ops-bar'

  return (
    <>
      <nav className={cls} aria-label="Operaciones de caja">
        <div className="caja-diario-ops-heading">
          <span className="caja-diario-side-section__eyebrow">
            Acciones rápidas
          </span>
          <span className="caja-diario-ops-heading__hint">
            Reportes, ruta y movimientos de la sesión.
          </span>
        </div>
        <Button
          block={variant === 'stack'}
          className="caja-diario-ops-report"
          icon={<FilePdfOutlined />}
          loading={pdfLoading}
          onClick={() => void cobrosDiaPdf()}
        >
          Cobros del día (PDF)
        </Button>
        <Button
          block={variant === 'stack'}
          className="caja-diario-ops-ruta"
          icon={<EnvironmentOutlined />}
          onClick={() => setRutaOpen(true)}
        >
          Armar ruta QR
        </Button>
        <Button
          block={variant === 'stack'}
          type="primary"
          icon={<TeamOutlined />}
          disabled={ctx.indCierre}
          onClick={() => navigate('/caja/cobro-bloque')}
        >
          Cobro en bloque
        </Button>
        <Button
          block={variant === 'stack'}
          icon={<UnorderedListOutlined />}
          onClick={() => setMovOpen(true)}
        >
          Movimientos
        </Button>
      </nav>

      <MovimientosCajaModal
        open={movOpen}
        oficinaId={ctx.oficinaId}
        cajaDiarioId={ctx.cajaDiarioId}
        onClose={() => setMovOpen(false)}
      />
      <RutaCobranzaDrawer
        open={rutaOpen}
        oficinaId={ctx.oficinaId}
        usuarioId={usuarioId}
        onClose={() => setRutaOpen(false)}
      />
    </>
  )
}
