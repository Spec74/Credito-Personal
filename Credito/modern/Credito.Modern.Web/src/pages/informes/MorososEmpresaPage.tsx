import { useDeferredValue, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  AutoComplete,
  Button,
  DatePicker,
  Empty,
  Input,
  Modal,
  Select,
  Skeleton,
  Space,
  Tooltip,
  message,
} from 'antd'
import {
  CheckCircleOutlined,
  ClearOutlined,
  DownloadOutlined,
  PauseCircleOutlined,
  ReloadOutlined,
  SearchOutlined,
  StopOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadMorosidadExcel,
  fetchMorosidadEmpresa,
  fetchMorosidadPermisos,
  type MorosidadEmpresaItem,
  type MorosidadTipo,
} from '../../api/morosidadEmpresa'
import { ApiError } from '../../api/errors'
import {
  CredixDataTable,
  CredixDatePicker,
  CredixFilterBar,
  CredixPage,
  CredixPanel,
} from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import '../../styles/morosos-empresa.css'

type OrdenKey = 'DIAS_DESC' | 'SALDO_DESC' | 'NOMBRE_ASC'

const PAGE_SIZE = 25

function formatFecha(valor: string | null | undefined): string {
  if (!valor) return '—'
  const d = dayjs(valor)
  return d.isValid() ? d.format('DD/MM/YYYY') : '—'
}

function etiquetaGestor(item: Pick<MorosidadEmpresaItem, 'gestorUsuario' | 'gestorNombre'>): string {
  const usuario = (item.gestorUsuario ?? '').trim()
  const nombre = (item.gestorNombre ?? '').trim()
  if (!usuario && !nombre) return '—'
  if (nombre && nombre !== usuario) return `${usuario || '—'} · ${nombre}`
  return usuario || nombre
}

function claseSituacion(codigo: string): string {
  if (codigo === 'NUNCA_PAGO') return 'morosos-badge morosos-badge--red'
  if (codigo === 'DEJO_PAGAR') return 'morosos-badge morosos-badge--amber'
  return 'morosos-badge morosos-badge--green'
}

function resumenTipo(filas: MorosidadEmpresaItem[], codigo: string) {
  let cantidad = 0
  let monto = 0
  for (const f of filas) {
    if (f.codigoClasificacion !== codigo) continue
    cantidad += 1
    monto += Number(f.saldoMora) || 0
  }
  return { cantidad, monto }
}

