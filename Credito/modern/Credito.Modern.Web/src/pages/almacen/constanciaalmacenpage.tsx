import { useEffect, useRef, useState } from 'react'

import { Link, useSearchParams } from 'react-router-dom'

import { useMutation } from '@tanstack/react-query'

import { SearchOutlined } from '@ant-design/icons'

import { Button, Descriptions, InputNumber, Table } from 'antd'

import type { ColumnsType } from 'antd/es/table'

import {

  downloadRptConstanciaAlmacenCsv,

  downloadRptConstanciaAlmacenPdf,

  fetchRptConstanciaAlmacen,

  type RptConstanciaAlmacenDetLinea,

} from '../../api/entradaAlmacen'

import { useAuth } from '../../auth/useAuth'

import { InformeExportBar } from '../../components/informes/InformeExportBar'

import { CredixDataTable, CredixInformePage, CredixPanel } from '../../components/credix'

import { useInformeStats } from '../../hooks/useInformeStats'

import { formatFecha } from '../../utils/formatFecha'

import { formatMoney } from '../../utils/formatMoney'



export function ConstanciaAlmacenPage() {

  const { session } = useAuth()

  const [searchParams, setSearchParams] = useSearchParams()

  const oficinaId = session?.oficinaId ?? 0

  const [movimientoId, setMovimientoId] = useState<number | null>(null)



  const consulta = useMutation({
    mutationFn: (p: { oficinaId: number; movimientoId: number }) =>
      fetchRptConstanciaAlmacen(p.oficinaId, p.movimientoId),
  })

  const paramMov = searchParams.get('movimientoId')
  const paramMovId =
    paramMov && !Number.isNaN(Number(paramMov)) ? Number(paramMov) : null

  if (paramMovId !== null && paramMovId !== movimientoId) {
    setMovimientoId(paramMovId)
  }

  const lastAutoFetch = useRef<number | null>(null)
  useEffect(() => {
    if (paramMovId != null && oficinaId > 0 && paramMovId !== lastAutoFetch.current) {
      lastAutoFetch.current = paramMovId
      consulta.mutate({ oficinaId, movimientoId: paramMovId })
    }
  }, [paramMovId, oficinaId, consulta])



  const csv = useMutation({

    mutationFn: (p: { oficinaId: number; movimientoId: number }) =>

      downloadRptConstanciaAlmacenCsv(p.oficinaId, p.movimientoId),

  })



  const pdf = useMutation({

    mutationFn: (p: { oficinaId: number; movimientoId: number }) =>

      downloadRptConstanciaAlmacenPdf(p.oficinaId, p.movimientoId),

  })



  const consultar = () => {

    if (movimientoId != null && movimientoId >= 1 && oficinaId > 0) {

      setSearchParams({ movimientoId: String(movimientoId) })

      consulta.mutate({ oficinaId, movimientoId })

    }

  }



  const stats = useInformeStats(
    { ...consulta, data: consulta.data?.detalle },
    oficinaId > 0 ? oficinaId : null,
  )
  const cab = consulta.data?.cabecera



  const columns: ColumnsType<RptConstanciaAlmacenDetLinea> = [

    { title: 'Cant.', dataIndex: 'cantidad', width: 70 },

    { title: 'Producto', dataIndex: 'descripcion', ellipsis: true },

    {

      title: 'P. unit.',

      dataIndex: 'precioUnitario',

      width: 95,

      align: 'right',

      render: formatMoney,

    },

    {

      title: 'Dsct.',

      dataIndex: 'descuento',

      width: 85,

      align: 'right',

      render: formatMoney,

    },

    {

      title: 'Importe',

      dataIndex: 'importe',

      width: 95,

      align: 'right',

      render: formatMoney,

    },

  ]



  return (

    <CredixInformePage

      title="Constancia de almacén"

      subtitle="Comprobante de entrada o salida por número de movimiento."

      breadcrumb={[

        { title: <Link to="/inicio">Inicio</Link> },

        { title: <Link to="/almacen">Almacén</Link> },

        { title: 'Constancia' },

      ]}

      stats={stats}

      filters={

        <>

          <InputNumber

            min={1}

            placeholder="Movimiento ID"

            value={movimientoId ?? undefined}

            onChange={(v) => setMovimientoId(v ?? null)}

            style={{ width: 160, marginRight: 8 }}

          />

          <Button

            type="primary"

            icon={<SearchOutlined />}

            disabled={movimientoId == null || movimientoId < 1 || oficinaId < 1}

            loading={consulta.isPending}

            onClick={consultar}

          >

            Consultar

          </Button>

        </>

      }

      exportBar={

        movimientoId != null && movimientoId >= 1 && oficinaId > 0 ? (

          <InformeExportBar

            csvLoading={csv.isPending}

            pdfLoading={pdf.isPending}

            onCsv={() => csv.mutate({ oficinaId, movimientoId })}

            onPdfTabular={() => pdf.mutate({ oficinaId, movimientoId })}

          />

        ) : null

      }

    >

      {cab ? (
        <div style={{ marginBottom: 16 }}>
        <CredixPanel title={`CONSTANCIA DE ${cab.tipo}: ${cab.movimientoId}`}>

          <p style={{ fontWeight: 600, marginBottom: 8 }}>{cab.tipoMovimientoDesc}</p>

          <Descriptions size="small" column={{ xs: 1, sm: 2 }}>

            <Descriptions.Item label="Oficina">{cab.oficina}</Descriptions.Item>

            <Descriptions.Item label="Almacén">{cab.almacen}</Descriptions.Item>

            <Descriptions.Item label="Tipo">{cab.tipoMovimiento}</Descriptions.Item>

            <Descriptions.Item label="Documentos">{cab.documento ?? '—'}</Descriptions.Item>

            <Descriptions.Item label="Fecha operación">

              {formatFecha(cab.fecha)}

            </Descriptions.Item>

            <Descriptions.Item label="Estado">{cab.estado}</Descriptions.Item>

            <Descriptions.Item label="Observación" span={2}>

              {cab.observacion ?? '—'}

            </Descriptions.Item>

            <Descriptions.Item label="Total importe">

              {formatMoney(cab.importe)}

            </Descriptions.Item>

          </Descriptions>

        </CredixPanel>
        </div>
      ) : null}



      <CredixDataTable<RptConstanciaAlmacenDetLinea>

        rowKey={(_, i) => String(i)}

        loading={consulta.isPending}

        dataSource={consulta.data?.detalle ?? []}

        columns={columns}

        pagination={false}

        summary={() =>

          cab ? (

            <Table.Summary.Row>

              <Table.Summary.Cell index={0} colSpan={4} align="right">

                <strong>Total</strong>

              </Table.Summary.Cell>

              <Table.Summary.Cell index={4} align="right">

                <strong>{formatMoney(cab.importe)}</strong>

              </Table.Summary.Cell>

            </Table.Summary.Row>

          ) : null

        }

        locale={{ emptyText: 'Indique movimiento y consulte' }}

      />

    </CredixInformePage>

  )

}


