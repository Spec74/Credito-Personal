import { Alert, Spin, Typography } from 'antd'
import { FileTextOutlined } from '@ant-design/icons'
import { ApiError } from '../../../api/errors'
import { formatMoney } from '../../../utils/formatMoney'
import { parseResumenBovedaTexto } from './bovedaResumenCuentaParse'
import { ResumenCuentaBrandLogo } from './resumenCuentaBrandIcons'

const { Text } = Typography

type Props = {
  texto?: string | null
  loading: boolean
  isError: boolean
  error?: unknown
}

export function BovedaResumenCuenta({ texto, loading, isError, error }: Props) {
  if (loading) {
    return (
      <div className="boveda-resumen-cuenta boveda-resumen-cuenta--loading">
        <Spin size="small" />
        <Text type="secondary">Cargando resumen de cuenta…</Text>
      </div>
    )
  }

  if (isError) {
    const msg = error instanceof ApiError ? error.message : 'No se pudo cargar el resumen.'
    return (
      <Alert type="warning" showIcon message="Resumen de cuenta" description={msg} />
    )
  }

  if (!texto?.trim()) {
    return (
      <div className="boveda-resumen-cuenta boveda-resumen-cuenta--empty">
        <FileTextOutlined className="boveda-resumen-cuenta__icon" aria-hidden />
        <Text type="secondary">Sin resumen disponible para esta bóveda.</Text>
      </div>
    )
  }

  const parsed = parseResumenBovedaTexto(texto)

  return (
    <div className="boveda-resumen-cuenta" role="region" aria-label="Resumen de cuenta">
      <div className="boveda-resumen-cuenta__header">
        <FileTextOutlined className="boveda-resumen-cuenta__icon" aria-hidden />
        <span className="boveda-resumen-cuenta__title">
          {parsed.titulo ?? 'Resumen de cuenta'}
        </span>
      </div>
      <div className="boveda-resumen-cuenta__body">
        {parsed.items.length > 0 ? (
          <div className="boveda-resumen-cuenta__grid">
            {parsed.items.map((item) => (
              <div
                key={item.clave}
                className={`boveda-resumen-cuenta__item boveda-resumen-cuenta__item--${item.variant}`}
              >
                <div className="boveda-resumen-cuenta__item-icon" title={item.etiqueta}>
                  <ResumenCuentaBrandLogo
                    variant={item.variant}
                    etiqueta={item.etiqueta}
                    className="boveda-resumen-cuenta__brand-svg"
                  />
                </div>
                <div className="boveda-resumen-cuenta__item-meta">
                  <span className="boveda-resumen-cuenta__item-label">{item.etiqueta}</span>
                  <span
                    className={
                      item.monto < 0
                        ? 'boveda-resumen-cuenta__item-monto boveda-resumen-cuenta__item-monto--neg'
                        : 'boveda-resumen-cuenta__item-monto'
                    }
                  >
                    S/ {formatMoney(item.monto)}
                  </span>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="boveda-resumen-cuenta__texto">{parsed.textoPlano ?? texto}</p>
        )}
      </div>
    </div>
  )
}
