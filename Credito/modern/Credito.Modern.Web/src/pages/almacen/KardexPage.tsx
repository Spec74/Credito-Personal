import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadGenerarKardexCsv,
  downloadGenerarKardexPdf,
  fetchGenerarKardex,
} from '../../api/almacen'
import { fetchAlmacenes } from '../../api/almacenes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixArticuloSelect, CredixDataTable, CredixInformePage, CredixSelect } from '../../components/credix'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { GenerarKardexParams, GenerarKardexRow } from '../../types/api'
import { esCodigoRematePrendario } from '../../utils/codigoRematePrendario'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

type FormValues = {
  oficinaId: number
  articuloId: number
  almacenId: number
}

function toParams(v: FormValues): GenerarKardexParams {
  return {
    oficinaId: v.oficinaId,
    articuloId: v.articuloId,
    almacenId: v.almacenId,
  }
}

export function KardexPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()
  const oficinaId = session?.oficinaId ?? 0
  const [articuloCodigo, setArticuloCodigo] = useState('')

  const almacenes = useQuery({
    queryKey: ['almacenes', oficinaId],
    queryFn: () => fetchAlmacenes(oficinaId),
    enabled: oficinaId > 0,
  })

  useEffect(() => {
    if (session) {
      form.setFieldsValue({ oficinaId: session.oficinaId })
    }
  }, [session, form])

  useEffect(() => {
    if (!almacenes.data?.length) return
    if (form.getFieldValue('almacenId')) return
    form.setFieldValue('almacenId', almacenes.data[0].almacenId)
  }, [almacenes.data, form])

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchGenerarKardex(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadGenerarKardexCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadGenerarKardexPdf(toParams(v)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<GenerarKardexRow> = [
    { title: 'Det.', dataIndex: 'movimientoDetId', width: 60 },
    {
      title: 'Fecha',
      dataIndex: 'fecha',
      width: 100,
      render: formatFecha,
    },
    { title: 'Concepto', dataIndex: 'concepto', ellipsis: true },
    { title: 'Cant. ent.', dataIndex: 'cantEnt', width: 75 },
    {
      title: 'PU ent.',
      dataIndex: 'puEnt',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Total ent.',
      dataIndex: 'totalEnt',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Cant. sal.', dataIndex: 'cantSal', width: 75 },
    {
      title: 'PU sal.',
      dataIndex: 'puSal',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Total sal.',
      dataIndex: 'totalSal',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Saldo cant.', dataIndex: 'cantSaldo', width: 85 },
    {
      title: 'PU saldo',
      dataIndex: 'puSaldo',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo total',
      dataIndex: 'totalSaldo',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
  ]

  const almacenOptions =
    almacenes.data?.map((a) => ({
      value: a.almacenId,
      label: a.denominacion,
    })) ?? []

  return (
    <CredixInformePage
      title="Kardex de artículo"
      subtitle="Kardex de entradas y salidas. Un PRM rematado no tiene entrada documental hasta que se vende."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/almacen">Almacén</Link> },
        { title: 'Kardex' },
      ]}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="vertical"
          className="kardex-filtros"
          onFinish={(v) => consulta.mutate({ ...v, oficinaId })}
        >
          <Form.Item name="oficinaId" hidden>
            <span />
          </Form.Item>
          <Form.Item
            name="almacenId"
            label="Almacén"
            extra={
              almacenOptions.length === 0 && !almacenes.isLoading ? (
                <Link to="/maestros/almacenes">Crear almacén</Link>
              ) : null
            }
            rules={[{ required: true, message: 'Seleccione almacén' }]}
          >
            <CredixSelect
              placeholder="Almacén"
              options={almacenOptions}
              loading={almacenes.isLoading}
              style={{ maxWidth: 360 }}
            />
          </Form.Item>
          <Form.Item
            name="articuloId"
            label="Artículo"
            extra={<Link to="/maestros/articulos">Ver artículos</Link>}
            rules={[{ required: true, message: 'Elija un artículo' }]}
          >
            <CredixArticuloSelect
              style={{ maxWidth: 360 }}
              onArticulo={(item) => setArticuloCodigo(item?.codArticulo?.trim() ?? '')}
            />
          </Form.Item>
          <Form.Item>
            <Button
              type="primary"
              icon={<SearchOutlined />}
              htmlType="submit"
              loading={consulta.isPending}
            >
              Consultar
            </Button>
          </Form.Item>
        </Form>
      }
      exportBar={
        <InformeExportBar
          csvLoading={csv.isPending}
          pdfLoading={pdf.isPending}
          onCsv={async () => csv.mutate(await form.validateFields())}
          onPdfTabular={async () => pdf.mutate(await form.validateFields())}
        />
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            message="Error al consultar"
            description={
              consulta.error instanceof ApiError
                ? consulta.error.message
                : 'Error desconocido'
            }
            style={{ marginBottom: 16 }}
          />
        ) : null
      }
    >
      {consulta.isSuccess && (consulta.data?.length ?? 0) === 0 ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message={
            esCodigoRematePrendario(articuloCodigo)
              ? `${articuloCodigo} no tiene kardex todavía`
              : 'Sin movimientos de kardex'
          }
          description={
            esCodigoRematePrendario(articuloCodigo) ? (
              <>
                El remate deja la serie EN_ALMACEN sin documento de entrada. El kardex aparece al
                venderlo.{' '}
                <Link to={`/ventas/venta-rapida?codigo=${encodeURIComponent(articuloCodigo)}`}>
                  Ir a venta rápida
                </Link>
                .
              </>
            ) : (
              'Kardex lee entradas y salidas confirmadas. Use Entrada para catálogo, o Venta rápida para un PRM rematado.'
            )
          }
        />
      ) : null}
      <CredixDataTable<GenerarKardexRow>
        rowKey={(r, i) => String(r.movimientoDetId ?? i)}
        size="small"
        scroll={{ x: 1200 }}
        loading={consulta.isPending}
        dataSource={consulta.data ?? []}
        columns={columns}
        pagination={{ pageSize: 25, showSizeChanger: true }}
        locale={{
          emptyText: consulta.isSuccess
            ? 'Sin filas de movimiento'
            : 'Indique artículo y almacén',
        }}
      />
    </CredixInformePage>
  )
}
