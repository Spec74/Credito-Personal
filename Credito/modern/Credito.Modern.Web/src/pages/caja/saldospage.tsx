import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import {
  Alert,
  Button,
  Modal,
  Space,
  Spin,
  Tabs,
  Tag,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { ExclamationCircleOutlined, FilePdfOutlined } from '@ant-design/icons'
import {
  actualizarDatosPostCierreBoveda,
  downloadRptMovimientoBovedaPdf,
  fetchBovedaAbierta,
  fetchValidarCierreCajaChica,
  fetchValidarCierreSaldos,
  transferirCierreCajaChica,
} from '../../api/boveda'
import { downloadRptSaldosCajaPdf } from '../../api/cajaDiario'
import {
  cerrarCajasDiarios,
  downloadCajasAsignadasPdf,
  fetchCajasAsignadas,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import {
  fetchSaldosCajaChicaDiario,
  fetchSaldosCajaDiario,
  fetchSaldosCajaDiarioBoveda,
} from '../../api/saldosCaja'
import { useAuth } from '../../auth/useAuth'
import { getLoginProfile } from '../../auth/sessionProfile'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { useResponsiveColumns } from '../../hooks/useResponsiveColumns'
import type { RptCajasAsignadasRow } from '../../types/api'
import {
  CredixDataTable,
  CredixPage,
  CredixPanel,
  CredixTotalsRow,
  CredixWideTable,
  type CredixStatItem,
  type CredixTotals,
} from '../../components/credix'
import {
  puedeAnularMovimientoCaja,
  puedeOperarCierreSaldos,
  esLecturaSaldoCaja,
} from '../../utils/cajaSaldosPermisos'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { filterTableRows } from '../../utils/tableClientFilter'
import { compararMonto, compararTexto } from '../../utils/tableSorters'
import { runOpenReport } from '../../utils/reportExport'
import { AnularMovimientoSaldosPanel } from './components/AnularMovimientoSaldosPanel'
import { AsignarCajaModal } from './components/asignarcajamodal'
import { ConteoBilletesModal } from './components/ConteoBilletesModal'
import { ResumenCuentaCaja } from './components/resumencuentacaja'
import { SaldosSesionTable } from './components/saldossesiontable'
import { SALDOS_ASIGNADAS_SCROLL } from './components/saldosTableLayout'
import { SaldosTableToolbar } from './components/SaldosTableToolbar'

const { Paragraph } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

function asignadaRowText(r: RptCajasAsignadasRow): string {
  return [r.cajaDiarioId, r.caja, r.cajero, r.modo, r.resumen].join(' ')
}

export function SaldosPage() {
  const { session } = useAuth()
  const queryClient = useQueryClient()
  const oficinaId = session?.oficinaId ?? 0
  const roles = session?.roles ?? []
  const lectura = esLecturaSaldoCaja(roles)
  const puedeOperar = puedeOperarCierreSaldos(roles)
  const puedeAnular = puedeAnularMovimientoCaja(roles)
  const oficinaLabel = getLoginProfile().oficinaLabel ?? `Oficina #${oficinaId}`

  const [tab, setTab] = useState('asignadas')
  const [filtro, setFiltro] = useState('')
  const [conteoOpen, setConteoOpen] = useState(false)
  const [asignarOpen, setAsignarOpen] = useState(false)
  // Los tabs de historial paginan en servidor: la búsqueda viaja con debounce.
  const busqueda = useDebouncedValue(filtro.trim(), 350)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)

  useEffect(() => {
    setPage(1)
  }, [tab, busqueda])

  const onPageChange = useCallback((nextPage: number, nextPageSize: number) => {
    setPage(nextPage)
    setPageSize(nextPageSize)
  }, [])

  const asignadas = useQuery({
    queryKey: ['cajas-asignadas', oficinaId],
    queryFn: () => fetchCajasAsignadas(oficinaId),
    enabled: oficinaId > 0 && (tab === 'asignadas' || tab === 'cierre' || conteoOpen),
  })

  const cajaDiario = useQuery({
    queryKey: ['saldos-caja-diario', oficinaId, busqueda, page, pageSize],
    queryFn: () => fetchSaldosCajaDiario(oficinaId, { buscar: busqueda, page, pageSize }),
    enabled: oficinaId > 0 && tab === 'caja-diario',
    placeholderData: keepPreviousData,
  })

  const cajaChica = useQuery({
    queryKey: ['saldos-caja-chica-diario', busqueda, page, pageSize],
    queryFn: () => fetchSaldosCajaChicaDiario({ buscar: busqueda, page, pageSize }),
    enabled: tab === 'caja-chica',
    placeholderData: keepPreviousData,
  })

  const boveda = useQuery({
    queryKey: ['boveda-abierta', oficinaId],
    queryFn: () => fetchBovedaAbierta(oficinaId),
    enabled: oficinaId > 0 && (tab === 'boveda' || tab === 'asignadas'),
    retry: false,
  })

  const bovedaSaldos = useQuery({
    queryKey: [
      'saldos-caja-diario-boveda',
      oficinaId,
      boveda.data?.bovedaId,
      busqueda,
      page,
      pageSize,
    ],
    queryFn: () =>
      fetchSaldosCajaDiarioBoveda(oficinaId, boveda.data!.bovedaId, {
        buscar: busqueda,
        page,
        pageSize,
      }),
    enabled:
      oficinaId > 0 && tab === 'boveda' && boveda.data != null && boveda.data.bovedaId > 0,
    placeholderData: keepPreviousData,
  })

  const validacion = useQuery({
    queryKey: ['validar-cierre-saldos', oficinaId],
    queryFn: () => fetchValidarCierreSaldos(oficinaId),
    enabled: oficinaId > 0 && tab === 'cierre' && puedeOperar,
  })

  const validacionChica = useQuery({
    queryKey: ['validar-cierre-caja-chica', oficinaId],
    queryFn: () => fetchValidarCierreCajaChica(oficinaId),
    enabled: oficinaId > 0 && tab === 'cierre' && puedeOperar,
  })

  const importeCierre = useMemo(() => {
    const rows = asignadas.data ?? []
    return rows.reduce((s, r) => s + (r.saldoFinal ?? 0), 0)
  }, [asignadas.data])

  const asignadasFiltradas = useMemo(
    () => filterTableRows(asignadas.data ?? [], filtro, asignadaRowText),
    [asignadas.data, filtro],
  )

  const totalesAsignadasRow = useMemo<CredixTotals>(() => {
    const rows = asignadasFiltradas
    const suma = (pick: (r: RptCajasAsignadasRow) => number | null | undefined) =>
      rows.reduce((s, r) => s + (pick(r) ?? 0), 0)
    return {
      cajaDiarioId: <strong>TOTAL</strong>,
      saldoInicial: formatMoney(suma((r) => r.saldoInicial)),
      entradas: formatMoney(suma((r) => r.entradas)),
      salidas: formatMoney(suma((r) => r.salidas)),
      saldoFinal: <strong>{formatMoney(suma((r) => r.saldoFinal))}</strong>,
    }
  }, [asignadasFiltradas])

  const cerrar = useMutation({
    mutationFn: (sobrante: number) => cerrarCajasDiarios({ oficinaId, sobrante }),
    onSuccess: async (r) => {
      message.success(`Cierre ejecutado (código ${r.resultCode})`)
      setConteoOpen(false)
      try {
        await actualizarDatosPostCierreBoveda(oficinaId)
      } catch {
        message.warning('Cierre OK; post-cierre bóveda no se completó.')
      }
      void queryClient.invalidateQueries({ queryKey: ['validar-cierre-saldos'] })
      void queryClient.invalidateQueries({ queryKey: ['saldos-caja-diario'] })
      void queryClient.invalidateQueries({ queryKey: ['cajas-asignadas'] })
    },
    onError: (err) => message.error(errMsg(err)),
  })

  const transferChica = useMutation({
    mutationFn: () => transferirCierreCajaChica(oficinaId),
    onSuccess: (r) => {
      message.success(`Caja chica transferida (${r.cajasTransferidas} sesión/es)`)
      void queryClient.invalidateQueries({ queryKey: ['validar-cierre-caja-chica'] })
      void queryClient.invalidateQueries({ queryKey: ['saldos-caja-chica-diario'] })
    },
    onError: (err) => message.error(errMsg(err)),
  })

  const postCierreBoveda = useMutation({
    mutationFn: () => actualizarDatosPostCierreBoveda(oficinaId),
    onSuccess: () => message.success('Datos post-cierre bóveda actualizados'),
    onError: (err) => message.error(errMsg(err)),
  })

  const imprimirSaldo = useCallback(async (id: number, cajaChica: boolean) => {
    try {
      await downloadRptSaldosCajaPdf(id, cajaChica)
    } catch (e) {
      message.error(e instanceof Error ? e.message : 'No se pudo abrir el reporte')
    }
  }, [])

  const onImprimirSaldo = useCallback(
    (id: number, cajaChica: boolean) => void imprimirSaldo(id, cajaChica),
    [imprimirSaldo],
  )

  const asignadasColumns: ColumnsType<RptCajasAsignadasRow> = useMemo(
    () => [
      {
        title: 'N°',
        dataIndex: 'cajaDiarioId',
        align: 'center',
        width: 76,
        sorter: (a, b) => a.cajaDiarioId - b.cajaDiarioId,
      },
      {
        title: 'Caja',
        dataIndex: 'caja',
        ellipsis: true,
        width: 150,
        sorter: (a, b) => compararTexto(a.caja, b.caja),
      },
      {
        title: 'Modo',
        dataIndex: 'modo',
        align: 'center',
        width: 108,
        responsive: ['md'],
        render: (v: string) =>
          v === 'ABIERTO' ? <Tag color="green">{v}</Tag> : <Tag>{v}</Tag>,
      },
      {
        title: 'Cajero',
        dataIndex: 'cajero',
        ellipsis: true,
        width: 190,
        sorter: (a, b) => compararTexto(a.cajero, b.cajero),
      },
      {
        title: 'Inicio',
        dataIndex: 'fechaIniOperacion',
        align: 'center',
        width: 128,
        render: formatFecha,
        sorter: (a, b) => compararTexto(a.fechaIniOperacion, b.fechaIniOperacion),
        responsive: ['lg'],
      },
      {
        title: 'Fin',
        dataIndex: 'fechaFinOperacion',
        align: 'center',
        width: 128,
        render: formatFecha,
        sorter: (a, b) => compararTexto(a.fechaFinOperacion, b.fechaFinOperacion),
        responsive: ['xl'],
      },
      {
        title: 'Saldo ini.',
        dataIndex: 'saldoInicial',
        align: 'right',
        width: 108,
        render: formatMoney,
        sorter: (a, b) => compararMonto(a.saldoInicial, b.saldoInicial),
        responsive: ['md'],
      },
      {
        title: 'Entradas',
        dataIndex: 'entradas',
        align: 'right',
        width: 108,
        render: formatMoney,
        sorter: (a, b) => compararMonto(a.entradas, b.entradas),
        responsive: ['lg'],
      },
      {
        title: 'Salidas',
        dataIndex: 'salidas',
        align: 'right',
        width: 108,
        render: formatMoney,
        sorter: (a, b) => compararMonto(a.salidas, b.salidas),
        responsive: ['lg'],
      },
      // Ancladas a la derecha: el dato que cierra la caja y su desglose quedan siempre visibles,
      // sin depender del scroll horizontal.
      {
        title: 'Saldo final',
        dataIndex: 'saldoFinal',
        align: 'right',
        width: 124,
        fixed: 'right',
        sorter: (a, b) => compararMonto(a.saldoFinal, b.saldoFinal),
        render: (v: number) => <strong>{formatMoney(v)}</strong>,
      },
      {
        title: '',
        key: 'resumen',
        align: 'center',
        width: 52,
        fixed: 'right',
        render: (_, row) => <ResumenCuentaCaja resumen={row.resumen} caja={row.caja} />,
      },
    ],
    [],
  )

  const asignadasVisibles = useResponsiveColumns(asignadasColumns)

  const refrescarTab = () => {
    if (tab === 'asignadas' || tab === 'cierre') void asignadas.refetch()
    else if (tab === 'caja-diario') void cajaDiario.refetch()
    else if (tab === 'caja-chica') void cajaChica.refetch()
    else if (tab === 'boveda') {
      void boveda.refetch()
      void bovedaSaldos.refetch()
    } else {
      void validacion.refetch()
      void validacionChica.refetch()
    }
  }

  const tabLoading =
    (tab === 'asignadas' && asignadas.isFetching) ||
    (tab === 'caja-diario' && cajaDiario.isFetching) ||
    (tab === 'caja-chica' && cajaChica.isFetching) ||
    (tab === 'boveda' && (boveda.isFetching || bovedaSaldos.isFetching)) ||
    (tab === 'cierre' &&
      (validacion.isFetching || validacionChica.isFetching || asignadas.isFetching))

  const saldosStats: CredixStatItem[] = useMemo(() => {
    if (tab === 'asignadas' || tab === 'cierre') {
      const rows = asignadas.data ?? []
      const abiertas = rows.filter((r) => !r.fechaFinOperacion).length
      return [
        { value: rows.length, label: 'Cajas asignadas' },
        { value: abiertas, label: 'En operación' },
        {
          value: formatMoney(importeCierre),
          label: 'Saldo final (cierre)',
          tone: 'red',
        },
      ]
    }
    if (tab === 'caja-diario') {
      return [
        { value: cajaDiario.data?.totalRecords ?? 0, label: 'Sesiones caja diario' },
        {
          value: formatMoney(cajaDiario.data?.totalSaldoFinal ?? 0),
          label: 'Saldo final acumulado',
        },
      ]
    }
    if (tab === 'caja-chica') {
      return [
        { value: cajaChica.data?.totalRecords ?? 0, label: 'Sesiones caja chica' },
        {
          value: formatMoney(cajaChica.data?.totalSaldoFinal ?? 0),
          label: 'Saldo final acumulado',
        },
      ]
    }
    if (tab === 'boveda' && boveda.data) {
      return [
        {
          value: boveda.data.indCierre ? 'CERRADA' : 'ABIERTA',
          label: 'Bóveda',
          detail: `#${boveda.data.bovedaId}`,
        },
        { value: bovedaSaldos.data?.totalRecords ?? 0, label: 'Sesiones en bóveda' },
      ]
    }
    if (tab === 'cierre' && puedeOperar) {
      const v = validacion.data
      const vc = validacionChica.data
      return [
        {
          value: v?.puedeCerrar ? 'Sí' : 'No',
          label: 'Cierre diario',
          tone: v?.puedeCerrar ? 'green' : 'red',
        },
        {
          value: vc?.puedeCerrar ? 'Sí' : 'No',
          label: 'Cierre chica',
          tone: vc?.puedeCerrar ? 'green' : 'red',
        },
      ]
    }
    return []
  }, [
    tab,
    asignadas.data,
    cajaDiario.data,
    cajaChica.data,
    boveda.data,
    bovedaSaldos.data,
    validacion.data,
    validacionChica.data,
    importeCierre,
    puedeOperar,
  ])

  const iniciarCierreMasivo = () => {
    if (importeCierre <= 0 && (asignadas.data?.length ?? 0) === 0) {
      message.warning('No hay cajas asignadas para cerrar.')
      return
    }
    setConteoOpen(true)
  }

  const confirmarTransferChica = () => {
    Modal.confirm({
      title: 'Transferir cierre caja chica',
      icon: <ExclamationCircleOutlined />,
      content: '¿Transferir el cierre de caja chica a bóveda?',
      okText: 'Transferir',
      cancelText: 'Cancelar',
      onOk: () => transferChica.mutateAsync(),
    })
  }

  /** Cajas asignadas ya está en memoria (conjunto del día): filtro instantáneo en cliente. */
  const toolbarAsignadas = (placeholder: string, total: number, filtered: number) => (
    <SaldosTableToolbar
      value={filtro}
      onChange={setFiltro}
      placeholder={placeholder}
      hint="La rueda del mouse desplaza las columnas. El saldo final queda fijo a la derecha."
      hintShort="Rueda: desplazar columnas."
      filteredCount={filtered}
      totalCount={total}
      loading={tabLoading}
      onRefresh={refrescarTab}
    />
  )

  /** Historial: la búsqueda va al servidor (el resultado está paginado). */
  const toolbarHistorial = (placeholder: string) => (
    <SaldosTableToolbar
      value={filtro}
      onChange={setFiltro}
      placeholder={placeholder}
      hint="Busca en el historial del servidor. La rueda del mouse desplaza las columnas."
      hintShort="Rueda: desplazar columnas."
      loading={tabLoading}
      onRefresh={refrescarTab}
    />
  )

  return (
    <CredixPage
      className="caja-saldos-page credix-page--stats-3"
      title="Saldos caja"
      subtitle={`${oficinaLabel} · cajas asignadas, saldos por sesión y cierre masivo.`}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: 'Saldos caja' },
      ]}
      stats={saldosStats}
      actions={
        puedeOperar ? (
          <Space wrap>
            <Button type="primary" onClick={() => setAsignarOpen(true)}>
              Asignar caja
            </Button>
            <Link to="/caja/chica">
              <Button>Operar caja chica</Button>
            </Link>
          </Space>
        ) : null
      }
    >
      <div className="caja-saldos-page__rol-badge">
        <Tag color={lectura ? 'gold' : 'blue'}>
          {lectura ? 'Vista ENCARGADO (solo lectura)' : 'Vista PRINCIPAL'}
        </Tag>
        {lectura ? (
          <span style={{ fontSize: 13, color: 'var(--credix-text-muted)' }}>
            Sin asignar ni cerrar cajas (paridad LECTURA_SALDO).
          </span>
        ) : null}
      </div>

      <Tabs
        className="credix-tabs"
        activeKey={tab}
        onChange={(k) => {
          setTab(k)
          setFiltro('')
        }}
        items={[
          {
            key: 'asignadas',
            label: 'Cajas asignadas',
            children: (
              <>
                {toolbarAsignadas(
                  'Caja, cajero, modo',
                  (asignadas.data ?? []).length,
                  asignadasFiltradas.length,
                )}
                <div className="caja-saldos-asignadas-actions">
                  <Button
                    icon={<FilePdfOutlined />}
                    onClick={() => {
                      void runOpenReport('Cajas asignadas', () =>
                        downloadCajasAsignadasPdf(oficinaId),
                      )
                    }}
                  >
                    Reporte cajas
                  </Button>
                  <Button
                    icon={<FilePdfOutlined />}
                    onClick={() => {
                      const id = boveda.data?.bovedaId
                      const openBoveda = (bovedaId: number) => {
                        void runOpenReport('Movimiento bóveda', () =>
                          downloadRptMovimientoBovedaPdf(bovedaId),
                        )
                      }
                      if (id) {
                        openBoveda(id)
                      } else {
                        void boveda.refetch().then((r) => {
                          if (r.data?.bovedaId) {
                            openBoveda(r.data.bovedaId)
                          } else {
                            message.warning('No hay bóveda abierta para el reporte.')
                          }
                        })
                      }
                    }}
                  >
                    Reporte bóveda
                  </Button>
                  {puedeOperar ? (
                    <Button type="primary" danger onClick={() => setTab('cierre')}>
                      Ir a cierre masivo
                    </Button>
                  ) : null}
                </div>
                {asignadas.isError ? (
                  <Alert type="error" showIcon message={errMsg(asignadas.error)} />
                ) : (
                  <CredixWideTable>
                    <CredixDataTable<RptCajasAsignadasRow>
                      mode="operacion"
                      className="caja-saldos-table"
                      rowKey="cajaDiarioId"
                      tableLayout="fixed"
                      scroll={SALDOS_ASIGNADAS_SCROLL}
                      loading={asignadas.isLoading}
                      dataSource={asignadasFiltradas}
                      columns={asignadasVisibles}
                      pagination={{ pageSize: 25 }}
                      locale={{
                        emptyText: filtro
                          ? 'Ninguna caja coincide con el filtro.'
                          : 'No hay cajas asignadas hoy en la oficina.',
                      }}
                      summary={
                        asignadasFiltradas.length
                          ? () => (
                              <CredixTotalsRow
                                columns={asignadasVisibles}
                                totals={totalesAsignadasRow}
                              />
                            )
                          : undefined
                      }
                    />
                  </CredixWideTable>
                )}
              </>
            ),
          },
          {
            key: 'caja-diario',
            label: 'Saldos caja diario',
            children: (
              <>
                {toolbarHistorial('Caja, responsable o N° de sesión')}
                {cajaDiario.isError ? (
                  <Alert type="error" showIcon message={errMsg(cajaDiario.error)} />
                ) : (
                  <SaldosSesionTable
                    pagina={cajaDiario.data}
                    loading={cajaDiario.isFetching}
                    esCajaChica={false}
                    page={page}
                    pageSize={pageSize}
                    onPageChange={onPageChange}
                    onImprimir={onImprimirSaldo}
                    emptyText={
                      busqueda
                        ? 'Ninguna sesión coincide con la búsqueda.'
                        : 'Sin sesiones de caja diario en la oficina.'
                    }
                  />
                )}
              </>
            ),
          },
          {
            key: 'caja-chica',
            label: 'Saldos caja chica',
            children: (
              <>
                {toolbarHistorial('Responsable o N° de sesión')}
                {cajaChica.isError ? (
                  <Alert type="error" showIcon message={errMsg(cajaChica.error)} />
                ) : (
                  <SaldosSesionTable
                    pagina={cajaChica.data}
                    loading={cajaChica.isFetching}
                    esCajaChica
                    page={page}
                    pageSize={pageSize}
                    onPageChange={onPageChange}
                    onImprimir={onImprimirSaldo}
                    emptyText={
                      busqueda
                        ? 'Ninguna sesión coincide con la búsqueda.'
                        : 'Sin sesiones de caja chica.'
                    }
                  />
                )}
              </>
            ),
          },
          {
            key: 'boveda',
            label: 'Saldos por bóveda',
            children: (
              <>
                {toolbarHistorial('Caja, responsable o N° de sesión')}
                {boveda.isError ? (
                  <Alert
                    type="warning"
                    showIcon
                    message="No hay bóveda abierta"
                    description={errMsg(boveda.error)}
                    style={{ marginBottom: 12 }}
                  />
                ) : boveda.data ? (
                  <div className="caja-saldos-boveda-banner">
                    Bóveda activa <strong>#{boveda.data.bovedaId}</strong> — saldo final{' '}
                    <strong>S/ {formatMoney(boveda.data.saldoFinal)}</strong>
                  </div>
                ) : null}
                {bovedaSaldos.isError ? (
                  <Alert type="error" showIcon message={errMsg(bovedaSaldos.error)} />
                ) : (
                  <SaldosSesionTable
                    pagina={bovedaSaldos.data}
                    loading={bovedaSaldos.isFetching}
                    esCajaChica={false}
                    page={page}
                    pageSize={pageSize}
                    onPageChange={onPageChange}
                    onImprimir={onImprimirSaldo}
                    emptyText={
                      busqueda
                        ? 'Ninguna sesión coincide con la búsqueda.'
                        : boveda.data
                          ? 'La bóveda abierta no tiene sesiones de caja.'
                          : 'Sin bóveda abierta en la oficina.'
                    }
                  />
                )}
              </>
            ),
          },
          {
            key: 'cierre',
            label: 'Cierre masivo',
            children: !puedeOperar ? (
              <Alert
                type="info"
                showIcon
                message="Modo solo lectura"
                description="Su rol no permite cerrar ni transferir cajas en esta pantalla."
              />
            ) : validacion.isLoading || validacionChica.isLoading ? (
              <CredixPanel>
                <Spin />
              </CredixPanel>
            ) : (
              <div className="caja-saldos-cierre-panel">
                {validacion.data ? (
                  <Alert
                    type={validacion.data.puedeCerrar ? 'success' : 'warning'}
                    showIcon
                    message={
                      validacion.data.puedeCerrar
                        ? 'Puede proceder al cierre masivo'
                        : 'Aún no puede cerrar saldos de caja diario'
                    }
                    description={validacion.data.mensaje}
                  />
                ) : null}
                {validacionChica.data ? (
                  <Alert
                    type={validacionChica.data.puedeCerrar ? 'success' : 'warning'}
                    showIcon
                    message={
                      validacionChica.data.puedeCerrar
                        ? 'Puede transferir cierre de caja chica'
                        : 'Aún no puede cerrar caja chica'
                    }
                    description={validacionChica.data.mensaje}
                  />
                ) : null}

                <CredixPanel title="Cerrar cajas diarias (CERRAR CAJAS)">
                  <Paragraph type="secondary">
                    Saldo final para conteo: <strong>S/ {formatMoney(importeCierre)}</strong>{' '}
                    (suma de cajas asignadas, paridad footer legacy).
                  </Paragraph>
                  <Button
                    type="primary"
                    danger
                    size="large"
                    disabled={!validacion.data?.puedeCerrar}
                    loading={cerrar.isPending}
                    onClick={iniciarCierreMasivo}
                  >
                    Cerrar y transferir a bóveda
                  </Button>
                </CredixPanel>

                {validacionChica.data?.puedeCerrar ? (
                  <CredixPanel title="Cierre caja chica">
                    <Button
                      type="primary"
                      loading={transferChica.isPending}
                      onClick={confirmarTransferChica}
                    >
                      CERRAR CAJA CHICA
                    </Button>
                  </CredixPanel>
                ) : null}

                <CredixPanel title="Post-cierre bóveda">
                  <Paragraph type="secondary">
                    Tras el cierre masivo el MVC actualiza cartera y calificación. También puede
                    ejecutarlo manualmente.
                  </Paragraph>
                  <Button
                    loading={postCierreBoveda.isPending}
                    onClick={() => postCierreBoveda.mutate()}
                  >
                    Actualizar datos post-cierre
                  </Button>
                </CredixPanel>
              </div>
            ),
          },
          ...(puedeAnular
            ? [
                {
                  key: 'anular',
                  label: 'Anular movimiento',
                  children: (
                    <AnularMovimientoSaldosPanel
                      oficinaId={oficinaId}
                      onAnulado={() => {
                        void cajaDiario.refetch()
                        void asignadas.refetch()
                      }}
                    />
                  ),
                },
              ]
            : []),
        ]}
      />

      <ConteoBilletesModal
        open={conteoOpen}
        importeCierre={importeCierre}
        loading={cerrar.isPending}
        onCancel={() => setConteoOpen(false)}
        onConfirm={async (sobrante) => {
          await cerrar.mutateAsync(sobrante)
        }}
      />

      <AsignarCajaModal
        open={asignarOpen}
        oficinaId={oficinaId}
        onClose={() => setAsignarOpen(false)}
        onSuccess={(r) => {
          message.success(
            r.esCajaChica
              ? 'Caja chica asignada.'
              : `Caja diario abierta${r.cajaDiarioId ? ` (ID ${r.cajaDiarioId})` : ''}.`,
          )
          setAsignarOpen(false)
          setTab('asignadas')
        }}
      />
    </CredixPage>
  )
}
