import { useEffect, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Descriptions, Input, Space } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { buscarClientes } from '../../api/clientes'
import {
  downloadRptClienteCsv,
  downloadRptClientePdf,
  fetchRptCliente,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { ClienteBuscarItem, RptAvalRow } from '../../types/api'
import { formatMoney } from '../../utils/formatMoney'

export function ReporteClientePage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [termino, setTermino] = useState('')
  const [personaId, setPersonaId] = useState<number | null>(null)
  const [clienteLabel, setClienteLabel] = useState('')

  const busqueda = useMutation({
    mutationFn: (term: string) => buscarClientes(term),
  })

  const consulta = useMutation({
    mutationFn: (pid: number) => fetchRptCliente(pid),
  })

  const paramId = searchParams.get('personaId')
  const paramPersonaId =
    paramId && !Number.isNaN(Number(paramId)) ? Number(paramId) : null

  if (paramPersonaId !== null && paramPersonaId !== personaId) {
    setPersonaId(paramPersonaId)
  }

  const lastAutoFetch = useRef<number | null>(null)
  useEffect(() => {
    if (paramPersonaId != null && paramPersonaId !== lastAutoFetch.current) {
      lastAutoFetch.current = paramPersonaId
      consulta.mutate(paramPersonaId)
    }
  }, [paramPersonaId, consulta])

  const csv = useMutation({
    mutationFn: (pid: number) => downloadRptClienteCsv(pid),
  })

  const pdf = useMutation({
    mutationFn: (pid: number) => downloadRptClientePdf(pid),
  })

  const seleccionarCliente = (item: ClienteBuscarItem) => {
    setPersonaId(item.personaId)
    setClienteLabel(item.label)
    setTermino(item.label)
    setSearchParams({ personaId: String(item.personaId) })
    consulta.mutate(item.personaId)
    busqueda.reset()
  }

  const stats = useInformeStats({ ...consulta, data: consulta.data?.avales })
  const f = consulta.data?.ficha

  const columnsAvales: ColumnsType<RptAvalRow> = [
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
      title="Reporte cliente (ficha)"
      subtitle="Ficha del cliente con datos personales, cónyuge y avales de créditos."
      breadcrumb={reportesCreditoBreadcrumb('Ficha cliente')}
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
              Persona id: {personaId}
              {clienteLabel ? ` — ${clienteLabel}` : ''}
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
      panelTitle={f?.cliente ?? 'Avales'}
    >
      {f ? (
        <Descriptions size="small" column={{ xs: 1, sm: 2 }} style={{ marginBottom: 16 }}>
          <Descriptions.Item label="DNI">{f.numeroDocumento}</Descriptions.Item>
          <Descriptions.Item label="Créditos DES">
            {f.creditosDesembolsados}
          </Descriptions.Item>
          <Descriptions.Item label="F. nacimiento">
            {f.fechaNacimiento ?? '—'}
          </Descriptions.Item>
          <Descriptions.Item label="Sexo">{f.sexo ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Celular">{f.celular ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Estado civil">{f.estadoCivil}</Descriptions.Item>
          <Descriptions.Item label="Tipo vivienda">{f.tipoVivienda}</Descriptions.Item>
          <Descriptions.Item label="Actividad econ.">
            {f.actividadEconomica}
          </Descriptions.Item>
          <Descriptions.Item label="Dirección" span={2}>
            {f.direccion ?? '—'}
          </Descriptions.Item>
          <Descriptions.Item label="Ref. dirección" span={2}>
            {f.direccionRef ?? '—'}
          </Descriptions.Item>
          <Descriptions.Item label="Cónyuge">{f.conyugue}</Descriptions.Item>
          <Descriptions.Item label="DNI cónyuge">{f.conyugueDni}</Descriptions.Item>
          <Descriptions.Item label="Cel. cónyuge">{f.conyugueCelular}</Descriptions.Item>
          <Descriptions.Item label="Negocio" span={2}>
            {f.direccionNegocio ?? '—'}
          </Descriptions.Item>
          <Descriptions.Item label="Nota" span={2}>
            {f.nota ?? '—'}
          </Descriptions.Item>
        </Descriptions>
      ) : null}
      <CredixDataTable<RptAvalRow>
        rowKey={(r) => `${r.creditoId}-${r.grupo}`}
        columns={columnsAvales}
        dataSource={consulta.data?.avales ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        locale={{ emptyText: 'Seleccione un cliente' }}
      />
    </CredixInformePage>
  )
}
