import { useMemo, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Alert, Button, Grid, Input, Radio, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { fetchCobroDiario, generarRutaCobros } from '../../../api/creditoPlanes'
import { ApiError } from '../../../api/errors'
import { CajaDrawer } from '../../../components/caja/CajaDrawer'
import { toCobroDiarioQuery } from '../../../utils/gestorInformeForm'
import { CredixDataTable } from '../../../components/credix'
import type { RptCobroDiarioRow } from '../../../types/api'
import { formatMoney } from '../../../utils/formatMoney'
import {
  cajaToastError,
  cajaToastWarning,
} from './cajaFeedback'
import {
  RutaCobradorResultModal,
  type RutaCobradorResult,
} from './RutaCobradorResultModal'

const { Text } = Typography
const MAX_RUTA = 25

function buildApiUrl(urlCortita: string): string {
  const apiBase = (import.meta.env.VITE_API_BASE_URL as string).replace(/\/$/, '')
  const relative = urlCortita.replace(/^\/api\/v1/, '')
  return `${apiBase}${relative}`
}

/**
 * Arma la ruta del cobrador (enfoque: morosos / personal temporal).
 * Resultado: mapa in-app + navegación Google multi-parada (WhatsApp/QR secundarios).
 */
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
  /** 1 = solo morosos (default profesional para cobrador contratado). */
  const [filtro, setFiltro] = useState<0 | 1>(1)
  const [selected, setSelected] = useState<number[]>([])
  const [buscar, setBuscar] = useState('')
  const [result, setResult] = useState<RutaCobradorResult | null>(null)
  const [resultOpen, setResultOpen] = useState(false)

  const query = useQuery({
    queryKey: ['caja-ruta-cartera', oficinaId, usuarioId, filtro],
    queryFn: () =>
      fetchCobroDiario(
        toCobroDiarioQuery(oficinaId, usuarioId, filtro === 1),
      ),
    enabled: open && oficinaId > 0 && usuarioId > 0,
  })

  const filas = useMemo(() => {
    const rows = query.data ?? []
    const q = buscar.trim().toLowerCase()
    if (!q) return rows
    return rows.filter((r) => {
      const nombre = (r.cliente ?? '').toLowerCase()
      const id = String(r.creditoId)
      return nombre.includes(q) || id.includes(q)
    })
  }, [query.data, buscar])

  const generar = useMutation({
    mutationFn: () =>
      generarRutaCobros(selected, { usuarioId, oficinaId }),
    onSuccess: (res) => {
      if (!res?.exito) {
        cajaToastError(
          res?.mensaje?.trim() ||
            'No se pudo generar la ruta. Compruebe que los créditos pertenecen a la cartera del gestor de esta caja.',
        )
        return
      }
      if (!res.paradas?.length) {
        cajaToastError(
          res.mensaje?.trim() ||
            'No se recibieron paradas para el mapa. Verifique la selección e intente nuevamente.',
        )
        return
      }
      const origen =
        res.latitudOrigen != null &&
        res.longitudOrigen != null &&
        res.latitudOrigen !== 0 &&
        res.longitudOrigen !== 0
          ? { lat: Number(res.latitudOrigen), lng: Number(res.longitudOrigen) }
          : null
      setResult({
        paradas: res.paradas.map((p) => ({
          orden: p.orden,
          creditoId: p.creditoId,
          cliente: p.cliente,
          montoCobrar: p.montoCobrar,
          direccion: p.direccion,
          latitud: p.latitud,
          longitud: p.longitud,
          tieneGps: p.tieneGps,
        })),
        origen,
        urlNavegacionGoogle: res.urlNavegacionGoogle,
        urlWhatsApp: res.urlCortita ? buildApiUrl(res.urlCortita) : null,
      })
      setResultOpen(true)
    },
    onError: (e) =>
      cajaToastError(
        e instanceof ApiError ? e.message : 'Error al generar ruta',
      ),
  })

  const columns: ColumnsType<RptCobroDiarioRow> = [
    { title: 'Crédito', dataIndex: 'creditoId', width: 80 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    { title: 'Dirección', dataIndex: 'direccion', ellipsis: true, minWidth: 120 },
    {
      title: 'Celular',
      dataIndex: 'celular',
      width: 108,
      ellipsis: true,
      render: (v: string | null) => v?.trim() || '—',
    },
    {
      title: 'Saldo',
      dataIndex: 'saldo',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Días',
      dataIndex: 'diasAtrazo',
      width: 70,
      align: 'center',
    },
  ]

  const handleClose = () => {
    setResultOpen(false)
    setResult(null)
    setBuscar('')
    onClose()
  }

  return (
    <>
      <CajaDrawer
        title="Ruta del cobrador"
        open={open}
        onClose={handleClose}
        width={isMobile ? '100%' : 600}
        footer={
          <Button
            type="primary"
            block={isMobile}
            disabled={selected.length === 0}
            loading={generar.isPending}
            onClick={() => generar.mutate()}
          >
            Generar ruta en mapa ({selected.length})
          </Button>
        }
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Ruta de cobranza"
          description="Seleccione clientes (máx. 25) y genere la secuencia con mapa e indicaciones de navegación."
        />

        <div style={{ marginBottom: 12 }}>
          <Text strong>Cartera</Text>
          <Radio.Group
            value={filtro}
            onChange={(e) => {
              setFiltro(e.target.value)
              setSelected([])
            }}
            style={{ display: 'flex', flexDirection: 'column', gap: 6, marginTop: 8 }}
          >
            <Radio value={1}>Solo morosos / atrasados (recomendado)</Radio>
            <Radio value={0}>Cuotas de hoy + atrasados</Radio>
          </Radio.Group>
        </div>

        <Input.Search
          allowClear
          placeholder="Buscar cliente o N° crédito"
          value={buscar}
          onChange={(e) => setBuscar(e.target.value)}
          style={{ marginBottom: 12 }}
        />

        <div style={{ display: 'flex', gap: 8, marginBottom: 12, flexWrap: 'wrap' }}>
          <Button size="small" onClick={() => void query.refetch()}>
            Actualizar
          </Button>
          <Button
            size="small"
            disabled={filas.length === 0}
            onClick={() => {
              const ids = filas.map((r) => r.creditoId).slice(0, MAX_RUTA)
              if (filas.length > MAX_RUTA) {
                cajaToastWarning(`Máximo ${MAX_RUTA}: se tomaron los primeros de la lista filtrada`)
              }
              setSelected(ids)
            }}
          >
            Seleccionar visibles
          </Button>
          <Button size="small" disabled={selected.length === 0} onClick={() => setSelected([])}>
            Limpiar selección
          </Button>
        </div>

        <Alert
          type="warning"
          showIcon
          message={`Máximo ${MAX_RUTA} paradas · ${selected.length} seleccionados`}
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
                cajaToastWarning(`Máximo ${MAX_RUTA} créditos por ruta`)
                setSelected(ids.slice(0, MAX_RUTA))
                return
              }
              setSelected(ids)
            },
          }}
        />
      </CajaDrawer>

      <RutaCobradorResultModal
        open={resultOpen}
        result={result}
        onClose={() => {
          setResultOpen(false)
          setResult(null)
        }}
      />
    </>
  )
}
