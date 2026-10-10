import { Alert, Button, Grid, List, Space, Typography } from 'antd'
import {
  EnvironmentOutlined,
  LinkOutlined,
  QrcodeOutlined,
  CompassOutlined,
} from '@ant-design/icons'
import { CajaModal } from '../../../components/caja/CajaModal'
import { RutaCobradorMap, type RutaMapParada } from '../../../components/maps/RutaCobradorMap'
import { formatMoney } from '../../../utils/formatMoney'
import { RutaQrModal } from './RutaQrModal'
import { useState } from 'react'

const { Text, Paragraph } = Typography

function googleMapsPointUrl(lat: number, lng: number): string {
  return `https://www.google.com/maps?q=${lat},${lng}`
}

export type RutaCobradorResult = {
  paradas: RutaMapParada[]
  origen: { lat: number; lng: number } | null
  urlNavegacionGoogle: string | null
  urlWhatsApp: string | null
}

export function RutaCobradorResultModal({
  open,
  result,
  onClose,
}: {
  open: boolean
  result: RutaCobradorResult | null
  onClose: () => void
}) {
  const screens = Grid.useBreakpoint()
  const isMobile = screens.md !== true
  const [qrOpen, setQrOpen] = useState(false)
  const paradas = result?.paradas ?? []
  const sinGps = paradas.filter((p) => !p.tieneGps).length
  const conGps = paradas.length - sinGps
  const mapHeight: number | string = isMobile
    ? 'min(42dvh, 360px)'
    : Math.min(480, typeof window !== 'undefined' ? window.innerHeight * 0.5 : 420)

  return (
    <>
      <CajaModal
        title="Ruta del cobrador"
        open={open}
        onCancel={onClose}
        width={isMobile ? '100%' : 920}
        centered={!isMobile}
        rootClassName={isMobile ? 'ruta-cobrador-result-modal-root' : undefined}
        className={isMobile ? 'ruta-cobrador-result-modal' : undefined}
        footer={
          <Space wrap>
            {result?.urlNavegacionGoogle ? (
              <Button
                type="primary"
                icon={<CompassOutlined />}
                href={result.urlNavegacionGoogle}
                target="_blank"
                rel="noopener noreferrer"
              >
                Navegar en Google Maps
              </Button>
            ) : null}
            {result?.urlWhatsApp ? (
              <Button
                icon={<LinkOutlined />}
                href={result.urlWhatsApp}
                target="_blank"
                rel="noopener noreferrer"
              >
                Compartir por WhatsApp
              </Button>
            ) : null}
            {result?.urlWhatsApp ? (
              <Button icon={<QrcodeOutlined />} onClick={() => setQrOpen(true)}>
                QR (opcional)
              </Button>
            ) : null}
            <Button onClick={onClose}>Cerrar</Button>
          </Space>
        }
      >
        <Paragraph type="secondary" style={{ marginTop: 0 }}>
          Secuencia de cobranza ordenada. Revise el mapa y la lista de paradas; use{' '}
          <strong>Navegar en Google Maps</strong> para indicaciones en ruta. WhatsApp y QR son opcionales
          para compartir.
        </Paragraph>

        {sinGps > 0 ? (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message={`${sinGps} cliente(s) sin coordenadas GPS (al final de la lista). Registre ubicación en la ficha del cliente.`}
          />
        ) : null}

        <div className="ruta-cobrador-result">
          <div className="ruta-cobrador-result__map">
            {open && result ? (
              <RutaCobradorMap
                paradas={paradas}
                origen={result.origen}
                height={mapHeight}
              />
            ) : null}
            <Text type="secondary" style={{ fontSize: 12 }}>
              {conGps} parada(s) con GPS. Indicaciones turn-by-turn disponibles con Google Maps.
            </Text>
          </div>
          <div className="ruta-cobrador-result__list">
            <Text strong>
              <EnvironmentOutlined /> Paradas ({paradas.length})
            </Text>
            <List
              size="small"
              dataSource={paradas}
              style={{ marginTop: 8, maxHeight: 420, overflow: 'auto' }}
              renderItem={(p) => {
                const lat = p.latitud != null ? Number(p.latitud) : NaN
                const lng = p.longitud != null ? Number(p.longitud) : NaN
                const mapUrl =
                  p.tieneGps && Number.isFinite(lat) && Number.isFinite(lng) && lat !== 0 && lng !== 0
                    ? googleMapsPointUrl(lat, lng)
                    : null
                return (
                  <List.Item>
                    <List.Item.Meta
                      avatar={
                        <span
                          className={`ruta-cobrador-pin${p.tieneGps ? '' : ' ruta-cobrador-pin--sin-gps'}`}
                        >
                          {p.orden}
                        </span>
                      }
                      title={
                        <Space size={8} wrap>
                          <span>{p.cliente}</span>
                          {mapUrl ? (
                            <Button
                              type="link"
                              size="small"
                              href={mapUrl}
                              target="_blank"
                              rel="noopener noreferrer"
                              style={{ padding: 0, height: 'auto', fontSize: 12 }}
                            >
                              Mapa
                            </Button>
                          ) : null}
                        </Space>
                      }
                      description={
                        <>
                          Cobrar: <strong>{formatMoney(p.montoCobrar)}</strong>
                          {!p.tieneGps ? ' · Sin GPS' : null}
                          {p.direccion ? (
                            <>
                              <br />
                              <span style={{ fontSize: 12 }}>{p.direccion}</span>
                            </>
                          ) : null}
                        </>
                      }
                    />
                  </List.Item>
                )
              }}
            />
          </div>
        </div>
      </CajaModal>

      <RutaQrModal
        open={qrOpen}
        url={result?.urlWhatsApp ?? null}
        onClose={() => setQrOpen(false)}
      />
    </>
  )
}
