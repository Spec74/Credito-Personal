import { useMutation } from '@tanstack/react-query'
import { Button, Dropdown, Space, Tooltip, Typography, message } from 'antd'
import type { MenuProps } from 'antd'
import { DownOutlined, FileExcelOutlined, FilePdfOutlined, PrinterOutlined } from '@ant-design/icons'
import {
  downloadMovimientosCreditoCsv,
  openMovimientosCreditoPdfInTab,
  openRptClientePdfInTab,
  openRptEstadoCreditoPdfInTab,
  openRptPlanPagosPdfInTab,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'

const { Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

type Props = {
  creditoId: number
  personaId?: number | null
}

/**
 * Reportes del crédito: PDFs principales visibles; el resto en menú.
 */
export function CreditoConsultaImpresosBar({ creditoId, personaId }: Props) {
  const planPdf = useMutation({
    mutationFn: () => openRptPlanPagosPdfInTab(creditoId),
    onSuccess: () => message.success('Plan de pagos abierto'),
    onError: (e) => message.error(errMsg(e)),
  })

  const estadoPdf = useMutation({
    mutationFn: () => openRptEstadoCreditoPdfInTab(creditoId),
    onSuccess: () => message.success('Estado de cuenta abierto'),
    onError: (e) => message.error(errMsg(e)),
  })

  const movPdf = useMutation({
    mutationFn: () => openMovimientosCreditoPdfInTab(creditoId),
    onSuccess: () => message.success('Movimientos abierto'),
    onError: (e) => message.error(errMsg(e)),
  })

  const movCsv = useMutation({
    mutationFn: () => downloadMovimientosCreditoCsv(creditoId),
    onSuccess: () => message.success('Movimientos CSV'),
    onError: (e) => message.error(errMsg(e)),
  })

  const fichaPdf = useMutation({
    mutationFn: () => openRptClientePdfInTab(personaId!),
    onSuccess: () => message.success('Ficha cliente abierta'),
    onError: (e) => message.error(errMsg(e)),
  })

  const busy =
    planPdf.isPending ||
    estadoPdf.isPending ||
    movPdf.isPending ||
    movCsv.isPending ||
    fichaPdf.isPending

  const masItems: MenuProps['items'] = [
    {
      key: 'mov-pdf',
      icon: <FilePdfOutlined />,
      label: 'Movimientos PDF',
      disabled: busy,
      onClick: () => movPdf.mutate(),
    },
    {
      key: 'mov-csv',
      icon: <FileExcelOutlined />,
      label: 'Movimientos CSV',
      disabled: busy,
      onClick: () => movCsv.mutate(),
    },
    personaId != null && personaId > 0
      ? {
          key: 'ficha',
          icon: <FilePdfOutlined />,
          label: 'Ficha cliente',
          disabled: busy,
          onClick: () => fichaPdf.mutate(),
        }
      : null,
  ].filter(Boolean)

  return (
    <section className="credito-consulta-impresos" aria-label="Impresos del crédito">
      <Text type="secondary" className="credito-consulta-impresos__label">
        <PrinterOutlined aria-hidden /> Reportes
      </Text>
      <Space wrap size={[8, 8]} className="credito-consulta-impresos__actions">
        <Tooltip title="Estado de cuenta en PDF">
          <Button
            icon={<FilePdfOutlined />}
            loading={estadoPdf.isPending}
            disabled={busy}
            className="credix-report-btn credix-report-btn--pdf"
            onClick={() => estadoPdf.mutate()}
          >
            Estado cuenta
          </Button>
        </Tooltip>
        <Tooltip title="Plan de cuotas en PDF">
          <Button
            icon={<FilePdfOutlined />}
            loading={planPdf.isPending}
            disabled={busy}
            className="credix-report-btn credix-report-btn--pdf"
            onClick={() => planPdf.mutate()}
          >
            Plan de pagos
          </Button>
        </Tooltip>
        <Dropdown menu={{ items: masItems }} trigger={['click']} placement="bottomRight">
          <Button disabled={busy}>
            Más reportes <DownOutlined />
          </Button>
        </Dropdown>
      </Space>
    </section>
  )
}
