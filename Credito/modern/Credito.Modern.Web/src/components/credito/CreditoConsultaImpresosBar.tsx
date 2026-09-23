import { useMutation } from '@tanstack/react-query'
import { Button, Space, Tooltip, Typography, message } from 'antd'
import { FileExcelOutlined, FilePdfOutlined, PrinterOutlined } from '@ant-design/icons'
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
 * Paridad botones `btncImpEstadoCuenta`, `btncImpPlanPago`, `btncImpMovimiento` en Creditos.cshtml.
 * PDF moderno con branding corporativo y visor en pestaña nueva.
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

  return (
    <section className="credito-consulta-impresos" aria-label="Impresos del crédito">
      <div className="credito-consulta-impresos__head">
        <span className="credito-consulta-impresos__icon" aria-hidden>
          <PrinterOutlined />
        </span>
        <div>
          <Text strong className="credito-consulta-impresos__title">
            Reportes del crédito
          </Text>
          <Text type="secondary" className="credito-consulta-impresos__hint">
            PDFs con cabecera del crédito, datos de paridad legacy y formato profesional.
          </Text>
        </div>
      </div>

      <div className="credito-consulta-impresos__group">
        <Text type="secondary" className="credito-consulta-impresos__group-label">
          Documentos disponibles
        </Text>
        <Space wrap size={[8, 8]} className="credito-consulta-impresos__actions">
          <Tooltip title="Abrir PDF moderno con logo, colores y datos del estado de cuenta">
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
          <Tooltip title="Abrir PDF moderno con logo, colores y plan de cuotas">
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
          <Tooltip title="Abrir PDF moderno de movimientos del crédito">
            <Button
              icon={<FilePdfOutlined />}
              loading={movPdf.isPending}
              disabled={busy}
              className="credix-report-btn credix-report-btn--pdf"
              onClick={() => movPdf.mutate()}
            >
              Movimientos
            </Button>
          </Tooltip>
          <Button
            icon={<FileExcelOutlined />}
            loading={movCsv.isPending}
            disabled={busy}
            className="credix-report-btn credix-report-btn--xls"
            onClick={() => movCsv.mutate()}
          >
            Movimientos CSV
          </Button>
          {personaId != null && personaId > 0 ? (
            <Button
              icon={<FilePdfOutlined />}
              loading={fichaPdf.isPending}
              disabled={busy}
              className="credix-report-btn credix-report-btn--pdf"
              onClick={() => fichaPdf.mutate()}
            >
              Ficha cliente
            </Button>
          ) : null}
        </Space>
      </div>
    </section>
  )
}
