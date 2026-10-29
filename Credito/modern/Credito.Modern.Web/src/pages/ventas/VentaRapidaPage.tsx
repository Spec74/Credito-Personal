import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, Button, Input, InputNumber, Spin, Typography, message } from 'antd'
import {
  fetchArticuloVentaRapida,
  fetchCajaDiarioVentaRapida,
  realizarPedido,
  type ArticuloVentaRapida,
  type PedidoLinea,
} from '../../api/ventas'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CajaSinSesionAlert } from '../caja/components/CajaSinSesionAlert'
import { CompradorVentaField } from '../../components/ventas/CompradorVentaField'
import {
  CredixArticuloSelect,
  CredixPage,
  CredixPanel,
  type CredixStatItem,
} from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'

const { Text } = Typography

type LineaCarrito = PedidoLinea & {
  key: string
  codArticulo: string
  denominacion: string
  precioUnit: number
}

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

function stockInsuficienteMsg(stock: number): string {
  if (stock < 1) {
    return 'Sin series en almacén (0 disponible). Crear el artículo no da stock: use Entrada de almacén o remate prendario.'
  }
  return `Stock insuficiente (${stock} disponible)`
}

export function VentaRapidaPage() {
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [searchParams, setSearchParams] = useSearchParams()
  const codigoRemate = searchParams.get('codigo')?.trim() ?? ''
  const remateCargado = useRef(false)
  const [codigo, setCodigo] = useState(codigoRemate)
  const [articuloId, setArticuloId] = useState<number | undefined>()
  const [articuloCodigo, setArticuloCodigo] = useState('')
  const [cantidad, setCantidad] = useState(1)
  const [carrito, setCarrito] = useState<LineaCarrito[]>([])
  const [personaId, setPersonaId] = useState<number | null>(null)
  const [clienteLabel, setClienteLabel] = useState('')
  const [terminoCliente, setTerminoCliente] = useState('')

  const caja = useQuery({
    queryKey: ['caja-diario-venta-rapida', oficinaId],
    queryFn: () => fetchCajaDiarioVentaRapida(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const sinCaja = caja.isError && caja.error instanceof ApiError && caja.error.status === 404

  const buscarArticulo = useMutation({
    mutationFn: (c: string) => fetchArticuloVentaRapida(oficinaId, c),
  })

  const cobrar = useMutation({
    mutationFn: () => {
      if (!personaId || personaId < 1) {
        throw new Error('Seleccione un cliente')
      }
      if (!caja.data?.cajaDiarioId) {
        throw new Error('No hay caja diario abierta')
      }
      if (carrito.length === 0) {
        throw new Error('Agregue al menos un artículo al carrito')
      }
      if (carrito.some((l) => !(l.cantidad > 0))) {
        throw new Error('Cada artículo debe tener cantidad mayor a cero')
      }
      return realizarPedido({
        oficinaId,
        cajaDiarioId: caja.data.cajaDiarioId,
        personaId,
        pedidos: carrito.map(({ articuloId: id, cantidad: c, descuento: d }) => ({
          articuloId: id,
          cantidad: c,
          descuento: d,
        })),
      })
    },
    onSuccess: (r) => {
      message.success(`Venta registrada — orden #${r.ordenVentaId}`)
      setCarrito([])
      setPersonaId(null)
      setClienteLabel('')
      setTerminoCliente('')
      setCodigo('')
      setArticuloId(undefined)
      setArticuloCodigo('')
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const claveBusqueda = articuloCodigo.trim() || codigo.trim() || (articuloId ? String(articuloId) : '')

  const agregarAlCarrito = async (clave?: string) => {
    const c = (clave ?? claveBusqueda).trim()
    if (!c) {
      message.warning('Elija un artículo o escriba su código')
      return
    }
    if (cantidad < 1) {
      message.warning('Cantidad mínima 1')
      return
    }
    try {
      const art: ArticuloVentaRapida = await buscarArticulo.mutateAsync(c)
      const ya = carrito.find((l) => l.articuloId === art.articuloId)
      const cantidadNueva = (ya?.cantidad ?? 0) + cantidad
      if (art.stock < cantidadNueva) {
        message.error(stockInsuficienteMsg(art.stock))
        return
      }
      const precio = art.precioVenta ?? 0
      setCarrito((prev) => {
        const i = prev.findIndex((x) => x.articuloId === art.articuloId)
        if (i >= 0) {
          const next = [...prev]
          next[i] = { ...next[i], cantidad: next[i].cantidad + cantidad }
          return next
        }
        return [
          ...prev,
          {
            key: `${art.articuloId}-${Date.now()}`,
            articuloId: art.articuloId,
            codArticulo: art.codArticulo,
            denominacion: art.denominacion,
            cantidad,
            descuento: 0,
            precioUnit: precio,
          },
        ]
      })
      setCodigo('')
      setArticuloId(undefined)
      setArticuloCodigo('')
      setCantidad(1)
    } catch (e) {
      message.error(errMsg(e))
    }
  }

  useEffect(() => {
    if (codigoRemate && !codigo) setCodigo(codigoRemate)
  }, [codigoRemate, codigo])

  useEffect(() => {
    const codigoStock = codigoRemate
    if (!codigoStock || remateCargado.current || !caja.data) return
    remateCargado.current = true
    let cancelado = false
    void (async () => {
      try {
        const art = await fetchArticuloVentaRapida(oficinaId, codigoStock)
        if (cancelado) return
        if (art.stock < 1) {
          message.error(stockInsuficienteMsg(art.stock))
          return
        }
        setCarrito((prev) => {
          if (prev.some((l) => l.articuloId === art.articuloId)) return prev
          return [
            ...prev,
            {
              key: `${art.articuloId}-${Date.now()}`,
              articuloId: art.articuloId,
              codArticulo: art.codArticulo,
              denominacion: art.denominacion,
              cantidad: 1,
              descuento: 0,
              precioUnit: art.precioVenta ?? 0,
            },
          ]
        })
        setCodigo('')
        message.success(`${art.denominacion} listo en el ticket`)
      } catch (e) {
        if (!cancelado) message.error(errMsg(e))
      } finally {
        if (!cancelado) {
          const next = new URLSearchParams(searchParams)
          next.delete('codigo')
          setSearchParams(next, { replace: true })
        }
      }
    })()
    return () => {
      cancelado = true
    }
  }, [caja.data, codigoRemate, oficinaId, searchParams, setSearchParams])

  const total = carrito.reduce((s, l) => s + l.precioUnit * l.cantidad - l.descuento, 0)
  const stats = useMemo(
    (): CredixStatItem[] => [
      { value: carrito.length, label: 'Líneas' },
      { value: formatMoney(total), label: 'Total' },
      {
        value: personaId ? clienteLabel || `#${personaId}` : 'Sin cliente',
        label: 'Cliente',
        tone: personaId ? 'green' : 'default',
      },
    ],
    [carrito.length, total, personaId, clienteLabel],
  )

  return (
    <CredixPage
      title="Venta rápida"
      subtitle="Busque el artículo, revise el ticket y cobre. No hace falta el id interno."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/ventas">Ventas</Link> },
        { title: 'Venta rápida' },
      ]}
    >
      {oficinaId < 1 ? (
        <Alert type="warning" showIcon message="Sesión sin oficina válida." />
      ) : caja.isLoading ? (
        <CredixPanel>
          <Spin />
        </CredixPanel>
      ) : sinCaja ? (
        <CajaSinSesionAlert
          variant="diario"
          refreshing={caja.isFetching}
          onRefresh={() => void caja.refetch()}
          onAssigned={() => void caja.refetch()}
        />
      ) : caja.isError ? (
        <Alert type="error" showIcon message={errMsg(caja.error)} />
      ) : caja.data ? (
        <>
          <Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>
            Caja {caja.data.cajaDenominacion} · Diario #{caja.data.cajaDiarioId}
          </Text>

          <div className="venta-rapida">
            <div className="venta-rapida__pedido">
              <CredixPanel title="Agregar artículo">
                <div className="venta-rapida-agregar">
                  <CredixArticuloSelect
                    value={articuloId}
                    placeholder="Buscar por nombre"
                    onChange={(id) => setArticuloId(id)}
                    onArticulo={(item) => {
                      setArticuloId(item?.articuloId)
                      setArticuloCodigo(item?.codArticulo?.trim() ?? '')
                    }}
                  />
                  <Input
                    value={codigo}
                    placeholder="O escriba / escanee el código"
                    onChange={(e) => setCodigo(e.target.value)}
                    onPressEnter={() => void agregarAlCarrito(codigo)}
                    allowClear
                  />
                  <div className="venta-rapida-agregar__fila">
                    <InputNumber
                      min={1}
                      value={cantidad}
                      onChange={(v) => setCantidad(v ?? 1)}
                      addonBefore="Cant."
                      style={{ width: '100%' }}
                    />
                    <Button
                      type="primary"
                      icon={<PlusOutlined />}
                      loading={buscarArticulo.isPending}
                      onClick={() => void agregarAlCarrito()}
                    >
                      Agregar
                    </Button>
                  </div>
                  <Text type="secondary">
                    El catálogo no es inventario. Para vender hace falta una serie EN_ALMACEN:{' '}
                    <Link to="/almacen/entrada">Entrada de almacén</Link> o remate prendario. Precio en{' '}
                    <Link to="/ventas/lista-precios">Lista de precios</Link>.
                  </Text>
                </div>
              </CredixPanel>

              <CredixPanel title="Pedido">
                {carrito.length === 0 ? (
                  <p className="venta-rapida-ticket__vacio">
                    Aún no hay líneas. Busque un artículo y pulse Agregar.
                  </p>
                ) : (
                  <div className="venta-rapida-ticket">
                    {carrito.map((l) => (
                      <div key={l.key} className="venta-rapida-linea">
                        <div>
                          <span className="venta-rapida-linea__nombre">{l.denominacion}</span>
                          <span className="venta-rapida-linea__meta">
                            {l.cantidad} × {formatMoney(l.precioUnit)}
                            {l.codArticulo ? ` · ${l.codArticulo}` : ''}
                            {l.descuento > 0 ? ` · desc. ${formatMoney(l.descuento)}` : ''}
                          </span>
                        </div>
                        <span className="venta-rapida-linea__importe">
                          {formatMoney(l.precioUnit * l.cantidad - l.descuento)}
                        </span>
                        <Button
                          type="text"
                          danger
                          aria-label={`Quitar ${l.denominacion}`}
                          icon={<DeleteOutlined />}
                          onClick={() => setCarrito((p) => p.filter((x) => x.key !== l.key))}
                        />
                      </div>
                    ))}
                  </div>
                )}
              </CredixPanel>
            </div>

            <aside className="venta-rapida__cobro">
              <CredixPanel title="Cliente">
                <CompradorVentaField
                  personaId={personaId}
                  clienteLabel={clienteLabel}
                  termino={terminoCliente}
                  onTerminoChange={setTerminoCliente}
                  onSelect={(id, label) => {
                    setPersonaId(id)
                    setClienteLabel(label)
                  }}
                  onClear={() => {
                    setPersonaId(null)
                    setClienteLabel('')
                    setTerminoCliente('')
                  }}
                />
              </CredixPanel>

              <CredixPanel title="Cobrar">
                <div className="venta-rapida-total">
                  <Text type="secondary">Total</Text>
                  <strong>{formatMoney(total)}</strong>
                </div>
                <Button
                  type="primary"
                  size="large"
                  disabled={!personaId || carrito.length === 0}
                  loading={cobrar.isPending}
                  onClick={() => cobrar.mutate()}
                  block
                >
                  Cobrar
                </Button>
                {!personaId || carrito.length === 0 ? (
                  <Text type="secondary" style={{ display: 'block', marginTop: 8 }}>
                    {!personaId ? 'Falta el cliente. ' : null}
                    {carrito.length === 0 ? 'Falta al menos un artículo.' : null}
                  </Text>
                ) : null}
              </CredixPanel>
            </aside>
          </div>
        </>
      ) : null}
    </CredixPage>
  )
}
