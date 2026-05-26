import { useMutation } from '@tanstack/react-query'
import { Button, Divider, Space, Tooltip, Typography, message } from 'antd'
import {
  FileExcelOutlined,
  FilePdfOutlined,
  PrinterOutlined,
  ExportOutlined,
} from '@ant-design/icons'
import {
  downloadMovimientosCreditoCsv,
  downloadMovimientosCreditoPdf,
  downloadRptClientePdf,
  downloadRptEstadoCreditoPdf,
  downloadRptPlanPagosPdf,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import {
  openLegacyReporteCreditoMovimiento,
  openLegacyReporteEstadoCredito,
  openLegacyReportePlanPagos,
} from '../../config/creditoLegacyReports'

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
 * Fase 5: PDF tabla (API) + enlace diseño RDLC legacy.
 */
export function CreditoConsultaImpresosBar({ creditoId, personaId }: Props) {
  const planPdf = useMutation({
    mutationFn: () => downloadRptPlanPagosPdf(creditoId),
    onSuccess: () => message.success('Plan de pagos (PDF tabla)'),
    onError: (e) => message.error(errMsg(e)),
  })

  const estadoPdf = useMutation({
    mutationFn: () => downloadRptEstadoCreditoPdf(creditoId),
    onSuccess: () => message.success('Estado de cuenta (PDF tabla)'),
    onError: (e) => message.error(errMsg(e)),
  })

  const movPdf = useMutation({
    mutationFn: () => downloadMovimientosCreditoPdf(creditoId),
    onSuccess: () => message.success('Movimientos (PDF tabla)'),
    onError: (e) => message.error(errMsg(e)),
  })

  const movCsv = useMutation({
    mutationFn: () => downloadMovimientosCreditoCsv(creditoId),
    onSuccess: () => message.success('Movimientos CSV'),
    onError: (e) => message.error(errMsg(e)),
  })

  const fichaPdf = useMutation({
    mutationFn: () => downloadRptClientePdf(personaId!),
    onSuccess: () => message.success('Ficha cliente PDF'),
    onError: (e) => message.error(errMsg(e)),
  })

  const busy =
    planPdf.isPending ||
    estadoPdf.isPending ||
    movPdf.isPending ||
    movCsv.isPending ||
    fichaPdf.isPending

  const openLegacy = (fn: () => void, label: string) => {
    try {
      fn()
    } catch (e) {
      message.error(e instanceof Error ? e.message : `No se pudo abrir ${label}`)
    }
  }

  return (
    <section className="credito-consulta-impresos" aria-label="Impresos del crédito">
      <div className="credito-consulta-impresos__head">
        <PrinterOutlined aria-hidden />
        <Text strong>Impresos</Text>
        <Text type="secondary" className="credito-consulta-impresos__hint">
          Datos vía API (QuestPDF) o diseño clásico ReportViewer (requiere sesión MVC).
        </Text>
      </div>

      <div className="credito-consulta-impresos__group">
        <Text type="secondary" className="credito-consulta-impresos__group-label">
          Exportación moderna (mismos datos, PDF tabla)
        </Text>
        <Space wrap size={[8, 8]} className="credito-consulta-impresos__actions">
          <Tooltip title="PDF tabular — equivalente en datos a Estado cuenta">
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
          <Tooltip title="PDF tabular — plan de cuotas">
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
          <Tooltip title="PDF tabular — movimientos del crédito">
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

      <Divider className="credito-consulta-impresos__divider" />

      <div className="credito-consulta-impresos__group">
        <Text type="secondary" className="credito-consulta-impresos__group-label">
          Diseño legacy (RDLC / ReportViewer)
        </Text>
        <Space wrap size={[8, 8]} className="credito-consulta-impresos__actions">
          <Tooltip title="Abre ReporteEstadoCredito en el sistema clásico">
            <Button
              icon={<ExportOutlined />}
              disabled={busy}
              onClick={() =>
                openLegacy(
                  () => openLegacyReporteEstadoCredito(creditoId),
                  'estado de cuenta',
                )
              }
            >
              Estado cuenta (RDLC)
            </Button>
          </Tooltip>
          <Tooltip title="Abre ReportePlanPagos (rptSimuladorPlanPago)">
            <Button
              icon={<ExportOutlined />}
              disabled={busy}
              onClick={() =>
                openLegacy(() => openLegacyReportePlanPagos(creditoId), 'plan de pagos')
              }
            >
              Plan pagos (RDLC)
            </Button>
          </Tooltip>
          <Tooltip title="Abre ReporteCreditoMovimiento">
            <Button
              icon={<ExportOutlined />}
              disabled={busy}
              onClick={() =>
                openLegacy(
                  () => openLegacyReporteCreditoMovimiento(creditoId),
                  'movimientos',
                )
              }
            >
              Movimientos (RDLC)
            </Button>
          </Tooltip>
        </Space>
      </div>
    </section>
  )
}
