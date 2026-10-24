import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Button, Input } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadMovimientoBovedaTicketPdf,
  downloadRptMovimientoBovedaPdf,
  fetchRptMovimientoBoveda,
  listarBovedasHistorial,
  type BovedaListadoRow,
  type RptMovimientoBovedaRow,
} from '../../../api/boveda'
import { fetchSaldosCajaDiarioBoveda, type SaldoCajaSesionRow } from '../../../api/saldosCaja'
import { CredixDataTable } from '../../../components/credix'
import { useSecondaryDataReady } from '../../../hooks/useSecondaryDataReady'
import { formatFecha } from '../../../utils/formatFecha'
import { formatMoney } from '../../../utils/formatMoney'
import { matchesSearchText } from '../../../utils/normalizeSearch'
import { TipoPagoCell } from './TipoPagoBrand'

type Props = {
  oficinaId: number
  bovedaAbiertaId?: number
}

type HistorialSection = 'movimientos' | 'cierres' | 'cajas'

const CAJAS_PAGE_SIZE = 20
const CIERRES_PAGE_SIZE = 20

const SECTIONS: { key: HistorialSection; label: string }[] = [
  { key: 'movimientos', label: 'Movimientos' },
  { key: 'cierres', label: 'Cierres' },
  { key: 'cajas', label: 'Cajas' },
]

function haystackMov(row: RptMovimientoBovedaRow): string {
  return [
    row.movimientoBovedaId,
    formatFecha(row.fechaReg),
    row.codOperacion,
    row.glosa,
    row.tipoPago,
    row.agente,
    row.entrada,
    row.salida,
  ]
    .filter((v) => v != null && v !== '')
    .join(' ')
}

function haystackCierre(row: BovedaListadoRow): string {
  return [
    row.bovedaId,
    row.tipo,
    formatFecha(row.fechaIniOperacion),
    row.fechaFinOperacion ? formatFecha(row.fechaFinOperacion) : '',
    row.indCierre ? 'cerrada si' : 'abierta no',
    row.saldoInicial,
    row.saldoFinal,
  ]
    .filter((v) => v != null && v !== '')
    .join(' ')
}

function haystackCaja(row: SaldoCajaSesionRow): string {
  return [
    row.id,
    row.caja,
    row.usuario,
    row.indCierre ? 'cerrado si' : 'abierto no',
    row.transBoveda ? 'boveda si' : '',
    formatFecha(row.fechaIniOperacion),
    row.fechaFinOperacion ? formatFecha(row.fechaFinOperacion) : '',
    row.saldoInicial,
    row.saldoFinal,
  ]
    .filter((v) => v != null && v !== '')
    .join(' ')
}

