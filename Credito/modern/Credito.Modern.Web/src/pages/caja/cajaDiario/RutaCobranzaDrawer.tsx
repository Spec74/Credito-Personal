import { useMemo, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Alert, Button, Grid, Radio, Typography, message } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { fetchCobroDiario, generarRutaCobros } from '../../../api/creditoPlanes'
import { ApiError } from '../../../api/errors'
import { CajaDrawer } from '../../../components/caja/CajaDrawer'
import { toCobroDiarioQuery } from '../../../utils/gestorInformeForm'
import { CredixDataTable } from '../../../components/credix'
import type { RptCobroDiarioRow } from '../../../types/api'
import { formatMoney } from '../../../utils/formatMoney'
import { RutaQrModal } from './RutaQrModal'

const { Text } = Typography
const MAX_RUTA = 25

function buildRutaUrl(urlCortita: string): string {
  const apiBase = (import.meta.env.VITE_API_BASE_URL as string).replace(/\/$/, '')
  const relative = urlCortita.replace(/^\/api\/v1/, '')
  return `${apiBase}${relative}`
}

export function RutaCobranzaDrawer({
  open,
  oficinaId,
  usuarioId,
  onClose,
}: {
  open: boolean
  oficinaId: number
  usuarioId: number
  onClose: () => void
}) {
  const screens = Grid.useBreakpoint()
  const isMobile = screens.md !== true
  const [filtro, setFiltro] = useState<0 | 1>(0)
  const [selected, setSelected] = useState<number[]>([])
  const [qrUrl, setQrUrl] = useState<string | null>(null)
  const [qrOpen, setQrOpen] = useState(false)

  const query = useQuery({
    queryKey: ['caja-ruta-cartera', oficinaId, usuarioId, filtro],
    queryFn: () =>
      fetchCobroDiario(
        toCobroDiarioQuery(oficinaId, usuarioId, filtro === 1),
      ),
    enabled: open && oficinaId > 0 && usuarioId > 0,
  })

  const filas = useMemo(() => query.data ?? [], [query.data])

  const generar = useMutation({
    mutationFn: () => generarRutaCobros(selected),
    onSuccess: (res) => {
      if (!res.exito || !res.urlCortita) {
        message.error(res.mensaje ?? 'No se pudo generar la ruta')
        return
      }
      const fullUrl = buildRutaUrl(res.urlCortita)
      setQrUrl(fullUrl)
      setQrOpen(true)
      message.success('Ruta generada — escanee el QR o abra el enlace')
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'Error al generar ruta'),
  })

  const columns: ColumnsType<RptCobroDiarioRow> = [
    { title: 'Crédito', dataIndex: 'creditoId', width: 80 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    {
      title: 'Cuota',
      dataIndex: 'cuotaTotal',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Días atraso',
      dataIndex: 'diasAtrazo',
      width: 90,
      align: 'center',
    },
  ]

  const handleClose = () => {
    setQrOpen(false)
    setQrUrl(null)
    onClose()
  }

  return (
    <>
      <CajaDrawer
        title="Armar ruta de cobranza"
        open={open}
        onClose={handleClose}
        width={isMobile ? '100%' : 560}
        footer={
          <Button
            type="primary"
            block={isMobile}
            disabled={selected.length === 0}
            loading={generar.isPending}
            onClick={() => generar.mutate()}
          >
            Generar QR de ruta ({selected.length})
          </Button>
        }
      >
        <div style={{ marginBottom: 12 }}>
          <Text strong>Filtrar cartera</Text>
          <Radio.Group
            value={filtro}
            onChange={(e) => {
              setFiltro(e.target.value)
              setSelected([])
            }}
            style={{ display: 'flex', flexDirection: 'column', gap: 6, marginTop: 8 }}
          >
            <Radio value={0}>Cuotas de hoy + atrasados</Radio>
            <Radio value={1}>Solo atrasados (morosos)</Radio>
          </Radio.Group>
          <Button
            size="small"
            style={{ marginTop: 8 }}
            onClick={() => void query.refetch()}
          >
            Actualizar lista
          </Button>
        </div>
        <Alert
          type="info"
          showIcon
          message={`Seleccione máximo ${MAX_RUTA} clientes (toque la tarjeta o el check)`}
          style={{ marginBottom: 12 }}
        />
        <CredixDataTable<RptCobroDiarioRow>
          mode="operacion"
          rowKey="creditoId"
          columns={columns}
          dataSource={filas}
          loading={query.isLoading}
          pagination={{ defaultPageSize: isMobile ? 10 : 15 }}
          size="small"
          rowSelection={{
            selectedRowKeys: selected,
            onChange: (keys) => {
              const ids = keys.map(Number)
              if (ids.length > MAX_RUTA) {
                message.warning(`Máximo ${MAX_RUTA} créditos por ruta`)
                setSelected(ids.slice(0, MAX_RUTA))
                return
              }
              setSelected(ids)
            },
          }}
        />
      </CajaDrawer>

      <RutaQrModal
        open={qrOpen}
        url={qrUrl}
        onClose={() => {
          setQrOpen(false)
          setQrUrl(null)
        }}
      />
    </>
  )
}
