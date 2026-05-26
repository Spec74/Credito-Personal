import { Button, Space, Typography } from 'antd'
import { LinkOutlined, QrcodeOutlined } from '@ant-design/icons'
import { CajaModal } from '../../../components/caja/CajaModal'

const { Paragraph, Text } = Typography

export function RutaQrModal({
  open,
  url,
  onClose,
}: {
  open: boolean
  url: string | null
  onClose: () => void
}) {
  const qrSrc =
    url != null
      ? `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(url)}`
      : null

  return (
    <CajaModal
      title="Ruta de cobranza — QR"
      open={open}
      onCancel={onClose}
      width={420}
      footer={
        <Space>
          {url ? (
            <Button
              type="primary"
              icon={<LinkOutlined />}
              href={url}
              target="_blank"
              rel="noopener noreferrer"
            >
              Abrir en celular
            </Button>
          ) : null}
          <Button onClick={onClose}>Cerrar</Button>
        </Space>
      }
    >
      <div className="caja-ruta-qr-body">
        <QrcodeOutlined className="caja-ruta-qr-icon" aria-hidden />
        <Paragraph type="secondary" className="caja-ruta-qr-hint">
          Escanee el código con el celular o abra el enlace para iniciar la ruta GPS de cobranza
          (paridad dlgRutaQR del MVC).
        </Paragraph>
        {qrSrc ? (
          <img
            src={qrSrc}
            alt="Código QR de la ruta de cobranza"
            width={220}
            height={220}
            className="caja-ruta-qr-image"
          />
        ) : null}
        {url ? (
          <Text copyable className="caja-ruta-qr-url">
            {url}
          </Text>
        ) : null}
      </div>
    </CajaModal>
  )
}