export function BovedaHistorialGrillas({ oficinaId, bovedaAbiertaId }: Props) {
  const [section, setSection] = useState<HistorialSection>('movimientos')
  const [query, setQuery] = useState('')
  const [page, setPage] = useState(1)
  const [cajasPage, setCajasPage] = useState(1)
  const [selectedId, setSelectedId] = useState<number | undefined>(bovedaAbiertaId)
  const secondaryReady = useSecondaryDataReady(oficinaId > 0)

  const historial = useQuery({
    queryKey: ['boveda-listar', oficinaId, page, CIERRES_PAGE_SIZE],
    queryFn: () => listarBovedasHistorial(oficinaId, page, CIERRES_PAGE_SIZE),
    enabled: oficinaId > 0 && secondaryReady,
  })

  const bovedaId = selectedId ?? historial.data?.rows[0]?.bovedaId

  const movimientos = useQuery({
    queryKey: ['rpt-movimiento-boveda', bovedaId],
    queryFn: () => fetchRptMovimientoBoveda(bovedaId!),
    enabled: !!bovedaId && secondaryReady,
  })

  const cajas = useQuery({
    queryKey: ['saldos-caja-diario-boveda', oficinaId, bovedaId, cajasPage],
    queryFn: () =>
      fetchSaldosCajaDiarioBoveda(oficinaId, bovedaId!, {
        page: cajasPage,
        pageSize: CAJAS_PAGE_SIZE,
      }),
    enabled: oficinaId > 0 && !!bovedaId && secondaryReady,
    placeholderData: keepPreviousData,
  })

  const cierres = useMemo(() => {
    const rows = historial.data?.rows ?? []
    return rows.filter((row) => matchesSearchText(haystackCierre(row), query))
  }, [historial.data?.rows, query])

  const movs = useMemo(() => {
    const rows = movimientos.data ?? []
    return rows.filter((row) => matchesSearchText(haystackMov(row), query))
  }, [movimientos.data, query])

  const cajasFiltradas = useMemo(() => {
    const rows = cajas.data?.rows ?? []
    return rows.filter((row) => matchesSearchText(haystackCaja(row), query))
  }, [cajas.data?.rows, query])

  const colsBoveda: ColumnsType<BovedaListadoRow> = [
    { title: 'Id', dataIndex: 'bovedaId', width: 70 },
    { title: 'Tipo', dataIndex: 'tipo', width: 72 },
    { title: 'Saldo ini.', dataIndex: 'saldoInicial', render: formatMoney, align: 'right' },
    { title: 'Entradas', dataIndex: 'entradas', render: formatMoney, align: 'right' },
    { title: 'Salidas', dataIndex: 'salidas', render: formatMoney, align: 'right' },
    { title: 'Saldo final', dataIndex: 'saldoFinal', render: formatMoney, align: 'right' },
    {
      title: 'Inicio',
      dataIndex: 'fechaIniOperacion',
      width: 110,
      render: (v: string) => formatFecha(v),
    },
    {
      title: 'Fin',
      dataIndex: 'fechaFinOperacion',
      width: 110,
      render: (v: string | null) => (v ? formatFecha(v) : '—'),
    },
    {
      title: 'Cierre',
      dataIndex: 'indCierre',
      width: 70,
      render: (v: boolean) => (v ? 'Sí' : 'No'),
    },
    {
      title: '',
      key: 'reporte',
      width: 100,
      fixed: 'right',
      render: (_, row) => (
        <Button
          size="small"
          type="link"
          onClick={(e) => {
            e.stopPropagation()
            void downloadRptMovimientoBovedaPdf(row.bovedaId)
          }}
        >
          Reporte
        </Button>
      ),
    },
  ]

  const colsMov: ColumnsType<RptMovimientoBovedaRow> = [
    { title: 'Id', dataIndex: 'movimientoBovedaId', width: 70 },
    { title: 'Fecha', dataIndex: 'fechaReg', width: 100, render: formatFecha },
    { title: 'Op.', dataIndex: 'codOperacion', width: 70 },
    {
      title: 'Tipo pago',
      dataIndex: 'tipoPago',
      width: 140,
      render: (v: string) => <TipoPagoCell tipoPago={v} />,
    },
    { title: 'Glosa', dataIndex: 'glosa', ellipsis: true },
    { title: 'Entrada', dataIndex: 'entrada', align: 'right', render: (v) => formatMoney(v ?? 0) },
    { title: 'Salida', dataIndex: 'salida', align: 'right', render: (v) => formatMoney(v ?? 0) },
    {
      title: '',
      key: 'pdf',
      width: 80,
      render: (_, row) => (
        <Button
          size="small"
          onClick={(e) => {
            e.stopPropagation()
            void downloadMovimientoBovedaTicketPdf(row.movimientoBovedaId)
          }}
        >
          Ticket
        </Button>
      ),
    },
  ]

  const colsCaja: ColumnsType<SaldoCajaSesionRow> = [
    { title: 'Nro', dataIndex: 'id', width: 70 },
    { title: 'Caja', dataIndex: 'caja', ellipsis: true },
    { title: 'Responsable', dataIndex: 'usuario', ellipsis: true },
    { title: 'Saldo ini.', dataIndex: 'saldoInicial', render: formatMoney, align: 'right' },
    { title: 'Saldo final', dataIndex: 'saldoFinal', render: formatMoney, align: 'right' },
    {
      title: 'Cerrado',
      dataIndex: 'indCierre',
      width: 70,
      render: (v: boolean) => (v ? 'Sí' : 'No'),
    },
    {
      title: 'En bóveda',
      dataIndex: 'transBoveda',
      width: 80,
      render: (v: boolean) => (v ? 'Sí' : 'No'),
    },
  ]

  const placeholder =
    section === 'movimientos'
      ? 'Glosa, operación, medio o agente…'
      : section === 'cierres'
        ? 'Nº de bóveda, tipo o fecha…'
        : 'Caja o responsable…'

  const filteredCount =
    section === 'movimientos'
      ? movs.length
      : section === 'cierres'
        ? cierres.length
        : cajasFiltradas.length
  const totalCount =
    section === 'movimientos'
      ? (movimientos.data?.length ?? 0)
      : section === 'cierres'
        ? (historial.data?.rows.length ?? 0)
        : (cajas.data?.rows.length ?? 0)

  return (
    <div className="boveda-historial">
      <div className="boveda-historial-toolbar">
        <nav className="boveda-historial-tabs" aria-label="Historial de bóveda" translate="no">
          {SECTIONS.map((item) => (
            <button
              key={item.key}
              type="button"
              className={
                item.key === section
                  ? 'boveda-historial-tabs__btn is-active'
                  : 'boveda-historial-tabs__btn'
              }
              aria-current={item.key === section ? 'true' : undefined}
              onClick={() => setSection(item.key)}
            >
              {item.label}
            </button>
          ))}
        </nav>
        <Input
          allowClear
          prefix={<SearchOutlined aria-hidden />}
          placeholder={placeholder}
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          className="boveda-historial-search"
          aria-label="Buscar en el historial"
        />
        {query.trim() ? (
          <span className="boveda-historial-count">
            {filteredCount} de {totalCount}
          </span>
        ) : null}
        {bovedaId ? (
          <Link
            to={`/tesoreria/movimiento-boveda?bovedaId=${bovedaId}`}
            className="boveda-historial-informe"
          >
            Informe de esta bóveda
          </Link>
        ) : null}
      </div>

      {section === 'cierres' ? (
        <CredixDataTable<BovedaListadoRow>
          mode="operacion"
          rowKey="bovedaId"
          size="small"
          columns={colsBoveda}
          dataSource={cierres}
          loading={historial.isLoading}
          rowSelection={{
            type: 'radio',
            selectedRowKeys: bovedaId ? [bovedaId] : [],
            onChange: (keys) => setSelectedId(keys[0] as number),
          }}
          onRow={(row) => ({
            onClick: () => setSelectedId(row.bovedaId),
          })}
          pagination={{
            current: page,
            pageSize: CIERRES_PAGE_SIZE,
            total: historial.data?.total ?? 0,
            showSizeChanger: false,
            onChange: setPage,
          }}
        />
      ) : null}

      {section === 'movimientos' ? (
        <CredixDataTable
          mode="operacion"
          rowKey="movimientoBovedaId"
          size="small"
          columns={colsMov}
          dataSource={movs}
          loading={movimientos.isLoading}
          pagination={{ pageSize: 10, showSizeChanger: true }}
          locale={{ emptyText: bovedaId ? 'Sin movimientos' : 'Elija una bóveda en Cierres' }}
        />
      ) : null}

      {section === 'cajas' ? (
        <CredixDataTable
          mode="operacion"
          rowKey="id"
          size="small"
          columns={colsCaja}
          dataSource={cajasFiltradas}
          loading={cajas.isFetching}
          pagination={{
            current: cajasPage,
            pageSize: CAJAS_PAGE_SIZE,
            total: cajas.data?.totalRecords ?? 0,
            showSizeChanger: false,
            onChange: setCajasPage,
          }}
          locale={{ emptyText: 'Sin cajas vinculadas' }}
        />
      ) : null}
    </div>
  )
}
