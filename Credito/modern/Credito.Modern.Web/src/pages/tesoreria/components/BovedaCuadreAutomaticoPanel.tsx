import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Descriptions,
  InputNumber,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  BarChartOutlined,
  CalculatorOutlined,
  FileSearchOutlined,
  ReloadOutlined,
} from '@ant-design/icons'
import {
  fetchBovedaCuadrePreview,
  type BovedaAbiertaDto,
  type BovedaCuadreDenominacion,
  type BovedaCuadreMedio,
  type BovedaCuadreResponsable,
  type BovedaCuadreValidacion,
} from '../../../api/boveda'
import { ApiError } from '../../../api/errors'
import { CredixDataTable } from '../../../components/credix'
import { formatFecha } from '../../../utils/formatFecha'
import { formatMoney } from '../../../utils/formatMoney'

const { Text } = Typography

type Props = {
  oficinaId: number
  boveda: BovedaAbiertaDto
}

type ConteoState = Record<string, number>

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

function round2(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100
}

function diffTag(value: number | null | undefined) {
  if (value == null) return <Tag>Sin contraste</Tag>
  if (Math.abs(value) < 0.01) return <Tag color="green">Cuadrado</Tag>
  return <Tag color={value > 0 ? 'gold' : 'red'}>{value > 0 ? 'Sobrante' : 'Faltante'}</Tag>
}

function grupoColor(grupo: string): string {
  if (grupo === 'efectivo') return 'green'
  if (grupo === 'yape') return 'purple'
  if (grupo === 'plin') return 'cyan'
  if (grupo === 'bcp') return 'blue'
  if (grupo === 'interbank') return 'orange'
  if (grupo === 'bn') return 'red'
  return 'geekblue'
}

function totalConteo(denominaciones: BovedaCuadreDenominacion[], conteo: ConteoState) {
  return round2(
    denominaciones.reduce((total, item) => total + (conteo[item.codigo] ?? 0) * item.valor, 0),
  )
}

