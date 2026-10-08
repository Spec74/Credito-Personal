import { Alert, Button, List, Space, Typography } from 'antd'
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
  const [qrOpen, setQrOpen] = useState(false)
  const paradas = result?.paradas ?? []
  const sinGps = paradas.filter((p) => !p.tieneGps).length
  const conGps = paradas.length - sinGps

  return (
    <>
      <CajaModal
        title="Ruta del cobrador"
        open={open}
        onCancel={onClose}
        width={920}
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
          Ruta ordenada para cobranza de morosos. El mapa muestra el recorrido; en el celular use
          <strong> Navegar</strong> para GPS turn-by-turn. WhatsApp/QR quedan solo para compartir.
        </Paragraph>

        {sinGps > 0 ? (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message={`${sinGps} cliente(s) sin GPS — aparecen al final de la lista. Complete ubicación en Clientes.`}
          />
        ) : null}

        <div className="ruta-cobrador-result">
          <div className="ruta-cobrador-result__map">
            {open && result ? (
              <RutaCobradorMap
                paradas={paradas}
                origen={result.origen}
                height={Math.min(480, typeof window !== 'undefined' ? window.innerHeight * 0.5 : 420)}
              />
            ) : null}
            <Text type="secondary" style={{ fontSize: 12 }}>
              {conGps} con GPS · mapa OpenStreetMap (gratis) · navegación con Google Maps en el
              celular
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
              renderItem={(p) => (
                <List.Item>
                  <List.Item.Meta
                    avatar={
                      <span className={`ruta-cobrador-pin${p.tieneGps ? '' : ' ruta-cobrador-pin--sin-gps'}`}>
                        {p.orden}
                      </span>
                    }
                    title={p.cliente}
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
              )}
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
