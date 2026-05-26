import { useMemo } from 'react'
import {
  Alert,
  Button,
  Col,
  Row,
  Space,
  Statistic,
  Tabs,
  Tag,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  FilePdfOutlined,
  InfoCircleOutlined,
  WalletOutlined,
} from '@ant-design/icons'
import {
  fetchCreditoMora,
  fetchCreditoMoraResumen,
  fetchCuotasPendientes,
  type CreditoMoraRow,
} from '../../api/cajaDiario'
import {
  downloadRptEstadoCreditoPdf,
  downloadRptPlanPagosPdf,
  fetchMoraPendiente,
} from '../../api/creditoPlanes'
import type { CuotasPendientesRow } from '../../types/api'
import { formatMoney } from '../../utils/formatMoney'
import { formatFecha, formatFechaHora } from '../../utils/formatFecha'
import { CajaModal } from './CajaModal'
import { CredixDataTable } from '../credix'
import {
  extraerMorasVigentesEnCuotas,
  sumarMoraVigente,
  sumarSaldoPostergadoHistorial,
  type MoraVigenteCuotaRow,
} from './creditoMoraVista'

const { Text, Paragraph } = Typography

export function CreditoMoraModal({
  open,
  creditoId,
  cuotas,
  onClose,
  onPrepararCobroTodasCuotas,
}: {
  open: boolean
  creditoId: number | null
  /** Cuotas cargadas en cobranzas (usp_CuotasPendientes). */
  cuotas: CuotasPendientesRow[]
  onClose: () => void
  /** Selecciona todas las cuotas cobrables y cierra el modal. */
  onPrepararCobroTodasCuotas?: () => void
}) {
  const historialQuery = useQuery({
    queryKey: ['credito-mora-historial', creditoId],
    queryFn: () => fetchCreditoMora(creditoId!),
    enabled: open && creditoId != null && creditoId > 0,
  })

  const cuotasFallbackQuery = useQuery({
    queryKey: ['cuotas-pendientes-mora-modal', creditoId],
    queryFn: () => fetchCuotasPendientes(creditoId!),
    enabled:
      open &&
      creditoId != null &&
      creditoId > 0 &&
      cuotas.length === 0,
  })

  const cuotasEfectivas = useMemo(
    () => (cuotas.length > 0 ? cuotas : (cuotasFallbackQuery.data ?? [])),
    [cuotas, cuotasFallbackQuery.data],
  )



  const resumenQuery = useQuery({
    queryKey: ['credito-mora-resumen', creditoId],
    queryFn: () => fetchCreditoMoraResumen(creditoId!),
    enabled: open && creditoId != null && creditoId > 0,
  })

  const moraCalcQuery = useQuery({
    queryKey: ['mora-pendiente', creditoId],
    queryFn: () => fetchMoraPendiente(creditoId!),
    enabled: open && creditoId != null && creditoId > 0,
  })

  const morasVigentes = useMemo(
    () => extraerMorasVigentesEnCuotas(cuotasEfectivas),
    [cuotasEfectivas],
  )

  const totalVigente = useMemo(
    () => sumarMoraVigente(cuotasEfectivas),
    [cuotasEfectivas],
  )

  const historial = historialQuery.data ?? []
  const saldoHistorial = useMemo(
    () => sumarSaldoPostergadoHistorial(historial),
    [historial],
  )

  const indMora = resumenQuery.data?.indMoraProducto ?? false
  const saldoApi = resumenQuery.data?.saldoPostergado ?? 0
  const moraCalculada = moraCalcQuery.data?.moraPendiente ?? 0

  const pdfEstado = useMutation({
    mutationFn: () => downloadRptEstadoCreditoPdf(creditoId!),
    onSuccess: () => message.success('Estado de cuenta descargado'),
    onError: () => message.error('No se pudo generar el PDF'),
  })

  const pdfPlan = useMutation({
    mutationFn: () => downloadRptPlanPagosPdf(creditoId!),
    onSuccess: () => message.success('Plan de pagos descargado'),
    onError: () => message.error('No se pudo generar el PDF'),
  })

  const colsVigente: ColumnsType<MoraVigenteCuotaRow> = [
    { title: 'Cuota', dataIndex: 'glosa', ellipsis: true },
    {
      title: 'Vence',
      dataIndex: 'fechaVencimiento',
      width: 100,
      render: (v: string | null) => formatFecha(v),
    },
    {
      title: 'Días',
      dataIndex: 'diasAtrazo',
      width: 64,
      align: 'center',
      render: (d: number) =>
        d > 0 ? <Tag color="error">{d} d</Tag> : <Tag>—</Tag>,
    },
    {
      title: 'Mora',
      dataIndex: 'importeMora',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cuota a pagar',
      dataIndex: 'aPagar',
      align: 'right',
      render: formatMoney,
    },
  ]

  const colsHistorial: ColumnsType<CreditoMoraRow> = [
    {
      title: 'Fecha reg.',
      dataIndex: 'fecha',
      width: 118,
      render: (v: string) => formatFechaHora(v),
    },
    {
      title: 'Mora',
      dataIndex: 'mora',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Días',
      dataIndex: 'diasAtrazo',
      width: 64,
      align: 'center',
    },
    {
      title: 'Saldo',
      dataIndex: 'saldoMora',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Estado',
      width: 120,
      render: (_, row) =>
        row.movimientoCajaId == null || row.movimientoCajaId === 0 ? (
          <Tag color="orange">Pendiente cobro</Tag>
        ) : (
          <Tag color="green">Liquidada</Tag>
        ),
    },
  ]

  const sinDatosHistorial = !historialQuery.isLoading && historial.length === 0
  const sinDatosVigente = morasVigentes.length === 0

  return (
    <CajaModal
      title="Mora del crédito"
      open={open}
      onCancel={onClose}
      width={920}
      className="caja-mora-modal"
      footer={
        <Space wrap className="caja-mora-modal__footer">
          <Button onClick={onClose}>Cerrar</Button>
          {creditoId ? (
            <>
              <Button
                icon={<FilePdfOutlined />}
                loading={pdfEstado.isPending}
                onClick={() => pdfEstado.mutate()}
              >
                Estado cuenta (PDF)
              </Button>
              <Button
                icon={<FilePdfOutlined />}
                loading={pdfPlan.isPending}
                onClick={() => pdfPlan.mutate()}
              >
                Plan pagos (PDF)
              </Button>
              {onPrepararCobroTodasCuotas ? (
                <Button
                  type="primary"
                  icon={<WalletOutlined />}
                  onClick={() => {
                    onPrepararCobroTodasCuotas()
                    onClose()
                  }}
                >
                  Preparar cobro de cuotas
                </Button>
              ) : null}
            </>
          ) : null}
        </Space>
      }
    >
      {!indMora && resumenQuery.isSuccess ? (
        <Alert
          type="info"
          showIcon
          message="Este producto no aplica mora postergada (IndMora desactivado)."
          style={{ marginBottom: 16 }}
        />
      ) : null}

      <Row gutter={[12, 12]} className="caja-mora-modal__kpis">
        <Col xs={24} sm={8}>
          <div className="caja-mora-kpi">
            <Statistic
              title="Mora en cuotas (hoy)"
              value={totalVigente}
              precision={2}
              prefix="S/."
              loading={false}
            />
            <Text type="secondary" className="caja-mora-kpi__hint">
              {morasVigentes.length} cuota(s) con atraso
            </Text>
          </div>
        </Col>
        <Col xs={24} sm={8}>
          <div className="caja-mora-kpi">
            <Statistic
              title="Postergada (tabla)"
              value={saldoApi || saldoHistorial}
              precision={2}
              prefix="S/."
              loading={resumenQuery.isLoading}
            />
            <Text type="secondary" className="caja-mora-kpi__hint">
              Registrada al pagar cuotas con mora
            </Text>
          </div>
        </Col>
        <Col xs={24} sm={8}>
          <div className="caja-mora-kpi">
            <Statistic
              title="Mora total (cálculo)"
              value={moraCalculada}
              precision={2}
              prefix="S/."
              loading={moraCalcQuery.isLoading}
            />
            <Text type="secondary" className="caja-mora-kpi__hint">
              Cálculo consolidado del crédito
            </Text>
          </div>
        </Col>
      </Row>

      <Alert
        type="info"
        showIcon
        icon={<InfoCircleOutlined />}
        className="caja-mora-modal__info"
        message="¿Dónde se cobra la mora?"
        description={
          <Paragraph style={{ marginBottom: 0 }}>
            La mora <strong>postergada</strong> se registra en{' '}
            <code>CREDITO.CreditoMora</code> al cobrar cuotas con atraso y se{' '}
            <strong>liquida al pagar la última cuota pendiente</strong> en la pestaña
            Cobranzas (movimiento MOR). No se cobra desde este cuadro: use «Preparar
            cobro de cuotas» o seleccione las cuotas en la grilla.
          </Paragraph>
        }
      />

      <Tabs
        className="caja-mora-modal__tabs"
        items={[
          {
            key: 'vigente',
            label: `Mora vigente (${morasVigentes.length})`,
            children: (
              <>
                {sinDatosVigente ? (
                  <Alert
                    type="success"
                    showIcon
                    message="No hay cuotas con mora o atraso en la vista actual."
                  />
                ) : (
                  <CredixDataTable<MoraVigenteCuotaRow>
                    rowKey="key"
                    columns={colsVigente}
                    dataSource={morasVigentes}
                    loading={cuotasFallbackQuery.isLoading && cuotas.length === 0}
                    pagination={{ defaultPageSize: 8, hideOnSinglePage: true }}
                    size="small"
                    scroll={{ x: 640 }}
                  />
                )}
              </>
            ),
          },
          {
            key: 'historial',
            label: `Historial postergado (${historial.length})`,
            children: (
              <>
                {sinDatosHistorial ? (
                  <Alert
                    type="warning"
                    showIcon
                    message="Sin registros en CreditoMora todavía"
                    description="Es normal si aún no se ha cobrado ninguna cuota con mora en esta sesión. Al pagar cuotas atrasadas, el sistema registrará cada mora aquí hasta liquidarlas con la última cuota."
                  />
                ) : (
                  <CredixDataTable<CreditoMoraRow>
                    rowKey="creditoMoraId"
                    columns={colsHistorial}
                    dataSource={historial}
                    loading={historialQuery.isLoading}
                    pagination={{ defaultPageSize: 8, hideOnSinglePage: true }}
                    size="small"
                    scroll={{ x: 640 }}
                  />
                )}
              </>
            ),
          },
        ]}
      />
    </CajaModal>
  )
}
