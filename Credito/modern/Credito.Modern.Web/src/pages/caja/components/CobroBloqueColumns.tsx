import { Button, Select, Space, Tag, Typography } from 'antd'
import {
  EnvironmentOutlined,
  PrinterOutlined,
  WhatsAppOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import type { CreditoGestorPendienteRow } from '../../../api/cajaDiario'
import {
  TipoPagoCell,
  tipoPagoOptionRender,
  tipoPagoPopupRender,
} from '../../tesoreria/components/TipoPagoBrand'
import { FechaHoraDigitalField } from './FechaHoraDigitalField'
import { MontoCobrarInput } from './MontoCobrarInput'
import {
  defaultPagoMediosSplit,
  type PagoMedioLineaUi,
} from '../../../components/caja/PagoMediosSplit'
import { formatFecha } from '../../../utils/formatFecha'
import { formatMoney } from '../../../utils/formatMoney'
import { esTipoPagoDigital } from '../../../utils/tipoPago'
import {
  clienteMapaHref,
  clienteMapaLabel,
  clienteMapaTitle,
  clienteTieneGps,
} from '../../../utils/clienteMapaNavegacion'
import type { CobroBloqueRowDraft } from '../../../utils/cobroBloqueDraft'
import {
  ticketIdsDeEnviado,
  type CobroColaDia,
  type CobroEnviadoItem,
} from '../../../utils/cobroCola'

const { Text } = Typography

export type CobroBloqueTipoPagoOption = {
  value: number
  label: string
}

export type CobroBloqueCarteraColumnDeps = {
  getEdit: (row: CreditoGestorPendienteRow) => CobroBloqueRowDraft
  patchEdit: (
    creditoId: number,
    patch: Partial<CobroBloqueRowDraft>,
    row: CreditoGestorPendienteRow,
  ) => void
  aplicarMontoRapido: (row: CreditoGestorPendienteRow) => void
  tipoPagoOptions: CobroBloqueTipoPagoOption[]
  enviadosSet: Set<number>
  cola: CobroColaDia
  esTipado: (row: CreditoGestorPendienteRow) => boolean
  cobrarFila: (row: CreditoGestorPendienteRow) => void
  enviarPending: boolean
  onAbrirMixto: (row: CreditoGestorPendienteRow, base: PagoMedioLineaUi[]) => void
}

export function buildCobroBloqueCarteraColumns(
  deps: CobroBloqueCarteraColumnDeps,
): ColumnsType<CreditoGestorPendienteRow> {
  const {
    getEdit,
    patchEdit,
    aplicarMontoRapido,
    tipoPagoOptions,
    enviadosSet,
    cola,
    esTipado,
    cobrarFila,
    enviarPending,
    onAbrirMixto,
  } = deps

  return [
    {
      title: 'Cliente',
      dataIndex: 'personaNombre',
      className: 'cobro-bloque-col-cliente',
      onCell: () => ({ className: 'cobro-bloque-col-cliente' }),
      render: (_, row) => (
        <div className="cobro-bloque-cliente-cell">
          <strong className="cobro-bloque-cliente-cell__name" title={row.personaNombre}>
            {row.personaNombre}
          </strong>
          <span className="cobro-bloque-cliente-cell__meta">
            #{row.creditoId}
            {row.diasAtrazo > 0 ? ` · ${row.diasAtrazo}d atraso` : ''}
          </span>
        </div>
      ),
    },
    {
      title: 'Celular',
      dataIndex: 'celular',
      className: 'cobro-bloque-col-celular',
      onCell: () => ({ className: 'cobro-bloque-col-celular' }),
      render: (v: string | null) => {
        const digits = (v ?? '').replace(/\D/g, '')
        if (!digits) {
          return <Text type="secondary">—</Text>
        }
        const wa = digits.startsWith('51') ? digits : `51${digits}`
        return (
          <span className="cobro-bloque-celular">
            <a href={`tel:${digits}`} className="cobro-bloque-celular__tel">
              {digits}
            </a>
            <a
              className="cobro-bloque-celular__wa"
              href={`https://wa.me/${wa}`}
              target="_blank"
              rel="noreferrer"
              aria-label="WhatsApp"
              title="WhatsApp"
            >
              <WhatsAppOutlined />
            </a>
          </span>
        )
      },
    },
    {
      title: 'Ubicación',
      key: 'ubicacion',
      className: 'cobro-bloque-col-ubicacion',
      onCell: () => ({ className: 'cobro-bloque-col-ubicacion' }),
      ellipsis: true,
      render: (_, row) => {
        const punto = {
          direccion: row.direccion,
          latitud: row.latitud,
          longitud: row.longitud,
        }
        const href = clienteMapaHref(punto)
        const label = clienteMapaLabel(punto)
        if (!href || !label) {
          return <Text type="secondary">—</Text>
        }
        return (
          <a
            className="cobro-bloque-ubicacion-link"
            href={href}
            target="_blank"
            rel="noreferrer"
            title={clienteMapaTitle(punto)}
          >
            <EnvironmentOutlined /> {label}
            {clienteTieneGps(punto) ? (
              <em className="cobro-bloque-ubicacion-link__gps">GPS</em>
            ) : null}
          </a>
        )
      },
    },
    {
      title: 'Venc.',
      dataIndex: 'fechaVencimiento',
      className: 'cobro-bloque-col-nowrap',
      onCell: () => ({ className: 'cobro-bloque-col-nowrap' }),
      render: (v: string) => formatFecha(v),
    },
    {
      title: 'Deuda',
      dataIndex: 'deudaPendiente',
      align: 'right',
      className: 'cobro-bloque-col-num',
      onCell: () => ({ className: 'cobro-bloque-col-num' }),
      render: (v: number, row) => (
        <Button
          type="link"
          size="small"
          disabled={(v || 0) <= 0}
          title="Usar como monto a cobrar"
          onClick={() => aplicarMontoRapido(row)}
        >
          <strong>{formatMoney(v)}</strong>
        </Button>
      ),
    },
    {
      title: 'Monto a cobrar',
      key: 'monto',
      className: 'cobro-bloque-col-monto',
      onCell: () => ({ className: 'cobro-bloque-col-monto' }),
      render: (_, row) => {
        const e = getEdit(row)
        return (
          <MontoCobrarInput
            min={0}
            max={row.deudaPendiente}
            step={0.01}
            value={e.montoPagar}
            className="cobro-bloque-input-monto"
            controls={false}
            inputMode="decimal"
            onChange={(montoPagar) =>
              patchEdit(
                row.creditoId,
                { montoPagar, cuotasSeleccionadas: [] },
                row,
              )
            }
            onPressEnter={(ev) => {
              const tr = (ev.target as HTMLElement).closest('tr')
              const next = tr?.nextElementSibling?.querySelector<HTMLInputElement>(
                '.ant-input-number-input',
              )
              next?.focus()
              next?.select()
            }}
          />
        )
      },
    },
    {
      title: 'Tipo pago',
      key: 'tipo',
      className: 'cobro-bloque-col-tipo',
      onCell: () => ({ className: 'cobro-bloque-col-tipo' }),
      render: (_, row) => {
        const e = getEdit(row)
        const mixto = (e.medios?.length ?? 0) >= 2
        if (mixto) {
          return <Tag color="processing">Mixto</Tag>
        }
        return (
          <Select
            value={e.tipoPagoId}
            options={tipoPagoOptions}
            className="cobro-bloque-input-tipo"
            optionRender={tipoPagoOptionRender}
            popupRender={tipoPagoPopupRender}
            popupClassName="medio-marca-dropdown"
            onChange={(v) =>
              patchEdit(row.creditoId, { tipoPagoId: v, medios: undefined }, row)
            }
          />
        )
      },
    },
    {
      title: 'Hora del voucher',
      key: 'fecha',
      className: 'cobro-bloque-col-fecha',
      onCell: () => ({ className: 'cobro-bloque-col-fecha' }),
      render: (_, row) => {
        const e = getEdit(row)
        if ((e.medios?.length ?? 0) >= 2) {
          return <Text type="secondary">Por medio</Text>
        }
        if (!esTipoPagoDigital(e.tipoPagoId)) {
          return <Text type="secondary">—</Text>
        }
        return (
          <FechaHoraDigitalField
            compact
            value={e.fechaHoraTrans}
            status={!e.fechaHoraTrans ? 'error' : undefined}
            onChange={(fechaHoraTrans) =>
              patchEdit(row.creditoId, { fechaHoraTrans }, row)
            }
          />
        )
      },
    },
    {
      title: 'Acción',
      key: 'accion',
      className: 'cobro-bloque-col-accion',
      onCell: () => ({ className: 'cobro-bloque-col-accion' }),
      render: (_, row) => {
        if (enviadosSet.has(row.creditoId)) {
          return <Tag color="success">Enviado</Tag>
        }
        const errorMsg = cola.errores[String(row.creditoId)]
        const tipado = esTipado(row)
        const e = getEdit(row)
        const esMixto = (e.medios?.length ?? 0) >= 2
        return (
          <Space size={4} wrap>
            <Button
              size="small"
              disabled={e.montoPagar <= 0}
              onClick={() => {
                const base =
                  e.medios && e.medios.length >= 2
                    ? e.medios.map((m) => ({
                        key: `m-${m.tipoPagoId}-${m.importe}`,
                        tipoPagoId: m.tipoPagoId,
                        importe: m.importe,
                        fechaTransferencia: m.fechaHoraTrans,
                      }))
                    : defaultPagoMediosSplit(tipoPagoOptions, e.montoPagar)
                onAbrirMixto(row, base)
              }}
            >
              {esMixto ? 'Editar mixto' : 'Dividir'}
            </Button>
            <Button
              type="primary"
              size="small"
              loading={enviarPending}
              disabled={!tipado}
              onClick={() => cobrarFila(row)}
            >
              Cobrar
            </Button>
            {errorMsg ? (
              <Text type="danger" title={errorMsg}>
                Error
              </Text>
            ) : null}
          </Space>
        )
      },
    },
  ]
}

export type CobroBloqueEnviadosColumnDeps = {
  tipoPagoOptions: CobroBloqueTipoPagoOption[]
  ticketPrintingId: number | null
  imprimirTicket: (ids: number | number[]) => void | Promise<void>
}

export function buildCobroBloqueEnviadosColumns(
  deps: CobroBloqueEnviadosColumnDeps,
): ColumnsType<CobroEnviadoItem> {
  const { tipoPagoOptions, ticketPrintingId, imprimirTicket } = deps

  return [
    {
      title: 'Cliente',
      dataIndex: 'personaNombre',
      render: (_, it) => (
        <div className="cobro-bloque-cliente-cell">
          <strong className="cobro-bloque-cliente-cell__name" title={it.personaNombre}>
            {it.personaNombre}
          </strong>
          <span className="cobro-bloque-cliente-cell__meta">#{it.creditoId}</span>
        </div>
      ),
    },
    {
      title: 'Monto',
      dataIndex: 'montoPagar',
      align: 'right',
      render: (v: number) => <strong>S/ {formatMoney(v)}</strong>,
    },
    {
      title: 'Tipo pago',
      dataIndex: 'tipoPagoId',
      render: (_v: number, it) => {
        const medios =
          it.medios && it.medios.length >= 2
            ? it.medios
            : [{ tipoPagoId: it.tipoPagoId, importe: it.montoPagar }]
        if (medios.length >= 2) {
          return (
            <div className="cobro-enviado-medios" translate="no">
              {medios.map((m, i) => {
                const label =
                  tipoPagoOptions.find((o) => o.value === m.tipoPagoId)?.label ??
                  `Tipo ${m.tipoPagoId}`
                return (
                  <div key={`${it.creditoId}-${m.tipoPagoId}-${i}`} className="cobro-enviado-medios__row">
                    <TipoPagoCell tipoPago={label} />
                    <span className="cobro-enviado-medios__monto">
                      S/ {formatMoney(m.importe)}
                    </span>
                  </div>
                )
              })}
            </div>
          )
        }
        const label =
          tipoPagoOptions.find((o) => o.value === medios[0].tipoPagoId)?.label ??
          `Tipo ${medios[0].tipoPagoId}`
        return <TipoPagoCell tipoPago={label} />
      },
    },
    {
      title: 'Origen',
      dataIndex: 'origen',
      render: (v: CobroEnviadoItem['origen']) =>
        v === 'cobrar_ya'
          ? 'Cobro'
          : v === 'sync'
            ? 'Sincronizado'
            : v === 'diario'
              ? 'Diario'
              : 'Lote',
    },
    {
      title: 'Hora',
      dataIndex: 'enviadoAt',
      render: (v: string) =>
        new Date(v).toLocaleTimeString('es-PE', { hour: '2-digit', minute: '2-digit' }),
    },
    {
      title: 'Ticket',
      key: 'ticket',
      width: 88,
      align: 'center',
      render: (_, it) => {
        const ids = ticketIdsDeEnviado(it)
        if (ids.length === 0) {
          return <Text type="secondary">—</Text>
        }
        return (
          <Button
            type="primary"
            size="small"
            icon={<PrinterOutlined />}
            loading={
              ticketPrintingId != null && ids.includes(ticketPrintingId)
            }
            aria-label={`Imprimir ticket crédito ${it.creditoId}`}
            title={
              ids.length > 1 ? 'Imprimir comprobante mixto' : 'Imprimir ticket'
            }
            onClick={() => void imprimirTicket(ids)}
          />
        )
      },
    },
  ]
}