export function BovedaCuadreAutomaticoPanel({ oficinaId, boveda }: Props) {
  const [conteo, setConteo] = useState<ConteoState>({})
  const [ajusteManual, setAjusteManual] = useState(0)

  const preview = useQuery({
    queryKey: ['boveda-cuadre-preview', oficinaId, boveda.bovedaId],
    queryFn: () => fetchBovedaCuadrePreview(oficinaId, boveda.bovedaId),
    enabled: oficinaId > 0 && boveda.bovedaId > 0,
  })

  const data = preview.data
  const denominaciones = useMemo(
    () => data?.denominaciones ?? [],
    [data?.denominaciones],
  )
  const fisicoContado = useMemo(
    () => totalConteo(denominaciones, conteo),
    [denominaciones, conteo],
  )
  const fisicoAjustado = round2(fisicoContado + ajusteManual)
  const efectivoSistema = data?.totales.efectivoSistema ?? 0
  const diferenciaFisica = round2(fisicoAjustado - efectivoSistema)
  const conteoTieneDatos = Object.values(conteo).some((value) => value > 0) || ajusteManual !== 0

  const medioColumns: ColumnsType<BovedaCuadreMedio> = [
    {
      title: 'Medio',
      dataIndex: 'tipoPago',
      width: 180,
      render: (value: string, row) => (
        <Space size={6}>
          <Tag color={grupoColor(row.grupo)}>{row.grupo}</Tag>
          <Text strong>{value}</Text>
        </Space>
      ),
    },
    {
      title: 'Sistema',
      dataIndex: 'monto',
      width: 120,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Verificación',
      dataIndex: 'pagosNoVerificados',
      width: 140,
      render: (value: number, row) =>
        row.requiereVerificacion ? (
          <Tag color={value > 0 ? 'gold' : 'green'}>
            {value > 0 ? `${value} pendiente(s)` : 'Verificado'}
          </Tag>
        ) : (
          <Tag color="green">No aplica</Tag>
        ),
    },
  ]

  const responsableColumns: ColumnsType<BovedaCuadreResponsable> = [
    { title: 'Caja', dataIndex: 'caja', width: 110, fixed: 'left', ellipsis: true },
    { title: 'Encargado / analista', dataIndex: 'responsable', width: 160, ellipsis: true },
    {
      title: 'Inicio',
      dataIndex: 'fechaIniOperacion',
      width: 104,
      render: formatFecha,
    },
    {
      title: 'Estado',
      key: 'estado',
      width: 112,
      render: (_, row) =>
        row.indCierre ? (
          <Tag color={row.transBoveda ? 'blue' : 'green'}>
            {row.transBoveda ? 'Transferida' : 'Cerrada'}
          </Tag>
        ) : (
          <Tag color="gold">Abierta</Tag>
        ),
    },
    {
      title: 'Efectivo',
      dataIndex: 'efectivo',
      width: 116,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Digital / bancos',
      dataIndex: 'digitalBancos',
      width: 132,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Total sistema',
      dataIndex: 'totalSistema',
      width: 128,
      align: 'right',
      render: (value: number) => <Text strong>{formatMoney(value)}</Text>,
    },
    {
      title: 'Saldo caja',
      dataIndex: 'saldoFinal',
      width: 116,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Medios',
      key: 'medios',
      width: 280,
      render: (_, row) => (
        <Space wrap size={[4, 4]}>
          {row.medios.map((medio) => (
            <Tag key={`${row.cajaDiarioId}-${medio.tipoPagoId}`} color={grupoColor(medio.grupo)}>
              {medio.tipoPago}: S/ {formatMoney(medio.monto)}
            </Tag>
          ))}
        </Space>
      ),
    },
  ]

  const validacionColumns: ColumnsType<BovedaCuadreValidacion> = [
    { title: 'Validación', dataIndex: 'concepto', width: 260 },
    {
      title: 'Sistema',
      dataIndex: 'sistema',
      width: 120,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Contraste',
      dataIndex: 'contraste',
      width: 120,
      align: 'right',
      render: (value: number | null) => (value == null ? '—' : formatMoney(value)),
    },
    {
      title: 'Diferencia',
      dataIndex: 'diferencia',
      width: 170,
      align: 'right',
      render: (value: number | null) => (
        <Space>
          {value == null ? '—' : formatMoney(value)}
          {diffTag(value)}
        </Space>
      ),
    },
    { title: 'Criterio', dataIndex: 'mensaje', ellipsis: true },
  ]

  if (preview.isError) {
    return (
      <Alert
        type="error"
        showIcon
        message="No se pudo generar el cuadre automático"
        description={errMsg(preview.error)}
      />
    )
  }

  return (
    <div className="boveda-cuadre">
      <Alert
        type={data?.pendientes.length ? 'warning' : 'success'}
        showIcon
        message="Cuadre automático integral"
        description="El sistema precarga responsables, cajas, bóveda, bancos, billeteras, transferencias, validaciones y pendientes. El encargado solo completa el conteo físico y ajustes documentados."
      />

      <div className="boveda-cuadre__stats">
        <Statistic
          title="Efectivo sistema"
          value={data?.totales.efectivoSistema ?? 0}
          precision={2}
          prefix="S/"
          loading={preview.isLoading}
          className="boveda-cuadre__stat"
        />
        <Statistic
          title="Digital / bancos"
          value={data?.totales.digitalBancosSistema ?? 0}
          precision={2}
          prefix="S/"
          loading={preview.isLoading}
          className="boveda-cuadre__stat"
        />
        <Statistic
          title="Total medios sistema"
          value={data?.totales.totalMediosSistema ?? 0}
          precision={2}
          prefix="S/"
          loading={preview.isLoading}
          className="boveda-cuadre__stat"
        />
        <Statistic
          title="Total contado"
          value={fisicoAjustado}
          precision={2}
          prefix="S/"
          className="boveda-cuadre__stat boveda-cuadre__stat--primary"
        />
        <Statistic
          title="Diferencia física"
          value={conteoTieneDatos ? diferenciaFisica : 0}
          precision={2}
          prefix="S/"
          className="boveda-cuadre__stat"
          valueStyle={{ color: Math.abs(diferenciaFisica) < 0.01 ? '#16a34a' : '#b91c1c' }}
        />
      </div>

      <div className="boveda-cuadre__actions">
        <Space wrap>
          <Tag icon={<CalculatorOutlined />} color="blue">
            Preview desde BD
          </Tag>
          <Button icon={<ReloadOutlined />} loading={preview.isFetching} onClick={() => void preview.refetch()}>
            Recalcular
          </Button>
          <Link to="/caja/saldos">
            <Button icon={<FileSearchOutlined />}>Revisar saldos y cierre</Button>
          </Link>
          <Link to={`/tesoreria/movimiento-boveda?bovedaId=${boveda.bovedaId}`}>
            <Button icon={<BarChartOutlined />}>Ver movimientos bóveda</Button>
          </Link>
        </Space>
      </div>

      {data?.pendientes.length ? (
        <Alert
          type="warning"
          showIcon
          message="Pendientes antes de cerrar"
          description={
            <ul className="boveda-cuadre__pending-list">
              {data.pendientes.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          }
        />
      ) : null}

      <Descriptions bordered size="small" column={{ xs: 1, md: 2, xl: 4 }}>
        <Descriptions.Item label="Bóveda">#{data?.boveda.bovedaId ?? boveda.bovedaId}</Descriptions.Item>
        <Descriptions.Item label="Efectivo bóveda">
          S/ {formatMoney(data?.totales.efectivoBoveda ?? boveda.saldoFinal)}
        </Descriptions.Item>
        <Descriptions.Item label="Efectivo cajas">
          S/ {formatMoney(data?.totales.efectivoCajas ?? 0)}
        </Descriptions.Item>
        <Descriptions.Item label="Monto a llevar">
          <Text strong>S/ {formatMoney(data?.totales.montoALlevarSugerido ?? 0)}</Text>
        </Descriptions.Item>
        <Descriptions.Item label="Saldo por llevar">
          S/ {formatMoney(data?.totales.saldoPorLlevarSugerido ?? 0)}
        </Descriptions.Item>
        <Descriptions.Item label="Total fondo">
          S/ {formatMoney(data?.totales.totalFondo ?? 0)}
        </Descriptions.Item>
        <Descriptions.Item label="Diferencia sistema">
          <Space>
            S/ {formatMoney(data?.totales.diferenciaSistema ?? 0)}
            {diffTag(data?.totales.diferenciaSistema)}
          </Space>
        </Descriptions.Item>
        <Descriptions.Item label="Conteo físico">
          <Space>
            S/ {formatMoney(fisicoAjustado)}
            {conteoTieneDatos ? diffTag(diferenciaFisica) : <Tag>Por contar</Tag>}
          </Space>
        </Descriptions.Item>
      </Descriptions>

      <div className="boveda-cuadre__section-title">Conteo físico por denominación</div>
      <div className="boveda-cuadre__denoms">
        {denominaciones.map((item) => (
          <div key={item.codigo} className="boveda-cuadre__denom">
            <Text strong>{item.etiqueta}</Text>
            <InputNumber
              min={0}
              precision={0}
              value={conteo[item.codigo] ?? 0}
              onChange={(value) =>
                setConteo((prev) => ({ ...prev, [item.codigo]: Number(value ?? 0) }))
              }
              style={{ width: '100%' }}
            />
            <Text type="secondary">
              S/ {formatMoney((conteo[item.codigo] ?? 0) * item.valor)}
            </Text>
          </div>
        ))}
        <div className="boveda-cuadre__denom boveda-cuadre__denom--ajuste">
          <Text strong>Ajuste / observación cuantificada</Text>
          <InputNumber
            precision={2}
            value={ajusteManual}
            onChange={(value) => setAjusteManual(Number(value ?? 0))}
            style={{ width: '100%' }}
          />
          <Text type="secondary">Use positivo para sobrante y negativo para faltante documentado.</Text>
        </div>
      </div>

      <div className="boveda-cuadre__section-title">Responsables, cajas y medios del sistema</div>
      <CredixDataTable<BovedaCuadreResponsable>
        mode="operacion"
        rowKey="cajaDiarioId"
        loading={preview.isLoading}
        columns={responsableColumns}
        dataSource={data?.responsables ?? []}
        pagination={{ pageSize: 12 }}
        scroll={{ x: 1300 }}
        summary={(rows) => (
          <Table.Summary fixed>
            <Table.Summary.Row>
              <Table.Summary.Cell index={0} colSpan={4}>
                <strong>TOTAL RESPONSABLES</strong>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={4} align="right">
                <strong>{formatMoney(rows.reduce((total, row) => total + row.efectivo, 0))}</strong>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={5} align="right">
                <strong>{formatMoney(rows.reduce((total, row) => total + row.digitalBancos, 0))}</strong>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={6} align="right">
                <strong>{formatMoney(rows.reduce((total, row) => total + row.totalSistema, 0))}</strong>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={7} align="right">
                <strong>{formatMoney(rows.reduce((total, row) => total + row.saldoFinal, 0))}</strong>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={8} />
            </Table.Summary.Row>
          </Table.Summary>
        )}
      />

      <div className="boveda-cuadre__split">
        <div>
          <div className="boveda-cuadre__section-title">Medios de bóveda</div>
          <CredixDataTable<BovedaCuadreMedio>
            mode="operacion"
            rowKey={(row) => `${row.tipoPagoId}-${row.grupo}`}
            loading={preview.isLoading}
            columns={medioColumns}
            dataSource={data?.mediosBoveda ?? []}
            pagination={false}
          />
        </div>
        <div>
          <div className="boveda-cuadre__section-title">Validaciones automáticas</div>
          <CredixDataTable<BovedaCuadreValidacion>
            mode="operacion"
            rowKey="codigo"
            loading={preview.isLoading}
            columns={validacionColumns}
            dataSource={data?.validaciones ?? []}
            pagination={false}
          />
        </div>
      </div>
    </div>
  )
}