export function MorososEmpresaPage() {
  const [fechaCorte, setFechaCorte] = useState<Dayjs>(() => dayjs())
  const [fechaCorteAplicada, setFechaCorteAplicada] = useState(() => dayjs().format('YYYY-MM-DD'))
  const [tipoActivo, setTipoActivo] = useState<MorosidadTipo>('TODOS')
  const [buscar, setBuscar] = useState('')
  const buscarDeferred = useDeferredValue(buscar)
  const [oficinaFiltro, setOficinaFiltro] = useState<string>('')
  const [gestorFiltro, setGestorFiltro] = useState('')
  const [orden, setOrden] = useState<OrdenKey>('DIAS_DESC')
  const [pagina, setPagina] = useState(1)
  const [exportOpen, setExportOpen] = useState(false)
  const [exportInicio, setExportInicio] = useState<Dayjs | null>(null)
  const [exportFin, setExportFin] = useState<Dayjs | null>(null)

  const permisos = useQuery({
    queryKey: ['morosidad-permisos'],
    queryFn: fetchMorosidadPermisos,
    staleTime: 5 * 60_000,
  })

  const dataQuery = useQuery({
    queryKey: ['morosidad-empresa', fechaCorteAplicada],
    queryFn: () => fetchMorosidadEmpresa({ fechaCorte: fechaCorteAplicada }),
    enabled: permisos.data?.puedeConsultar === true,
    staleTime: 60_000,
  })

  const excel = useMutation({
    mutationFn: async () => {
      if (!exportInicio || !exportFin) {
        throw new Error('Seleccione ambas fechas.')
      }
      if (exportInicio.isAfter(exportFin, 'day')) {
        throw new Error('La fecha de inicio no puede ser posterior a la fecha final.')
      }
      await downloadMorosidadExcel(
        exportInicio.format('YYYY-MM-DD'),
        exportFin.format('YYYY-MM-DD'),
      )
    },
    onSuccess: () => {
      message.success('Excel descargado')
      setExportOpen(false)
    },
    onError: (err: unknown) => {
      const msg =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : 'No se pudo generar el Excel de morosidad.'
      message.error(msg)
    },
  })

  const filas = dataQuery.data?.filas ?? []

  const oficinasOptions = useMemo(() => {
    const map = new Map<string, string>()
    for (const f of filas) {
      const key = f.oficinaId == null ? 'VARIAS' : String(f.oficinaId)
      map.set(key, f.oficina || 'Sin oficina')
    }
    return [...map.entries()]
      .sort((a, b) => a[1].localeCompare(b[1], 'es'))
      .map(([value, label]) => ({ value, label }))
  }, [filas])

  const gestoresOptions = useMemo(() => {
    const set = new Set<string>()
    for (const f of filas) {
      const label = etiquetaGestor(f)
      if (label !== '—') set.add(label)
    }
    return [...set].sort((a, b) => a.localeCompare(b, 'es')).map((v) => ({ value: v }))
  }, [filas])

  const baseFiltrada = useMemo(() => {
    const q = buscarDeferred.trim().toUpperCase()
    const gestorQ = gestorFiltro.trim().toUpperCase()

    return filas.filter((item) => {
      if (q) {
        const haystack = [item.nombreCompleto, item.numeroDocumento, item.celular]
          .filter(Boolean)
          .join(' ')
          .toUpperCase()
        if (!haystack.includes(q)) return false
      }

      if (oficinaFiltro) {
        if (oficinaFiltro === 'VARIAS') {
          if (item.oficinaId != null) return false
        } else if (String(item.oficinaId ?? '') !== oficinaFiltro) {
          return false
        }
      }

      if (gestorQ) {
        const responsable = etiquetaGestor(item).toUpperCase()
        if (!responsable.includes(gestorQ)) return false
      }

      return true
    })
  }, [filas, buscarDeferred, oficinaFiltro, gestorFiltro])

  const kpis = useMemo(() => {
    let saldo = 0
    let creditos = 0
    for (const f of baseFiltrada) {
      saldo += Number(f.saldoMora) || 0
      creditos += Number(f.creditosMora) || 0
    }
    return {
      clientes: baseFiltrada.length,
      creditos,
      saldo,
      nunca: resumenTipo(baseFiltrada, 'NUNCA_PAGO'),
      dejo: resumenTipo(baseFiltrada, 'DEJO_PAGAR'),
      atraso: resumenTipo(baseFiltrada, 'PAGA_CON_ATRASO'),
    }
  }, [baseFiltrada])

  const filtradas = useMemo(() => {
    const rows =
      tipoActivo === 'TODOS'
        ? [...baseFiltrada]
        : baseFiltrada.filter((f) => f.codigoClasificacion === tipoActivo)

    rows.sort((a, b) => {
      if (orden === 'SALDO_DESC') return (b.saldoMora || 0) - (a.saldoMora || 0)
      if (orden === 'NOMBRE_ASC') {
        return (a.nombreCompleto || '').localeCompare(b.nombreCompleto || '', 'es')
      }
      return (b.diasAtraso || 0) - (a.diasAtraso || 0)
    })
    return rows
  }, [baseFiltrada, tipoActivo, orden])

  const columns: ColumnsType<MorosidadEmpresaItem> = useMemo(
    () => [
      {
        title: 'Cliente',
        key: 'cliente',
        width: 240,
        render: (_, r) => (
          <div className="morosos-client">
            <strong>{r.nombreCompleto || 'SIN NOMBRE'}</strong>
            <span>
              DNI: {r.numeroDocumento || '—'}
              {r.celular ? ` · Cel: ${r.celular}` : ''}
            </span>
          </div>
        ),
      },
      {
        title: 'Situación',
        key: 'situacion',
        width: 160,
        render: (_, r) => (
          <div className="morosos-situacion">
            <span className={claseSituacion(r.codigoClasificacion)}>
              {r.clasificacion || r.codigoClasificacion}
            </span>
            <span className="morosos-cell-meta">{r.diasAtraso} días de atraso</span>
          </div>
        ),
      },
      {
        title: 'Oficina / analista',
        key: 'owner',
        width: 220,
        render: (_, r) => (
          <div className="morosos-owner">
            <strong>{r.oficina || '—'}</strong>
            <Tooltip title={etiquetaGestor(r)}>
              <span>{etiquetaGestor(r)}</span>
            </Tooltip>
          </div>
        ),
      },
      {
        title: 'Saldo / créditos',
        key: 'saldo',
        width: 140,
        align: 'right',
        render: (_, r) => (
          <div className="morosos-balance">
            <strong className="morosos-money">S/ {formatMoney(r.saldoMora)}</strong>
            <span className="morosos-cell-meta">
              {r.creditosMora} {r.creditosMora === 1 ? 'crédito' : 'créditos'}
            </span>
          </div>
        ),
      },
      {
        title: 'Fechas de mora',
        key: 'fechas',
        width: 170,
        render: (_, r) => (
          <div className="morosos-dates">
            <span>
              <b>Venció:</b> {formatFecha(r.primeraCuotaVencida)}
            </span>
            <span>
              <b>Últ. pago:</b> {formatFecha(r.fechaUltimoPago)}
            </span>
          </div>
        ),
      },
      {
        title: 'Acción',
        key: 'accion',
        width: 110,
        fixed: 'right',
        render: (_, r) => (
          <Link
            className="morosos-profile"
            to={`/credito/persona/${r.personaId}`}
            title="Abrir créditos del cliente"
          >
            Ver cliente
          </Link>
        ),
      },
    ],
    [],
  )

  const breadcrumb = [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/informes">Informes</Link> },
    { title: 'Morosos' },
  ]

  const seleccionarTipo = (tipo: MorosidadTipo) => {
    setTipoActivo((prev) => (prev === tipo ? 'TODOS' : tipo))
    setPagina(1)
  }

  const limpiarFiltros = () => {
    setTipoActivo('TODOS')
    setBuscar('')
    setOficinaFiltro('')
    setGestorFiltro('')
    setOrden('DIAS_DESC')
    setPagina(1)
  }

  const abrirExportacion = () => {
    const corte = dayjs(fechaCorteAplicada)
    setExportFin(corte)
    setExportInicio(corte.startOf('month'))
    setExportOpen(true)
  }

  if (permisos.isLoading) {
    return (
      <CredixPage className="morosos-empresa-page" title="Morosidad empresarial" breadcrumb={breadcrumb}>
        <div className="morosos-skeleton">
          <Skeleton active paragraph={{ rows: 2 }} />
          <Skeleton active paragraph={{ rows: 4 }} />
        </div>
      </CredixPage>
    )
  }

  if (permisos.isError || permisos.data?.puedeConsultar === false) {
    return (
      <CredixPage className="morosos-empresa-page" title="Morosidad empresarial" breadcrumb={breadcrumb}>
        <Alert
          type="warning"
          showIcon
          message="Sin permiso de consulta"
          description={
            permisos.error instanceof ApiError
              ? permisos.error.message
              : 'Su usuario no está autorizado para consultar la morosidad empresarial.'
          }
        />
      </CredixPage>
    )
  }

  return (
    <CredixPage
      className="morosos-empresa-page"
      title="Morosidad empresarial"
      subtitle="Todos los clientes morosos de la empresa, clasificados según su comportamiento de pago (nunca pagó, dejó de pagar, paga con atraso)."
      breadcrumb={breadcrumb}
      actions={
        <Space wrap>
          <CredixDatePicker
            value={fechaCorte}
            onChange={(d) => d && setFechaCorte(d)}
            allowClear={false}
            format="DD/MM/YYYY"
            aria-label="Fecha de corte"
            style={{ width: 150, maxWidth: 150 }}
          />
          <Button
            icon={<ReloadOutlined />}
            loading={dataQuery.isFetching}
            onClick={() => {
              setFechaCorteAplicada(fechaCorte.format('YYYY-MM-DD'))
              setPagina(1)
            }}
          >
            Actualizar
          </Button>
          <Button type="primary" icon={<DownloadOutlined />} onClick={abrirExportacion}>
            Exportar Excel
          </Button>
        </Space>
      }
    >
      {dataQuery.isError ? (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 16 }}
          message="No se pudo cargar la morosidad"
          description={
            dataQuery.error instanceof ApiError
              ? dataQuery.error.message
              : 'Revise la conexión e intente de nuevo.'
          }
          action={
            <Button size="small" onClick={() => void dataQuery.refetch()}>
              Reintentar
            </Button>
          }
        />
      ) : null}

      <section className="morosos-overview" aria-label="Resumen de morosidad">
        <div className="morosos-total-card">
          <span className="morosos-total-label">Saldo total en mora</span>
          <strong>S/ {formatMoney(kpis.saldo)}</strong>
          <span>
            {kpis.clientes.toLocaleString('es-PE')} clientes ·{' '}
            {kpis.creditos.toLocaleString('es-PE')} créditos
          </span>
          {dataQuery.data?.fechaCorte ? (
            <span className="morosos-total-corte">
              Corte {dayjs(dataQuery.data.fechaCorte).format('DD/MM/YYYY')}
            </span>
          ) : null}
        </div>

        <button
          type="button"
          className={`morosos-status-card morosos-status-card--red${
            tipoActivo === 'NUNCA_PAGO' ? ' is-active' : ''
          }`}
          aria-pressed={tipoActivo === 'NUNCA_PAGO'}
          onClick={() => seleccionarTipo('NUNCA_PAGO')}
        >
          <StopOutlined className="morosos-status-icon" aria-hidden />
          <span className="morosos-status-copy">
            <span className="morosos-status-label">Nunca pagaron</span>
            <strong>{kpis.nunca.cantidad}</strong>
            <small>S/ {formatMoney(kpis.nunca.monto)}</small>
          </span>
        </button>

        <button
          type="button"
          className={`morosos-status-card morosos-status-card--amber${
            tipoActivo === 'DEJO_PAGAR' ? ' is-active' : ''
          }`}
          aria-pressed={tipoActivo === 'DEJO_PAGAR'}
          onClick={() => seleccionarTipo('DEJO_PAGAR')}
        >
          <PauseCircleOutlined className="morosos-status-icon" aria-hidden />
          <span className="morosos-status-copy">
            <span className="morosos-status-label">Dejaron de pagar</span>
            <strong>{kpis.dejo.cantidad}</strong>
            <small>S/ {formatMoney(kpis.dejo.monto)}</small>
          </span>
        </button>

        <button
          type="button"
          className={`morosos-status-card morosos-status-card--green${
            tipoActivo === 'PAGA_CON_ATRASO' ? ' is-active' : ''
          }`}
          aria-pressed={tipoActivo === 'PAGA_CON_ATRASO'}
          onClick={() => seleccionarTipo('PAGA_CON_ATRASO')}
        >
          <CheckCircleOutlined className="morosos-status-icon" aria-hidden />
          <span className="morosos-status-copy">
            <span className="morosos-status-label">Pagan con atraso</span>
            <strong>{kpis.atraso.cantidad}</strong>
            <small>S/ {formatMoney(kpis.atraso.monto)}</small>
          </span>
        </button>
      </section>

      <CredixPanel
        title="Clientes morosos"
        extra={
          <Button type="link" icon={<ClearOutlined />} onClick={limpiarFiltros}>
            Limpiar filtros
          </Button>
        }
      >
        <p className="morosos-panel-hint">
          {filtradas.length.toLocaleString('es-PE')} cliente
          {filtradas.length === 1 ? '' : 's'} encontrado
          {filtradas.length === 1 ? '' : 's'}
          {tipoActivo !== 'TODOS' ? ' · filtro de semáforo activo' : ''}
        </p>

        <CredixFilterBar>
          <Input
            allowClear
            prefix={<SearchOutlined />}
            placeholder="Nombre, DNI o celular"
            value={buscar}
            onChange={(e) => {
              setBuscar(e.target.value)
              setPagina(1)
            }}
            style={{ minWidth: 220, flex: '1 1 14rem' }}
          />
          <Select
            allowClear
            placeholder="Todas las oficinas"
            value={oficinaFiltro || undefined}
            options={oficinasOptions}
            onChange={(v) => {
              setOficinaFiltro(v ?? '')
              setPagina(1)
            }}
            style={{ minWidth: 180 }}
          />
          <AutoComplete
            allowClear
            options={gestoresOptions}
            value={gestorFiltro}
            onChange={(v) => {
              setGestorFiltro(v)
              setPagina(1)
            }}
            placeholder="Todos los analistas"
            filterOption={(input, option) =>
              (option?.value ?? '').toUpperCase().includes(input.toUpperCase())
            }
            style={{ minWidth: 220 }}
          />
          <Select
            value={orden}
            onChange={(v) => {
              setOrden(v)
              setPagina(1)
            }}
            options={[
              { value: 'DIAS_DESC', label: 'Mayor atraso' },
              { value: 'SALDO_DESC', label: 'Mayor saldo' },
              { value: 'NOMBRE_ASC', label: 'Nombre del cliente' },
            ]}
            style={{ minWidth: 160 }}
          />
        </CredixFilterBar>

        {dataQuery.isLoading ? (
          <Skeleton active paragraph={{ rows: 8 }} />
        ) : (
          <CredixDataTable<MorosidadEmpresaItem>
            className="morosos-table"
            rowKey={(r) => `${r.personaId}-${r.gestorId ?? 0}-${r.codigoClasificacion}`}
            columns={columns}
            dataSource={filtradas}
            loading={dataQuery.isFetching}
            scroll={{ x: 1100 }}
            pagination={{
              current: pagina,
              pageSize: PAGE_SIZE,
              total: filtradas.length,
              showSizeChanger: false,
              showTotal: (total, range) =>
                total === 0 ? '0–0 de 0' : `${range[0]}–${range[1]} de ${total}`,
              onChange: (p) => setPagina(p),
            }}
            locale={{
              emptyText: (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description="No encontramos clientes con los filtros actuales"
                />
              ),
            }}
            rowClassName={(r) =>
              r.codigoClasificacion === 'NUNCA_PAGO'
                ? 'morosos-row--red'
                : r.codigoClasificacion === 'DEJO_PAGAR'
                  ? 'morosos-row--amber'
                  : 'morosos-row--green'
            }
          />
        )}
      </CredixPanel>

      <Modal
        title="Exportar clientes morosos"
        open={exportOpen}
        onCancel={() => setExportOpen(false)}
        okText="Descargar Excel"
        cancelText="Cancelar"
        confirmLoading={excel.isPending}
        onOk={() => void excel.mutateAsync()}
        destroyOnClose
      >
        <p className="morosos-export-desc">
          El archivo incluirá clientes cuya <strong>primera cuota vencida</strong> esté dentro del
          rango. El semáforo se calculará a la fecha final.
        </p>
        <div className="morosos-export-dates">
          <label>
            Fecha de inicio
            <DatePicker
              value={exportInicio}
              onChange={(d) => setExportInicio(d)}
              format="DD/MM/YYYY"
              style={{ width: '100%' }}
            />
          </label>
          <label>
            Fecha final
            <DatePicker
              value={exportFin}
              onChange={(d) => setExportFin(d)}
              format="DD/MM/YYYY"
              style={{ width: '100%' }}
            />
          </label>
        </div>
      </Modal>
    </CredixPage>
  )
}
