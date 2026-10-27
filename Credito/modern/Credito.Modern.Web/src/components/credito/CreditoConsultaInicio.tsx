import { Link } from 'react-router-dom'
import { Button } from 'antd'
import {
  AuditOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  GoldOutlined,
} from '@ant-design/icons'
import type { CreditoConsultaReciente } from '../../utils/creditoConsultaRecientes'

type Props = {
  recientes: CreditoConsultaReciente[]
  onAbrir: (item: CreditoConsultaReciente) => void
  onQuitar: (personaId: number) => void
}

export function CreditoConsultaInicio({ recientes, onAbrir, onQuitar }: Props) {
  return (
    <div className="credito-consulta-inicio" translate="no">
      <p className="credito-consulta-inicio__lead">
        El DNI o el nro. de crédito cargan solos. El nombre se elige de la lista. Borrar el campo
        deja la mesa limpia.
      </p>

      {recientes.length > 0 ? (
        <section className="credito-consulta-inicio__block" aria-labelledby="credito-consulta-recientes">
          <h2 id="credito-consulta-recientes">Recientes de esta sesión</h2>
          <ul className="credito-consulta-inicio__recientes">
            {recientes.map((item) => (
              <li key={item.personaId}>
                <button
                  type="button"
                  className="credito-consulta-inicio__reciente"
                  onClick={() => onAbrir(item)}
                >
                  <span className="credito-consulta-inicio__reciente-name">{item.label}</span>
                  {item.creditoId ? (
                    <span className="credito-consulta-inicio__reciente-meta">
                      Crédito {item.creditoId}
                    </span>
                  ) : null}
                </button>
                <Button
                  type="link"
                  size="small"
                  className="credito-consulta-inicio__quitar"
                  onClick={() => onQuitar(item.personaId)}
                >
                  Quitar
                </Button>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <nav className="credito-consulta-inicio__atajos" aria-label="Atajos de crédito">
        <Link className="credito-consulta-inicio__atajo" to="/credito/nuevo">
          <AuditOutlined aria-hidden />
          <span>Nueva solicitud</span>
        </Link>
        <Link className="credito-consulta-inicio__atajo" to="/credito/prendario/nuevo">
          <GoldOutlined aria-hidden />
          <span>Prendario</span>
        </Link>
        <Link className="credito-consulta-inicio__atajo" to="/credito/aprobar">
          <CheckCircleOutlined aria-hidden />
          <span>Aprobar</span>
        </Link>
        <Link className="credito-consulta-inicio__atajo" to="/credito/tareas">
          <ClockCircleOutlined aria-hidden />
          <span>Tareas</span>
        </Link>
      </nav>
    </div>
  )
}
