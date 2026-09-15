import { useState } from 'react'

import { Link } from 'react-router-dom'

import { useMutation } from '@tanstack/react-query'

import { SearchOutlined } from '@ant-design/icons'

import { Button, InputNumber } from 'antd'

import type { ColumnsType } from 'antd/es/table'

import {

  downloadCodigoBarrasLstCsv,

  downloadCodigoBarrasLstPdf,

  fetchCodigoBarrasLst,

} from '../../api/ventas'

import { InformeExportBar } from '../../components/informes/InformeExportBar'

import { CredixDataTable, CredixInformePage } from '../../components/credix'

import { useInformeStats } from '../../hooks/useInformeStats'

import type { CodigoBarrasLstRow } from '../../types/api'

import { formatMoney } from '../../utils/formatMoney'



export function CodigoBarrasPage() {

  const [movimientoId, setMovimientoId] = useState<number | null>(null)



  const consulta = useMutation({

    mutationFn: (id: number) => fetchCodigoBarrasLst(id),

  })



  const csv = useMutation({

    mutationFn: (id: number) => downloadCodigoBarrasLstCsv(id),

  })



  const pdf = useMutation({

    mutationFn: (id: number) => downloadCodigoBarrasLstPdf(id),

  })



  const stats = useInformeStats(consulta)
  const columns: ColumnsType<CodigoBarrasLstRow> = [

    { title: 'Serie 1', dataIndex: 'serie1', width: 120 },

    { title: 'Artículo 1', dataIndex: 'articulo1', ellipsis: true },

    {

      title: 'Precio 1',

      dataIndex: 'precio1',

      width: 95,

      align: 'right',

      render: formatMoney,

    },

    { title: 'Serie 2', dataIndex: 'serie2', width: 120 },

    { title: 'Artículo 2', dataIndex: 'articulo2', ellipsis: true },

    {

      title: 'Precio 2',

      dataIndex: 'precio2',

      width: 95,

      align: 'right',

      render: formatMoney,

    },

  ]



  return (

    <CredixInformePage

      title="Códigos de barras"

      subtitle="Series y artículos asociados a un movimiento de almacén."

      breadcrumb={[

        { title: <Link to="/inicio">Inicio</Link> },

        { title: <Link to="/almacen">Almacén</Link> },

        { title: 'Códigos de barras' },

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

            disabled={movimientoId == null || movimientoId < 1}

            loading={consulta.isPending}

            onClick={() => movimientoId != null && consulta.mutate(movimientoId)}

          >

            Consultar

          </Button>

        </>

      }

      exportBar={

        movimientoId != null && movimientoId >= 1 ? (

          <InformeExportBar

            csvLoading={csv.isPending}

            pdfLoading={pdf.isPending}

            onCsv={() => csv.mutate(movimientoId)}

            onPdfTabular={() => pdf.mutate(movimientoId)}

          />

        ) : null

      }

    >

      <CredixDataTable<CodigoBarrasLstRow>

        rowKey={(_, i) => String(i)}

        size="small"

        loading={consulta.isPending}

        dataSource={consulta.data ?? []}

        columns={columns}

        pagination={{ pageSize: 20, showSizeChanger: true }}

        locale={{ emptyText: 'Indique movimiento y consulte' }}

      />

    </CredixInformePage>

  )

}

