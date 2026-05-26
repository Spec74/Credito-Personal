import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Modal,
  Space,
  Spin,
  Table,
  Tabs,
  Tag,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  ExclamationCircleOutlined,
  FilePdfOutlined,
  PrinterOutlined,
} from '@ant-design/icons'
import {
  actualizarDatosPostCierreBoveda,
  fetchBovedaAbierta,
  fetchValidarCierreCajaChica,
  fetchValidarCierreSaldos,
  transferirCierreCajaChica,
} from '../../api/boveda'
import { downloadRptSaldosCajaPdf } from '../../api/cajaDiario'
import { cerrarCajasDiarios, fetchCajasAsignadas } from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import {
  fetchSaldosCajaChicaDiario,
  fetchSaldosCajaDiario,
  fetchSaldosCajaDiarioBoveda,
  type SaldoCajaSesionRow,
} from '../../api/saldosCaja'
import { useAuth } from '../../auth/useAuth'
import {
  openLegacyCajasAsignadas,
  openLegacyMovimientoBoveda,
  openLegacyReporteSaldoCaja,
} from '../../config/legacyReportUrls'
import type { RptCajasAsignadasRow } from '../../types/api'
import {
  CredixDataTable,
  CredixPage,
  CredixPanel,
  type CredixStatItem,
} from '../../components/credix'
import { puedeOperarCierreSaldos, esLecturaSaldoCaja } from '../../utils/cajaSaldosPermisos'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { filterTableRows } from '../../utils/tableClientFilter'
import { ConteoBilletesModal } from './components/ConteoBilletesModal'
import { SaldosTableToolbar } from './components/SaldosTableToolbar'

const { Paragraph } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

