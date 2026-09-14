import { useState } from 'react'
import { Link } from 'react-router-dom'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Button, Space } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadMovimientoBovedaTicketPdf,
  fetchRptMovimientoBoveda,
  listarBovedasHistorial,
  type BovedaListadoRow,
} from '../../../api/boveda'
import { fetchSaldosCajaDiarioBoveda, type SaldoCajaSesionRow } from '../../../api/saldosCaja'
import { CredixDataTable } from '../../../components/credix'
import { formatFecha } from '../../../utils/formatFecha'
import { formatMoney } from '../../../utils/formatMoney'
import type { RptMovimientoBovedaRow } from '../../../api/boveda'

type Props = {
  oficinaId: number
  bovedaAbiertaId?: number
}

const CAJAS_PAGE_SIZE = 10

export function BovedaHistorialGrillas({ oficinaId, bovedaAbiertaId }: Props) {
  const [page, setPage] = useState(1)
  const [cajasPage, setCajasPage] = useState(1)
  const [selectedId, setSelectedId] = useState<number | undefined>(bovedaAbiertaId)

  const historial = useQuery({
    queryKey: ['boveda-listar', oficinaId, page],
    queryFn: () => listarBovedasHistorial(oficinaId, page, 5),
    enabled: oficinaId > 0,
  })

  const bovedaId = selectedId ?? historial.data?.rows[0]?.bovedaId

  const movimientos = useQuery({
    queryKey: ['rpt-movimiento-boveda', bovedaId],
    queryFn: () => fetchRptMovimientoBoveda(bovedaId!),
    enabled: !!bovedaId,
  })

  const cajas = useQuery({
    queryKey: ['saldos-caja-diario-boveda', oficinaId, bovedaId, cajasPage],
    queryFn: () =>
      fetchSaldosCajaDiarioBoveda(oficinaId, bovedaId!, {
        page: cajasPage,
        pageSize: CAJAS_PAGE_SIZE,
      }),
    enabled: oficinaId > 0 && !!bovedaId,
    placeholderData: keepPreviousData,
  })

  const colsBoveda: ColumnsType<BovedaListadoRow> = [
    { title: 'Id', dataIndex: 'bovedaId', width: 70 },
    { title: 'Tipo', dataIndex: 'tipo', width: 60 },
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
  ]

  const colsMov: ColumnsType<RptMovimientoBovedaRow> = [
    { title: 'Id', dataIndex: 'movimientoBovedaId', width: 70 },
    { title: 'Fecha', dataIndex: 'fechaReg', width: 100, render: formatFecha },
    { title: 'Op.', dataIndex: 'codOperacion', width: 70 },
    { title: 'Tipo pago', dataIndex: 'tipoPago', width: 90 },
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

  return (
    <div className="boveda-historial">
      <p className="boveda-historial__section-title">Cierres de bóveda</p>
      <CredixDataTable<BovedaListadoRow>
        mode="operacion"
        rowKey="bovedaId"
        size="small"
        columns={colsBoveda}
        dataSource={historial.data?.rows ?? []}
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
          pageSize: 5,
          total: historial.data?.total ?? 0,
          showSizeChanger: false,
          onChange: setPage,
        }}
      />

      {bovedaId ? (
        <>
          <p className="boveda-historial__section-title">
            Movimientos bóveda #{bovedaId}
            <Space style={{ marginLeft: 12 }}>
              <Link to={`/tesoreria/movimiento-boveda?bovedaId=${bovedaId}`}>
                <Button size="small" type="link">
                  Informe completo
                </Button>
              </Link>
            </Space>
          </p>
          <CredixDataTable
            mode="operacion"
            rowKey="movimientoBovedaId"
            size="small"
            columns={colsMov}
            dataSource={movimientos.data ?? []}
            loading={movimientos.isLoading}
            pagination={{ pageSize: 5, showSizeChanger: true }}
            locale={{ emptyText: 'Sin movimientos' }}
          />

          <p className="boveda-historial__section-title">Saldos caja diario (sesión bóveda)</p>
          <CredixDataTable
            mode="operacion"
            rowKey="id"
            size="small"
            columns={colsCaja}
            dataSource={cajas.data?.rows ?? []}
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
        </>
      ) : null}
    </div>
  )
}
