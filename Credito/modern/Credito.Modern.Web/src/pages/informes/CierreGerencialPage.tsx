import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Button,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Select,
  Space,
  Tag,
  Typography,
  message,
} from 'antd'
import {
  DownloadOutlined,
  ReloadOutlined,
  SaveOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadCierreGerencialExcel,
  fetchCierreGerencialAvance,
  fetchCierreGerencialMetas,
  fetchCierreGerencialPermisos,
  guardarCierreGerencialMetas,
  type AvanceMetaGerencialRow,
  type MetaGerencialRow,
} from '../../api/cierreGerencial'
import { ApiError } from '../../api/errors'
import { CredixDataTable, CredixPage, CredixPanel } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'

const { Text } = Typography

type TipoFiltro = '' | 'PRODUCTIVA' | 'ESPECIAL'

function periodoIso(d: Dayjs): string {
  return d.startOf('month').format('YYYY-MM-DD')
}

function estadoTone(estado?: string): 'success' | 'warning' | 'error' | 'default' {
  const e = (estado ?? '').toUpperCase()
  if (e.includes('CUMPL') || e.includes('OK') || e.includes('VERDE')) return 'success'
  if (e.includes('ALERT') || e.includes('AMAR')) return 'warning'
  if (e.includes('NO') || e.includes('ROJO') || e.includes('CRIT')) return 'error'
  return 'default'
}

function esEspecial(tipo?: string): boolean {
  return (tipo ?? '').toUpperCase() === 'ESPECIAL'
}

function coincideFiltro(
  row: { tipoCartera: string; nombreCompleto?: string; nombreUsuario?: string; mercado?: string; supervisor?: string },
  tipo: TipoFiltro,
  buscar: string,
): boolean {
  if (tipo && row.tipoCartera?.toUpperCase() !== tipo) return false
  const q = buscar.trim().toUpperCase()
  if (!q) return true
  const haystack = [row.nombreCompleto, row.nombreUsuario, row.mercado, row.supervisor]
    .filter(Boolean)
    .join(' ')
    .toUpperCase()
  return haystack.includes(q)
}

function MetaInput(props: {
  editable: boolean
  noAplica?: boolean
  value: number | null
  integer?: boolean
  onChange: (v: number | null) => void
}) {
  if (props.noAplica) {
    return <Text type="secondary">NO APLICA</Text>
  }
  return (
    <InputNumber
      min={0}
      step={props.integer ? 1 : 0.01}
      style={{ width: '100%' }}
      disabled={!props.editable}
      value={props.value ?? undefined}
      onChange={(v) => props.onChange(v == null ? null : Number(v))}
    />
  )
}

