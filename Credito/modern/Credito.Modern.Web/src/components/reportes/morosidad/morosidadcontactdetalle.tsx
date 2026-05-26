import { EnvironmentOutlined, PhoneOutlined, UserOutlined } from '@ant-design/icons'
import { Space, Typography } from 'antd'
import type { RptCreditoMorosidadRow } from '../../../types/api'
import { formatInformeFecha } from '../../../utils/formatFecha'

/** Subfila de contacto — paridad segunda línea del PDF <c>rptCreditoMorosidad.rdlc</c>. */
export function MorosidadContactDetalle({ row }: { row: RptCreditoMorosidadRow }) {
  return (
    <div className="credix-morosidad-contact">
      <Space wrap size="middle">
        <Typography.Text>
          <UserOutlined aria-hidden /> <strong>{row.cliente ?? '—'}</strong>
        </Typography.Text>
        <Typography.Text type="secondary">
          <PhoneOutlined aria-hidden /> {row.celular ?? '—'}
        </Typography.Text>
        <Typography.Text type="secondary">
          <EnvironmentOutlined aria-hidden /> {row.direccion ?? '—'}
        </Typography.Text>
        {row.fechaUltPago ? (
          <Typography.Text type="secondary">
            Últ. pago: {formatInformeFecha(row.fechaUltPago)}
          </Typography.Text>
        ) : null}
      </Space>
    </div>
  )
}