function saldoRowText(r: SaldoCajaSesionRow): string {
  return [r.id, r.caja, r.usuario, r.saldoInicial, r.saldoFinal].join(' ')
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

  const [tab, setTab] = useState('asignadas')
  const [filtro, setFiltro] = useState('')
  const [conteoOpen, setConteoOpen] = useState(false)

  const asignadas = useQuery({
    queryKey: ['cajas-asignadas', oficinaId],
    queryFn: () => fetchCajasAsignadas(oficinaId),
    enabled: oficinaId > 0 && (tab === 'asignadas' || tab === 'cierre' || conteoOpen),
  })

  const cajaDiario = useQuery({
    queryKey: ['saldos-caja-diario', oficinaId],
    queryFn: () => fetchSaldosCajaDiario(oficinaId),
    enabled: oficinaId > 0 && tab === 'caja-diario',
  })

  const cajaChica = useQuery({
    queryKey: ['saldos-caja-chica-diario'],
    queryFn: fetchSaldosCajaChicaDiario,
    enabled: tab === 'caja-chica',
  })

  const boveda = useQuery({
    queryKey: ['boveda-abierta', oficinaId],
    queryFn: () => fetchBovedaAbierta(oficinaId),
    enabled: oficinaId > 0 && (tab === 'boveda' || tab === 'asignadas'),
    retry: false,
  })

  const bovedaSaldos = useQuery({
    queryKey: ['saldos-caja-diario-boveda', oficinaId, boveda.data?.bovedaId],
    queryFn: () => fetchSaldosCajaDiarioBoveda(oficinaId, boveda.data!.bovedaId),
    enabled:
      oficinaId > 0 && tab === 'boveda' && boveda.data != null && boveda.data.bovedaId > 0,
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

  const cajaDiarioFiltradas = useMemo(
    () => filterTableRows(cajaDiario.data ?? [], filtro, saldoRowText),
    [cajaDiario.data, filtro],
  )

  const cajaChicaFiltradas = useMemo(
    () => filterTableRows(cajaChica.data ?? [], filtro, saldoRowText),
    [cajaChica.data, filtro],
  )

  const bovedaSaldosFiltradas = useMemo(
    () => filterTableRows(bovedaSaldos.data ?? [], filtro, saldoRowText),
    [bovedaSaldos.data, filtro],
  )

  const totalesAsignadas = useMemo(() => {
    const rows = asignadasFiltradas
    return {
      saldoInicial: rows.reduce((s, r) => s + (r.saldoInicial ?? 0), 0),
      entradas: rows.reduce((s, r) => s + (r.entradas ?? 0), 0),
      salidas: rows.reduce((s, r) => s + (r.salidas ?? 0), 0),
      saldoFinal: rows.reduce((s, r) => s + (r.saldoFinal ?? 0), 0),
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

  const imprimirSaldo = async (cajaDiarioId: number) => {
    try {
      await downloadRptSaldosCajaPdf(cajaDiarioId)
    } catch {
      try {
        openLegacyReporteSaldoCaja(cajaDiarioId)
      } catch (e) {
        message.error(e instanceof Error ? e.message : 'No se pudo abrir el reporte')
      }
    }
  }

  const saldoColumns: ColumnsType<SaldoCajaSesionRow> = useMemo(
    () => [
      { title: 'Nro', dataIndex: 'id', width: 72 },
      { title: 'Caja', dataIndex: 'caja', width: 140, ellipsis: true },
      { title: 'Responsable', dataIndex: 'usuario', width: 130, ellipsis: true },
      {
        title: 'Saldo ini.',
        dataIndex: 'saldoInicial',
        width: 100,
        align: 'right',
        render: formatMoney,
      },
      {
        title: 'Saldo final',
        dataIndex: 'saldoFinal',
        width: 100,
        align: 'right',
        render: formatMoney,
      },
      {
        title: 'Inicio',
        dataIndex: 'fechaIniOperacion',
        width: 100,
        render: formatFecha,
      },
      {
        title: 'Fin',
        dataIndex: 'fechaFinOperacion',
        width: 100,
        render: formatFecha,
      },
      {
        title: 'Cerrado',
        dataIndex: 'indCierre',
        width: 76,
        render: (v: boolean) => (v ? <Tag color="green">Sí</Tag> : <Tag>No</Tag>),
      },
      {
        title: 'Bóveda',
        dataIndex: 'transBoveda',
        width: 76,
        render: (v: boolean) => (v ? <Tag color="blue">Sí</Tag> : <Tag>No</Tag>),
      },
      {
        title: '',
        key: 'pdf',
        width: 52,
        fixed: 'right',
        render: (_, row) => (
          <Button
            type="text"
            size="small"
            icon={<PrinterOutlined />}
            aria-label="Imprimir saldo"
            onClick={() => void imprimirSaldo(row.id)}
          />
        ),
      },
    ],
    [],
  )

  const asignadasColumns: ColumnsType<RptCajasAsignadasRow> = useMemo(
    () => [
      { title: 'Id', dataIndex: 'cajaDiarioId', width: 72 },
      { title: 'Caja', dataIndex: 'caja', width: 120, ellipsis: true },
      { title: 'Modo', dataIndex: 'modo', width: 80 },
      { title: 'Cajero', dataIndex: 'cajero', width: 140, ellipsis: true },
      {
        title: 'Inicio',
        dataIndex: 'fechaIniOperacion',
        width: 100,
        render: formatFecha,
      },
      {
        title: 'Fin',
        dataIndex: 'fechaFinOperacion',
        width: 100,
        render: formatFecha,
      },
      {
        title: 'Saldo ini.',
        dataIndex: 'saldoInicial',
        width: 100,
        align: 'right',
        render: formatMoney,
      },
      {
        title: 'Entradas',
        dataIndex: 'entradas',
        width: 100,
        align: 'right',
        render: formatMoney,
      },
      {
        title: 'Salidas',
        dataIndex: 'salidas',
        width: 100,
        align: 'right',
        render: formatMoney,
      },
      {
        title: 'Saldo final',
        dataIndex: 'saldoFinal',
        width: 100,
        align: 'right',
        render: formatMoney,
      },
      { title: 'Resumen', dataIndex: 'resumen', ellipsis: true },
    ],
    [],
  )

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
      const rows = cajaDiario.data ?? []
      return [
        { value: rows.length, label: 'Sesiones caja diario' },
        {
          value: formatMoney(rows.reduce((s, r) => s + (r.saldoFinal ?? 0), 0)),
          label: 'Saldo final acumulado',
        },
      ]
    }
    if (tab === 'caja-chica') {
      return [{ value: (cajaChica.data ?? []).length, label: 'Sesiones caja chica' }]
    }
    if (tab === 'boveda' && boveda.data) {
      return [
        {
          value: boveda.data.indCierre ? 'CERRADA' : 'ABIERTA',
          label: 'Bóveda',
          detail: `#${boveda.data.bovedaId}`,
        },
        { value: (bovedaSaldos.data ?? []).length, label: 'Sesiones en bóveda' },
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

  const toolbarForTab = (placeholder: string, total: number, filtered: number) => (
    <SaldosTableToolbar
      value={filtro}
      onChange={setFiltro}
      placeholder={placeholder}
      filteredCount={filtered}
      totalCount={total}
      loading={tabLoading}
      onRefresh={refrescarTab}
    />
  )

  return (
    <CredixPage
      className="caja-saldos-page credix-page--stats-3"
      title="Saldos y cierres"
      subtitle="Lista de cajas asignadas, saldos por sesión y cierre masivo — misma lógica que Saldos/Index del MVC."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: 'Saldos y cierres' },
      ]}
      stats={saldosStats}
      actions={
        puedeOperar ? (
          <Space wrap>
            <Link to="/caja/asignar">
              <Button type="primary">Asignar caja</Button>
            </Link>
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
                {toolbarForTab(
                  'Caja, cajero, modo',
                  (asignadas.data ?? []).length,
                  asignadasFiltradas.length,
                )}
                {puedeOperar ? (
                  <div className="caja-saldos-asignadas-actions">
                    <Button icon={<FilePdfOutlined />} onClick={() => openLegacyCajasAsignadas()}>
                      Reporte cajas
                    </Button>
                    <Button
                      icon={<FilePdfOutlined />}
                      onClick={() => {
                        const id = boveda.data?.bovedaId
                        if (id) {
                          openLegacyMovimientoBoveda(id)
                        } else {
                          void boveda.refetch().then((r) => {
                            if (r.data?.bovedaId) {
                              openLegacyMovimientoBoveda(r.data.bovedaId)
                            } else {
                              message.warning('No hay bóveda abierta para el reporte.')
                            }
                          })
                        }
                      }}
                    >
                      Reporte bóveda
                    </Button>
                    <Button type="primary" danger onClick={() => setTab('cierre')}>
                      Ir a cierre masivo
                    </Button>
                  </div>
                ) : null}
                {asignadas.isError ? (
                  <Alert type="error" showIcon message={errMsg(asignadas.error)} />
                ) : (
                  <CredixDataTable<RptCajasAsignadasRow>
                    mode="operacion"
                    className="caja-saldos-table"
                    rowKey="cajaDiarioId"
                    size="small"
                    scroll={{ x: 'max-content' }}
                    loading={asignadas.isLoading}
                    dataSource={asignadasFiltradas}
                    columns={asignadasColumns}
                    pagination={{ pageSize: 25, showSizeChanger: true }}
                    summary={() => (
                      <Table.Summary fixed>
                        <Table.Summary.Row>
                          <Table.Summary.Cell index={0} colSpan={6}>
                            <strong>TOTAL CAJAS</strong>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={6} align="right">
                            {formatMoney(totalesAsignadas.saldoInicial)}
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={7} align="right">
                            {formatMoney(totalesAsignadas.entradas)}
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={8} align="right">
                            {formatMoney(totalesAsignadas.salidas)}
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={9} align="right">
                            <strong>{formatMoney(totalesAsignadas.saldoFinal)}</strong>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={10} />
                        </Table.Summary.Row>
                      </Table.Summary>
                    )}
                  />
                )}
              </>
            ),
          },
          {
            key: 'caja-diario',
            label: 'Saldos caja diario',
            children: (
              <>
                {toolbarForTab(
                  'Caja, responsable, nro',
                  (cajaDiario.data ?? []).length,
                  cajaDiarioFiltradas.length,
                )}
                {cajaDiario.isError ? (
                  <Alert type="error" showIcon message={errMsg(cajaDiario.error)} />
                ) : (
                  <CredixDataTable<SaldoCajaSesionRow>
                    mode="operacion"
                    className="caja-saldos-table"
                    rowKey="id"
                    size="small"
                    scroll={{ x: 'max-content' }}
                    loading={cajaDiario.isLoading}
                    dataSource={cajaDiarioFiltradas}
                    columns={saldoColumns}
                    pagination={{ pageSize: 25, showSizeChanger: true }}
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
                {toolbarForTab(
                  'Caja, responsable',
                  (cajaChica.data ?? []).length,
                  cajaChicaFiltradas.length,
                )}
                {cajaChica.isError ? (
                  <Alert type="error" showIcon message={errMsg(cajaChica.error)} />
                ) : (
                  <CredixDataTable<SaldoCajaSesionRow>
                    mode="operacion"
                    className="caja-saldos-table"
                    rowKey="id"
                    size="small"
                    scroll={{ x: 'max-content' }}
                    loading={cajaChica.isLoading}
                    dataSource={cajaChicaFiltradas}
                    columns={saldoColumns}
                    pagination={{ pageSize: 25, showSizeChanger: true }}
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
                {toolbarForTab(
                  'Filtrar sesiones',
                  (bovedaSaldos.data ?? []).length,
                  bovedaSaldosFiltradas.length,
                )}
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
                  <CredixDataTable<SaldoCajaSesionRow>
                    mode="operacion"
                    className="caja-saldos-table"
                    rowKey="id"
                    size="small"
                    scroll={{ x: 'max-content' }}
                    loading={bovedaSaldos.isLoading}
                    dataSource={bovedaSaldosFiltradas}
                    columns={saldoColumns}
                    pagination={{ pageSize: 25, showSizeChanger: true }}
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
    </CredixPage>
  )
}
