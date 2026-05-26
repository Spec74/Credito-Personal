import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Input, Space } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { buscarClientes } from '../../api/clientes'
import {
  downloadRptAvalCsv,
  downloadRptAvalPdf,
  fetchRptAval,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import type { ClienteBuscarItem, RptAvalRow } from '../../types/api'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import { formatMoney } from '../../utils/formatMoney'

export function AvalPersonaPage() {
  const [termino, setTermino] = useState('')
  const [personaId, setPersonaId] = useState<number | null>(null)
  const [clienteLabel, setClienteLabel] = useState('')

  const busqueda = useMutation({
    mutationFn: (term: string) => buscarClientes(term),
  })

  const consulta = useMutation({
    mutationFn: (pid: number) => fetchRptAval(pid),
  })

  const csv = useMutation({
    mutationFn: (pid: number) => downloadRptAvalCsv(pid),
  })

  const pdf = useMutation({
    mutationFn: (pid: number) => downloadRptAvalPdf(pid),
  })

  const seleccionarCliente = (item: ClienteBuscarItem) => {
    setPersonaId(item.personaId)
    setClienteLabel(item.label)
    setTermino(item.label)
    busqueda.reset()
  }

  const stats = useInformeStats(consulta)
  const columns: ColumnsType<RptAvalRow> = [
    { title: 'Grupo', dataIndex: 'grupo', width: 80 },
    { title: 'Crédito', dataIndex: 'creditoId', width: 75 },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Estado', dataIndex: 'estado', width: 100 },
    { title: 'Persona', dataIndex: 'persona', ellipsis: true },
    { title: 'DNI', dataIndex: 'dni', width: 100 },
    { title: 'Celular', dataIndex: 'celular', width: 110 },
  ]

  const busquedaColumns: ColumnsType<ClienteBuscarItem> = [
    { title: 'Cliente', dataIndex: 'label' },
    {
      title: '',
      key: 'sel',
      width: 100,
      render: (_, row) => (
        <Button size="small" onClick={() => seleccionarCliente(row)}>
          Seleccionar
        </Button>
      ),
    },
  ]

  return (
    <CredixInformePage
      title="Avales de créditos por persona"
      subtitle="Busque un cliente y consulte los avales asociados a sus créditos."
      breadcrumb={reportesCreditoBreadcrumb('Avales por persona')}
      stats={stats}
      filters={
        <Space direction="vertical" style={{ width: '100%' }} size="middle">
          <Space wrap>
            <Input
              placeholder="Buscar cliente (mín. 2 caracteres)"
              value={termino}
              onChange={(e) => setTermino(e.target.value)}
              onPressEnter={() => {
                const t = termino.trim()
                if (t.length >= 2) busqueda.mutate(t)
              }}
              style={{ width: 320 }}
            />
            <Button
              icon={<SearchOutlined />}
              onClick={() => {
                const t = termino.trim()
                if (t.length >= 2) busqueda.mutate(t)
              }}
              loading={busqueda.isPending}
            >
              Buscar
            </Button>
            {personaId ? (
              <Button
                type="primary"
                onClick={() => consulta.mutate(personaId)}
                loading={consulta.isPending}
              >
                Consultar avales
              </Button>
            ) : null}
          </Space>
          {busqueda.data && busqueda.data.length > 0 ? (
            <CredixDataTable<ClienteBuscarItem>
              rowKey="personaId"
              pagination={false}
              dataSource={busqueda.data}
              columns={busquedaColumns}
            />
          ) : null}
          {personaId ? (
            <span style={{ color: 'var(--ant-color-text-secondary)' }}>
              Persona id: {personaId} — {clienteLabel}
            </span>
          ) : null}
        </Space>
      }
      exportBar={
        personaId ? (
          <InformeExportBar
            csvLoading={csv.isPending}
            pdfLoading={pdf.isPending}
            onCsv={() => csv.mutate(personaId)}
            onPdfTabular={() => pdf.mutate(personaId)}
          />
        ) : null
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 16 }}
            message={
              consulta.error instanceof ApiError ? consulta.error.message : 'Error en la consulta'
            }
          />
        ) : null
      }
    >
      <CredixDataTable<RptAvalRow>
        rowKey={(r) => `${r.creditoId}-${r.grupo}`}
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        locale={{ emptyText: 'Seleccione un cliente y consulte' }}
      />
    </CredixInformePage>
  )
}