export function CierreGerencialPage() {
  const queryClient = useQueryClient()
  const [periodo, setPeriodo] = useState<Dayjs>(() => dayjs().startOf('month'))
  const periodoKey = periodoIso(periodo)
  const [tipoFiltro, setTipoFiltro] = useState<TipoFiltro>('')
  const [buscar, setBuscar] = useState('')
  const [metasEdit, setMetasEdit] = useState<MetaGerencialRow[]>([])

  const permisos = useQuery({
    queryKey: ['cierre-gerencial-permisos'],
    queryFn: fetchCierreGerencialPermisos,
  })

  const avance = useQuery({
    queryKey: ['cierre-gerencial-avance', periodoKey],
    queryFn: () => fetchCierreGerencialAvance(periodoKey),
    enabled: permisos.data?.puedeConsultar === true,
  })

  const metas = useQuery({
    queryKey: ['cierre-gerencial-metas', periodoKey],
    queryFn: () => fetchCierreGerencialMetas(periodoKey),
    enabled: permisos.data?.puedeConsultar === true,
  })

  useEffect(() => {
    if (metas.data?.metas) {
      setMetasEdit(metas.data.metas.map((m) => ({ ...m })))
    }
  }, [metas.data])

  const excel = useMutation({
    mutationFn: () => downloadCierreGerencialExcel(periodoKey),
    onSuccess: () => message.success('Excel descargado'),
    onError: (e) => message.error(e instanceof ApiError ? e.message : 'No se pudo exportar'),
  })

  const guardar = useMutation({
    mutationFn: () =>
      guardarCierreGerencialMetas({
        periodo: periodoKey,
        metas: metasEdit.map((m) => ({
          usuarioId: m.usuarioId,
          tipoCartera: m.tipoCartera,
          metaCapitalCierre: esEspecial(m.tipoCartera) ? null : m.metaCapitalCierre,
          metaClientesActivosCierre: esEspecial(m.tipoCartera) ? null : m.metaClientesActivosCierre,
          metaVencidosMaximoCierre: m.metaVencidosMaximoCierre,
          metaRecuperacionVencidosMes: m.metaRecuperacionVencidosMes,
        })),
      }),
    onSuccess: async (r) => {
      message.success(r.message)
      await queryClient.invalidateQueries({ queryKey: ['cierre-gerencial-metas', periodoKey] })
      await queryClient.invalidateQueries({ queryKey: ['cierre-gerencial-avance', periodoKey] })
    },
    onError: (e) => message.error(e instanceof ApiError ? e.message : 'No se pudo guardar'),
  })

  const avanceFiltrado = useMemo(
    () => (avance.data?.filas ?? []).filter((f) => coincideFiltro(f, tipoFiltro, buscar)),
    [avance.data, tipoFiltro, buscar],
  )

  const metasFiltradas = useMemo(
    () => metasEdit.filter((m) => coincideFiltro(m, tipoFiltro, buscar)),
    [metasEdit, tipoFiltro, buscar],
  )

  const kpis = useMemo(() => {
    const filas = avanceFiltrado
    const total = avance.data?.filas?.length ?? 0
    return {
      capital: filas.reduce((s, f) => s + (f.capitalActual || 0), 0),
      clientes: filas.reduce((s, f) => s + (f.clientesActivosActual || 0), 0),
      vencidos: filas.reduce((s, f) => s + (f.vencidosActual || 0), 0),
      metasOk: filas.filter((f) => f.metaConfigurada).length,
      total,
    }
  }, [avanceFiltrado, avance.data])

  const puedeEditarMetas = metas.data?.puedeEditar === true

  const avanceColumns: ColumnsType<AvanceMetaGerencialRow> = [
    { title: 'N.º', dataIndex: 'orden', width: 50, align: 'center' },
    {
      title: 'Analista',
      key: 'analista',
      ellipsis: true,
      width: 200,
      render: (_, r) => (
        <div>
          <div>{r.nombreCompleto || r.nombreUsuario}</div>
          <Text type="secondary" style={{ fontSize: 12 }}>
            {[r.supervisor, r.mercado].filter(Boolean).join(' · ')}
          </Text>
        </div>
      ),
    },
    { title: 'Tipo', dataIndex: 'tipoCartera', width: 100 },
    {
      title: 'Base cap.',
      dataIndex: 'capitalBase',
      align: 'right',
      width: 100,
      render: (v: number | null) => (v == null ? '—' : formatMoney(v)),
    },
    {
      title: 'Meta cap.',
      dataIndex: 'metaCapitalCierre',
      align: 'right',
      width: 100,
      render: (v: number | null) => (v == null ? '—' : formatMoney(v)),
    },
    {
      title: 'Capital',
      dataIndex: 'capitalActual',
      align: 'right',
      width: 100,
      render: formatMoney,
    },
    {
      title: '% Cap.',
      dataIndex: 'cumplimientoCapitalPct',
      width: 72,
      align: 'right',
      render: (v: number | null) => (v == null ? '—' : `${v.toFixed(1)}%`),
    },
    {
      title: 'Est. capital',
      dataIndex: 'estadoCapital',
      width: 100,
      render: (v: string) => <Tag color={estadoTone(v)}>{v || '—'}</Tag>,
    },
    {
      title: 'Cli. base',
      dataIndex: 'clientesBase',
      width: 72,
      align: 'right',
      render: (v: number | null) => (v == null ? '—' : v),
    },
    {
      title: 'Clientes',
      dataIndex: 'clientesActivosActual',
      width: 72,
      align: 'right',
    },
    {
      title: 'Est. cli.',
      dataIndex: 'estadoClientes',
      width: 100,
      render: (v: string) => <Tag color={estadoTone(v)}>{v || '—'}</Tag>,
    },
    {
      title: 'Base venc.',
      dataIndex: 'vencidosBaseComparable',
      align: 'right',
      width: 100,
      render: (v: number | null) => (v == null ? '—' : formatMoney(v)),
    },
    {
      title: 'Límite venc.',
      dataIndex: 'metaVencidosMaximoCierre',
      align: 'right',
      width: 100,
      render: (v: number | null) => (v == null ? '—' : formatMoney(v)),
    },
    {
      title: 'Vencidos',
      dataIndex: 'vencidosActual',
      align: 'right',
      width: 100,
      render: formatMoney,
    },
    {
      title: 'Est. venc.',
      dataIndex: 'estadoVencidos',
      width: 100,
      render: (v: string) => <Tag color={estadoTone(v)}>{v || '—'}</Tag>,
    },
    {
      title: 'Recuperación',
      dataIndex: 'recuperacionVencidosActual',
      align: 'right',
      width: 110,
      render: (v: number | null) => (v == null ? '—' : formatMoney(v)),
    },
    {
      title: 'Est. rec.',
      dataIndex: 'estadoRecuperacion',
      width: 100,
      render: (v: string) => <Tag color={estadoTone(v)}>{v || '—'}</Tag>,
    },
  ]

  const metasColumns: ColumnsType<MetaGerencialRow> = [
    { title: 'N.º', dataIndex: 'orden', width: 50, align: 'center' },
    { title: 'Analista', dataIndex: 'nombreCompleto', ellipsis: true, width: 180 },
    { title: 'Tipo', dataIndex: 'tipoCartera', width: 100 },
    {
      title: 'Base capital',
      dataIndex: 'capitalBase',
      align: 'right',
      width: 100,
      render: formatMoney,
    },
    {
      title: 'Meta capital',
      key: 'metaCapital',
      width: 120,
      render: (_, row) => {
        const idx = metasEdit.findIndex((m) => m.usuarioId === row.usuarioId && m.tipoCartera === row.tipoCartera)
        return (
          <MetaInput
            editable={puedeEditarMetas}
            noAplica={esEspecial(row.tipoCartera)}
            value={row.metaCapitalCierre}
            onChange={(v) =>
              setMetasEdit((prev) => {
                const next = [...prev]
                if (idx >= 0) next[idx] = { ...next[idx], metaCapitalCierre: v }
                return next
              })
            }
          />
        )
      },
    },
    {
      title: 'Base clientes',
      dataIndex: 'clientesActivosBase',
      width: 90,
      align: 'right',
    },
    {
      title: 'Meta clientes',
      key: 'metaClientes',
      width: 110,
      render: (_, row) => {
        const idx = metasEdit.findIndex((m) => m.usuarioId === row.usuarioId && m.tipoCartera === row.tipoCartera)
        return (
          <MetaInput
            editable={puedeEditarMetas}
            noAplica={esEspecial(row.tipoCartera)}
            integer
            value={row.metaClientesActivosCierre}
            onChange={(v) =>
              setMetasEdit((prev) => {
                const next = [...prev]
                if (idx >= 0) next[idx] = { ...next[idx], metaClientesActivosCierre: v }
                return next
              })
            }
          />
        )
      },
    },
    {
      title: 'Base vencidos',
      dataIndex: 'vencidosBaseComparable',
      align: 'right',
      width: 110,
      render: (v: number | null) => (v == null ? '—' : formatMoney(v)),
    },
    {
      title: 'Límite vencidos',
      key: 'metaVenc',
      width: 120,
      render: (_, row) => {
        const idx = metasEdit.findIndex((m) => m.usuarioId === row.usuarioId && m.tipoCartera === row.tipoCartera)
        return (
          <MetaInput
            editable={puedeEditarMetas}
            value={row.metaVencidosMaximoCierre}
            onChange={(v) =>
              setMetasEdit((prev) => {
                const next = [...prev]
                if (idx >= 0) next[idx] = { ...next[idx], metaVencidosMaximoCierre: v }
                return next
              })
            }
          />
        )
      },
    },
    {
      title: 'Meta recuperación',
      key: 'metaRec',
      width: 120,
      render: (_, row) => {
        const idx = metasEdit.findIndex((m) => m.usuarioId === row.usuarioId && m.tipoCartera === row.tipoCartera)
        return (
          <MetaInput
            editable={puedeEditarMetas}
            value={row.metaRecuperacionVencidosMes}
            onChange={(v) =>
              setMetasEdit((prev) => {
                const next = [...prev]
                if (idx >= 0) next[idx] = { ...next[idx], metaRecuperacionVencidosMes: v }
                return next
              })
            }
          />
        )
      },
    },
    {
      title: 'Estado',
      dataIndex: 'configurada',
      width: 110,
      align: 'center',
      render: (v: boolean) => (
        <Tag color={v ? 'success' : 'warning'}>{v ? 'CONFIGURADA' : 'PENDIENTE'}</Tag>
      ),
    },
  ]

  if (permisos.isLoading) {
    return (
      <CredixPage title="Cierre gerencial" breadcrumb={[{ title: 'Cargando…' }]}>
        <Text type="secondary">Verificando permisos…</Text>
      </CredixPage>
    )
  }

  if (permisos.isError || permisos.data?.puedeConsultar === false) {
    return (
      <CredixPage
        title="Cierre gerencial"
        breadcrumb={[
          { title: <Link to="/inicio">Inicio</Link> },
          { title: 'Cierre gerencial' },
        ]}
      >
        <Alert
          type="warning"
          showIcon
          message="Sin permiso"
          description={
            permisos.error instanceof ApiError
              ? permisos.error.message
              : 'Su usuario no está autorizado para consultar el módulo gerencial.'
          }
        />
      </CredixPage>
    )
  }

  return (
    <CredixPage
      title="Cierre y metas gerenciales"
      subtitle="Avance mensual oficial / no oficial por analista. El cierre oficial se genera al actualizar datos post-cierre de bóveda (fin de mes o días 1–2)."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/informes">Informes</Link> },
        { title: 'Cierre gerencial' },
      ]}
    >
      <Form layout="inline" className="credix-cierre-toolbar" style={{ marginBottom: 16, rowGap: 8 }}>
        <Form.Item label="Periodo">
          <DatePicker
            picker="month"
            value={periodo}
            format="MMMM YYYY"
            allowClear={false}
            onChange={(v) => v && setPeriodo(v.startOf('month'))}
          />
        </Form.Item>
        <Form.Item label="Tipo">
          <Select
            style={{ width: 140 }}
            value={tipoFiltro}
            onChange={(v) => setTipoFiltro(v as TipoFiltro)}
            options={[
              { value: '', label: 'Todas' },
              { value: 'PRODUCTIVA', label: 'Productiva' },
              { value: 'ESPECIAL', label: 'Especial' },
            ]}
          />
        </Form.Item>
        <Form.Item label="Buscar">
          <Input
            allowClear
            placeholder="Analista, mercado…"
            style={{ width: 180 }}
            value={buscar}
            onChange={(e) => setBuscar(e.target.value)}
          />
        </Form.Item>
        <Space wrap>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => {
              void avance.refetch()
              void metas.refetch()
            }}
          >
            Actualizar
          </Button>
          <Button
            icon={<DownloadOutlined />}
            loading={excel.isPending}
            onClick={() => void excel.mutateAsync()}
          >
            Exportar Excel
          </Button>
          {puedeEditarMetas ? (
            <Button
              type="primary"
              icon={<SaveOutlined />}
              loading={guardar.isPending}
              onClick={() => void guardar.mutateAsync()}
            >
              Guardar metas
            </Button>
          ) : null}
        </Space>
      </Form>

      {avance.data ? (
        <Alert
          style={{ marginBottom: 16 }}
          type={avance.data.avanceNoOficial ? 'warning' : 'success'}
          showIcon
          message={avance.data.avanceNoOficial ? 'Avance no oficial' : 'Cierre oficial'}
          description={`Periodo ${avance.data.periodo}${
            avance.data.fechaCalculo ? ` · Calculado ${avance.data.fechaCalculo}` : ''
          }${
            avance.data.avanceNoOficial
              ? '. Las cifras cambian hasta que se genere el cierre mensual (post-cierre bóveda).'
              : ''
          }`}
        />
      ) : null}

      <div className="credix-cobro-bloque-resumen" style={{ marginBottom: 16 }}>
        <div>
          <Text type="secondary">Saldo total</Text>
          <div>
            <strong>S/ {formatMoney(kpis.capital)}</strong>
          </div>
        </div>
        <div>
          <Text type="secondary">Clientes activos</Text>
          <div>
            <strong>{kpis.clientes}</strong>
          </div>
        </div>
        <div>
          <Text type="secondary">Vencidos actuales</Text>
          <div>
            <strong>S/ {formatMoney(kpis.vencidos)}</strong>
          </div>
        </div>
        <div>
          <Text type="secondary">Metas configuradas</Text>
          <div>
            <strong>
              {kpis.metasOk}
              {kpis.total > 0 ? ` / ${kpis.total}` : ''}
            </strong>
          </div>
        </div>
      </div>

      <CredixPanel title={`Avance (${avanceFiltrado.length}${tipoFiltro || buscar ? ` de ${avance.data?.total ?? 0}` : ''})`}>
        {avance.isError ? (
          <Alert
            type="error"
            showIcon
            message="No se pudo cargar el avance"
            description={avance.error instanceof ApiError ? avance.error.message : 'Error'}
          />
        ) : (
          <CredixDataTable
            rowKey={(r) => `${r.usuarioId}-${r.tipoCartera}`}
            loading={avance.isLoading}
            columns={avanceColumns}
            dataSource={avanceFiltrado}
            pagination={{ pageSize: 25, showSizeChanger: true }}
            size="small"
            scroll={{ x: 1600 }}
          />
        )}
      </CredixPanel>

      <CredixPanel
        title={`Metas definitivas (${metasFiltradas.length})`}
        style={{ marginTop: 16 }}
        extra={
          metas.data?.periodoCerrado ? (
            <Tag color="default">Periodo cerrado</Tag>
          ) : puedeEditarMetas ? (
            <Tag color="processing">Editable hasta {metas.data?.fechaLimiteEdicion}</Tag>
          ) : (
            <Tag>Solo lectura</Tag>
          )
        }
      >
        {metas.isError ? (
          <Alert
            type="error"
            showIcon
            message="No se pudieron cargar las metas"
            description={metas.error instanceof ApiError ? metas.error.message : 'Error'}
          />
        ) : (
          <CredixDataTable
            rowKey={(r) => `${r.usuarioId}-${r.tipoCartera}`}
            loading={metas.isLoading}
            columns={metasColumns}
            dataSource={metasFiltradas}
            pagination={false}
            size="small"
            scroll={{ x: 1400 }}
          />
        )}
      </CredixPanel>
    </CredixPage>
  )
}
