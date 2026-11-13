import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type Key,
} from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Checkbox,
  Grid,
  InputNumber,
  Space,
} from 'antd'
import {
  CheckCircleOutlined,
  CreditCardOutlined,
  DollarOutlined,
  DownOutlined,
  FilePdfOutlined,
  FileSearchOutlined,
  HistoryOutlined,
  TeamOutlined,
  UnorderedListOutlined,
  UpOutlined,
  WalletOutlined,
} from '@ant-design/icons'
import {
  completarImpagos,
  completarImpagosValidacion,
  fetchCreditoMoraResumen,
  fetchCreditosPorPersona,
  fetchCuentasPorCobrarPendientes,
  pagarCuotaConMora,
  pagarCuotaImporteLibre,
  pagarCuotaImporteLibreMixto,
  pagarCuotas,
} from '../../../api/cajaDiario'
import { downloadMovimientosCreditoPdf } from '../../../api/creditoPlanes'
import { fetchCreditoContexto } from '../../../api/creditoGestion'
import { ApiError } from '../../../api/errors'
import { CajaModal } from '../../../components/caja/CajaModal'
import {
  cajaToastError,
  cajaToastInfo,
  cajaToastSuccess,
  cajaToastWarning,
} from './cajaFeedback'
import { cajaConfirm } from '../../../components/caja/cajaConfirm'
import { CreditoMoraModal } from '../../../components/caja/CreditoMoraModal'
import { fetchValoresTabla } from '../../../api/maestros'
import {
  fetchCondonacionPendienteCredito,
  solicitarCondonacion,
} from '../../../api/creditoCondonacion'
import { CajaCuotasTable } from '../../../components/caja/CajaCuotasTable'
import {
  loadCuotasCobranzaGrid,
  type CuotaCobranzaRow,
} from '../../../components/caja/cuotasGridMerge'
import { CajaSection } from '../../../components/caja/CajaSection'
import { ClienteBuscarAutoComplete } from '../../../components/caja/ClienteBuscarAutoComplete'
import { TipoPagoChips } from '../../../components/caja/TipoPagoChips'
import {
  defaultPagoMediosSplit,
  PagoMediosSplit,
  sumPagoMedios,
  validatePagoMedios,
  type PagoMedioLineaUi,
} from '../../../components/caja/PagoMediosSplit'
import { buildTipoPagoOptions } from '../../tesoreria/components/TipoPagoBrand'
import { useEsCentral } from '../../../hooks/useEsCentral'
import {
  FechaHoraDigitalField,
  nowDatetimeLocal,
} from '../components/FechaHoraDigitalField'
import {
  contarCuotasCobrables,
  contarCuotasConMora,
  isCuotaSelectable,
} from '../../../components/caja/cuotaRowStyle'
import { sumarMoraVigente } from '../../../components/caja/creditoMoraVista'
import type { CreditoPorPersonaRow } from '../../../types/api'
import { formatMoney } from '../../../utils/formatMoney'
import { moneyEquals, roundMoney, sumMoney } from '../../../utils/money'
import { esTipoPagoDigital } from '../../../utils/tipoPago'
import { toLegacyFechaTransferencia } from '../../../utils/formatFecha'
import { CancelacionCreditoPanel } from './CancelacionCreditoPanel'
import { CxcInlinePanel } from './CxcInlinePanel'
import { CreditosPendientesModal } from './CreditosPendientesModal'
import type { CajaSession } from './types'
import { errMsg } from './types'
import { useAuth } from '../../../auth/useAuth'
import { syncDiarioPagoHaciaCobro } from '../../../utils/cajaOperacionSync'
import { assertSinCxcPendiente } from './cajaPagoHelpers'

export function CobranzasTab({
  ctx,
  creditoIdInicial,
  onChanged,
}: {
  ctx: CajaSession
  creditoIdInicial: string | null
  onChanged: () => void
}) {
  const { session } = useAuth()
  const { esCentral } = useEsCentral()
  const usuarioId = session?.usuarioId ?? 0
  const [clienteLabel, setClienteLabel] = useState('')
  const [personaId, setPersonaId] = useState(0)
  const [creditosPersona, setCreditosPersona] = useState<CreditoPorPersonaRow[]>(
    [],
  )
  const [creditoId, setCreditoId] = useState<number | null>(() => {
    if (!creditoIdInicial) {
      return null
    }
    const id = Number(creditoIdInicial)
    return Number.isNaN(id) || id < 1 ? null : id
  })
  const [cuotas, setCuotas] = useState<CuotaCobranzaRow[]>([])
  const [selectedKeys, setSelectedKeys] = useState<Key[]>([])
  const [importeRecibido, setImporteRecibido] = useState<number | null>(null)
  const [tipoPagoId, setTipoPagoId] = useState(1)
  const [tipoPagoLibreId, setTipoPagoLibreId] = useState(1)
  const [pagoLibre, setPagoLibre] = useState<number | null>(null)
  const [pagoLibreMixto, setPagoLibreMixto] = useState(false)
  const [mediosLibre, setMediosLibre] = useState<PagoMedioLineaUi[]>([])
  const [cuotaMixto, setCuotaMixto] = useState(false)
  const [mediosCuota, setMediosCuota] = useState<PagoMedioLineaUi[]>([])
  const [fechaTransferencia, setFechaTransferencia] = useState('')
  const [fechaPagoLibre, setFechaPagoLibre] = useState('')
  const [showCxc, setShowCxc] = useState(false)
  const [cuotasModalOpen, setCuotasModalOpen] = useState(false)
  const [consultandoImpagos, setConsultandoImpagos] = useState(false)
  const [moraModalOpen, setMoraModalOpen] = useState(false)
  const [solicitarOpen, setSolicitarOpen] = useState(false)
  const [moraSolicitar, setMoraSolicitar] = useState<number | null>(null)
  const [modoPago, setModoPago] = useState<'cuotas' | 'libre'>('cuotas')
  /** Móvil: sheet de pago expandido; colapsado deja ver plan/tablas. */
  const [pagoDockExpanded, setPagoDockExpanded] = useState(true)
  const [dockBox, setDockBox] = useState<{ left: number; width: number } | null>(
    null,
  )
  const screens = Grid.useBreakpoint()
  const isMobile = screens.md !== true
  const moraResumenQuery = useQuery({
    queryKey: ['credito-mora-resumen', creditoId],
    queryFn: () => fetchCreditoMoraResumen(creditoId!),
    enabled: creditoId != null && creditoId > 0,
  })
  const condonacionPendienteQuery = useQuery({
    queryKey: ['condonacion-pendiente', creditoId],
    queryFn: () => fetchCondonacionPendienteCredito(creditoId!),
    enabled: creditoId != null && creditoId > 0,
  })
  const tiposPagoQuery = useQuery({
    queryKey: ['valores-tabla', 13],
    queryFn: () => fetchValoresTabla(13),
    staleTime: 5 * 60_000,
  })

  const tipoPagoOptions = useMemo(
    () => buildTipoPagoOptions(tiposPagoQuery.data ?? [], { esCentral }),
    [tiposPagoQuery.data, esCentral],
  )

  useEffect(() => {
    const main = document.querySelector('.credix-main')
    if (!main) return
    const sync = () => {
      const r = main.getBoundingClientRect()
      setDockBox({ left: Math.max(0, r.left), width: Math.max(280, r.width) })
    }
    sync()
    const ro = new ResizeObserver(sync)
    ro.observe(main)
    window.addEventListener('resize', sync)
    return () => {
      ro.disconnect()
      window.removeEventListener('resize', sync)
    }
  }, [])

  useEffect(() => {
    if (esTipoPagoDigital(tipoPagoId)) {
      setFechaTransferencia((prev) => prev.trim() || nowDatetimeLocal())
    }
  }, [tipoPagoId])

  useEffect(() => {
    if (esTipoPagoDigital(tipoPagoLibreId)) {
      setFechaPagoLibre((prev) => prev.trim() || nowDatetimeLocal())
    }
  }, [tipoPagoLibreId])

  const buscar = useMutation({
    mutationFn: ({
      creditoId: id,
      silent,
    }: {
      creditoId: number
      silent?: boolean
    }) => loadCuotasCobranzaGrid(id).then((data) => ({ data, silent })),
    onSuccess: ({ data, silent }) => {
      setCuotas(data)
      setSelectedKeys([])
      setImporteRecibido(null)
      if (silent) {
        return
      }
      // La grilla ya refleja el plan; no apilar toasts de carga rutinaria.
      if (data.length === 0) {
        cajaToastInfo('Sin cuotas para este crédito', 'caja-plan')
      }
    },
    onError: (e) => cajaToastError(errMsg(e)),
  })

  const cargarCuotas = (id: number, options?: { silent?: boolean }) => {
    setCreditoId(id)
    // Móvil: sheet colapsado al cargar → se ve el plan; se abre al elegir cuotas.
    setPagoDockExpanded(!isMobile)
    buscar.mutate({ creditoId: id, silent: options?.silent })
  }

  useEffect(() => {
    if (!creditoId) {
      setPagoDockExpanded(true)
    }
  }, [creditoId])

  useEffect(() => {
    if (!creditoIdInicial) {
      return
    }
    const id = Number(creditoIdInicial)
    if (id > 0) {
      cargarCuotas(id, { silent: true })
    }
    // Solo al cambiar el crédito en la URL; no incluir cargarCuotas (evita bucle).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [creditoIdInicial])

  const cargarCreditos = async (
    pid: number,
    label: string,
    preferCreditoId?: number,
  ) => {
    setPersonaId(pid)
    setClienteLabel(label)
    const list = await fetchCreditosPorPersona(pid, ctx.esCajaCentral)
    setCreditosPersona(list)

    const cxc = await fetchCuentasPorCobrarPendientes(
      ctx.oficinaId,
      ctx.cajaDiarioId,
      pid,
    )
    if (cxc.length > 0) {
      setShowCxc(true)
    }

    if (list.length === 0) {
      cajaToastInfo('El cliente no tiene créditos activos para cobrar', 'caja-cliente')
      return
    }

    const preferido =
      preferCreditoId && list.some((c) => c.creditoId === preferCreditoId)
        ? preferCreditoId
        : list[0].creditoId
    cargarCuotas(preferido, { silent: true })

    if (list.length > 1) {
      cajaToastInfo(
        `${list.length} créditos activos · se cargó el ${preferido}`,
        'caja-cliente',
      )
    }
  }

  /** Buscador único: si no hay cliente, intentar nro. de crédito. */
  const cargarPorCreditoId = useCallback(
    async (term: string): Promise<boolean> => {
      const id = Number(term.trim())
      if (!Number.isFinite(id) || id < 1 || !/^\d{4,10}$/.test(term.trim())) {
        return false
      }
      try {
        const info = await fetchCreditoContexto(id)
        const nombre = (info.personaNombre ?? '').trim()
        const label = nombre
          ? `${nombre} · Crédito ${id}`
          : `Crédito ${id}`
        await cargarCreditos(info.personaId, label, id)
        return true
      } catch {
        return false
      }
    },
    // cargarCreditos cierra sobre ctx / setters estables en este tab.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [ctx.esCajaCentral, ctx.oficinaId, ctx.cajaDiarioId],
  )

  const creditoSel = creditosPersona.find((c) => c.creditoId === creditoId)

  const cuotasPagables = useMemo(
    () => cuotas.filter(isCuotaSelectable),
    [cuotas],
  )

  const resumenCuotas = useMemo(() => {
    const cobrables = contarCuotasCobrables(cuotas)
    const mora = contarCuotasConMora(cuotas)
    const moraVigente = sumarMoraVigente(cuotas)
    return { cobrables, mora, moraVigente, filas: cuotas.length }
  }, [cuotas])

  const prepararCobroTodasCuotas = useCallback(() => {
    const ids = cuotasPagables
      .map((c) => c.planPagoId)
      .filter((id): id is number => id != null && id > 0)
    setSelectedKeys(ids)
    const total = sumMoney(
      cuotasPagables.map((c) => c.pagoCuota ?? c.cuota ?? 0),
    )
    setImporteRecibido(total)
    cajaToastInfo(
      `${ids.length} cuota(s) lista(s) para cobrar`,
      'caja-seleccion',
    )
  }, [cuotasPagables])



  const totalCuotasCredito = useMemo(
    () => sumMoney(cuotasPagables.map((c) => c.pagoCuota ?? c.cuota ?? 0)),
    [cuotasPagables],
  )

  const pagar = useMutation({
    mutationFn: async () => {
      if (cuotaMixto) {
        const esperado = roundMoney(importeRecibido ?? selectedTotal)
        const err = validatePagoMedios(mediosCuota, esperado)
        if (err) throw new Error(err)
        return pagarCuotaImporteLibreMixto({
          oficinaId: ctx.oficinaId,
          cajaDiarioId: ctx.cajaDiarioId,
          creditoId: creditoId!,
          medios: mediosCuota.map((l) => ({
            tipoPagoId: l.tipoPagoId,
            importe: roundMoney(l.importe ?? 0),
            fechaPagoTransferencia:
              esTipoPagoDigital(l.tipoPagoId) && l.fechaTransferencia
                ? toLegacyFechaTransferencia(l.fechaTransferencia) ?? null
                : null,
          })),
        })
      }
      return pagarCuotas({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        creditoId: creditoId!,
        listaPlanPagoId: selectedKeys.join(','),
        importeRecibido: importeRecibido ?? 0,
        tipoPagoId,
        fechaPagoTransferencia:
          esTipoPagoDigital(tipoPagoId) && fechaTransferencia
            ? toLegacyFechaTransferencia(fechaTransferencia)
            : undefined,
        esUltimaCuota:
          selectedKeys.length > 0 && selectedKeys.length === cuotasPagables.length,
        aplicarMoraPostergada: moraResumenQuery.data?.indMoraProducto === true,
      })
    },
    onSuccess: (r) => {
      const ids =
        'movimientoCajaIds' in r && Array.isArray(r.movimientoCajaIds)
          ? r.movimientoCajaIds
          : null
      cajaToastSuccess(
        cuotaMixto
          ? `Cobro mixto registrado (${ids?.length ?? 1} mov.)`
          : `Pago registrado (${r.resultId ?? 'OK'})`,
      )
      if (creditoId && usuarioId > 0) {
        syncDiarioPagoHaciaCobro({
          usuarioId,
          oficinaId: ctx.oficinaId,
          cajaDiarioId: ctx.cajaDiarioId,
          creditoId,
          personaNombre: clienteLabel || creditoSel?.descripcion,
          montoPagar: cuotaMixto ? sumPagoMedios(mediosCuota) : (importeRecibido ?? 0),
          tipoPagoId: cuotaMixto ? (mediosCuota[0]?.tipoPagoId ?? 1) : tipoPagoId,
          movimientoCajaId: r.resultId ?? null,
        })
      }
      setCuotaMixto(false)
      setMediosCuota([])
      onChanged()
      void moraResumenQuery.refetch()
      if (creditoId) {
        buscar.mutate({ creditoId, silent: true })
      }
    },
    onError: (e) => cajaToastError(errMsg(e)),
  })

  const solicitar = useMutation({
    mutationFn: () => {
      const mora = Number(moraSolicitar ?? 0)
      if (!creditoId || creditoId < 1) {
        return Promise.reject(new Error('Seleccione un crédito.'))
      }
      if (!(mora > 0)) {
        return Promise.reject(
          new Error('Indique un monto de mora mayor a 0 para solicitar la condonación.'),
        )
      }
      return solicitarCondonacion({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        creditoId,
        moraCondonacion: roundMoney(mora),
      })
    },
    onSuccess: () => {
      cajaToastSuccess('Solicitud de condonación registrada')
      setSolicitarOpen(false)
      void condonacionPendienteQuery.refetch()
    },
    onError: (e) => {
      const detail =
        e instanceof ApiError
          ? e.problem?.detail || e.message
          : errMsg(e)
      cajaToastError(detail || 'No se pudo registrar la solicitud de condonación.')
    },
  })

  const ejecutarPagoLibre = useMutation({
    mutationFn: async () => {
      if (pagoLibreMixto) {
        const err = validatePagoMedios(mediosLibre)
        if (err) throw new Error(err)
        return pagarCuotaImporteLibreMixto({
          oficinaId: ctx.oficinaId,
          cajaDiarioId: ctx.cajaDiarioId,
          creditoId: creditoId!,
          medios: mediosLibre.map((l) => ({
            tipoPagoId: l.tipoPagoId,
            importe: roundMoney(l.importe ?? 0),
            fechaPagoTransferencia:
              esTipoPagoDigital(l.tipoPagoId) && l.fechaTransferencia
                ? toLegacyFechaTransferencia(l.fechaTransferencia) ?? null
                : null,
          })),
        })
      }
      const esUltima = cuotasPagables.length <= 1
      const usarMoraIntegrada =
        (moraResumenQuery.data?.indMoraProducto ?? false) && esUltima
      if (usarMoraIntegrada) {
        return pagarCuotaConMora({
          oficinaId: ctx.oficinaId,
          cajaDiarioId: ctx.cajaDiarioId,
          creditoId: creditoId!,
          importeRecibido: pagoLibre ?? 0,
          esUltimaCuota: true,
          tipoPagoId: tipoPagoLibreId,
          fechaPagoTransferencia:
            esTipoPagoDigital(tipoPagoLibreId) && fechaPagoLibre
              ? toLegacyFechaTransferencia(fechaPagoLibre) ?? null
              : null,
        })
      }
      return pagarCuotaImporteLibre({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
        creditoId: creditoId!,
        importeRecibido: pagoLibre ?? 0,
        tipoPagoId: tipoPagoLibreId,
        fechaPagoTransferencia:
          esTipoPagoDigital(tipoPagoLibreId) && fechaPagoLibre
            ? toLegacyFechaTransferencia(fechaPagoLibre) ?? null
            : null,
      })
    },
    onSuccess: (r) => {
      const msg =
        'mensaje' in r && r.mensaje
          ? r.mensaje
          : pagoLibreMixto
            ? 'Cobro mixto registrado'
            : 'Pago libre registrado'
      cajaToastSuccess(msg)
      const movId =
        'movimientoCajaId' in r
          ? r.movimientoCajaId
          : 'resultId' in r
            ? r.resultId
            : null
      const montoSync = pagoLibreMixto ? sumPagoMedios(mediosLibre) : (pagoLibre ?? 0)
      const tipoSync = pagoLibreMixto
        ? (mediosLibre[0]?.tipoPagoId ?? 1)
        : tipoPagoLibreId
      if (creditoId && usuarioId > 0) {
        syncDiarioPagoHaciaCobro({
          usuarioId,
          oficinaId: ctx.oficinaId,
          cajaDiarioId: ctx.cajaDiarioId,
          creditoId,
          personaNombre: clienteLabel || creditoSel?.descripcion,
          montoPagar: montoSync,
          tipoPagoId: tipoSync,
          movimientoCajaId: movId ?? null,
        })
      }
      setPagoLibre(null)
      setPagoLibreMixto(false)
      setMediosLibre([])
      onChanged()
      void moraResumenQuery.refetch()
      if (creditoId) {
        buscar.mutate({ creditoId, silent: true })
      }
    },
    onError: (e) => {
      cajaToastError(errMsg(e) || 'No se pudo registrar el pago libre.')
    },
  })

  const ejecutarCompletarImpagos = useMutation({
    mutationFn: () =>
      completarImpagos({
        oficinaId: ctx.oficinaId,
        cajaDiarioId: ctx.cajaDiarioId,
      }),
    onSuccess: (r) => {
      if (r.success) {
        cajaToastSuccess('Impagos completados')
      } else {
        cajaToastWarning(
          'La base de datos no completó los impagos. Consulte con sistemas.',
        )
      }
      onChanged()
    },
    onError: (e) => cajaToastError(errMsg(e)),
  })

  const solicitarCompletarImpagos = async () => {
    setConsultandoImpagos(true)
    try {
      const validacion = await completarImpagosValidacion(
        ctx.oficinaId,
        ctx.cajaDiarioId,
      )
      const pendientes = validacion.cantidadImpagosPendientes ?? 0
      if (pendientes <= 0) {
        cajaToastInfo('No hay créditos impagos pendientes para completar.')
        return
      }
      cajaConfirm({
        title: 'Completar impagos',
        content: `Hay ${pendientes} crédito(s) sin cobro registrado hoy. Se registrará un movimiento CUO de S/ 0.00 (pago libre 0) en cada uno, para poder cerrar la caja. ¿Continuar?`,
        onOk: () => ejecutarCompletarImpagos.mutateAsync(),
      })
    } catch (e) {
      cajaToastError(errMsg(e))
    } finally {
      setConsultandoImpagos(false)
    }
  }

  const solicitarPagoLibre = async () => {
    if (!creditoId) return

      if (pagoLibreMixto) {
      const totalMixto = sumPagoMedios(mediosLibre)
      const err = validatePagoMedios(mediosLibre, totalMixto)
      if (err) {
        cajaToastWarning(err)
        return
      }
      if (totalMixto > totalCuotasCredito && totalCuotasCredito > 0) {
        cajaToastError(
          `El cobro mixto no debe superar ${formatMoney(totalCuotasCredito)}`,
        )
        return
      }
      try {
        if (!(await assertSinCxcPendiente(creditoId))) return
        const detalle = mediosLibre
          .map((l) => {
            const lab =
              tipoPagoOptions.find((o) => o.value === l.tipoPagoId)?.label ??
              `Tipo ${l.tipoPagoId}`
            return `${lab} ${formatMoney(l.importe ?? 0)}`
          })
          .join(' + ')
        cajaConfirm({
          title: 'Confirmar cobro mixto',
          content: `¿Registrar ${formatMoney(totalMixto)} (${detalle})?`,
          onOk: () => ejecutarPagoLibre.mutateAsync(),
        })
      } catch (e) {
        cajaToastError(errMsg(e))
      }
      return
    }

    if (!pagoLibre || pagoLibre <= 0) {
      return
    }
    if (pagoLibre > totalCuotasCredito && totalCuotasCredito > 0) {
      cajaToastError(
        `El pago libre no debe superar ${formatMoney(totalCuotasCredito)}`,
      )
      return
    }
    try {
      if (!(await assertSinCxcPendiente(creditoId))) {
        return
      }
      if (esTipoPagoDigital(tipoPagoLibreId) && !fechaPagoLibre.trim()) {
        cajaToastWarning('Indique la hora del voucher en el panel de cobro')
        return
      }
      const tipoLabel =
        tipoPagoOptions.find((o) => o.value === tipoPagoLibreId)?.label ??
        'pago'
      cajaConfirm({
        title: 'Confirmar pago libre',
        content:
          esTipoPagoDigital(tipoPagoLibreId)
            ? `¿Registrar ${formatMoney(pagoLibre)} por ${tipoLabel}?`
            : `¿Registrar pago libre de ${formatMoney(pagoLibre)}?`,
        onOk: () => ejecutarPagoLibre.mutateAsync(),
      })
    } catch (e) {
      cajaToastError(errMsg(e))
    }
  }

  const solicitarPagarCuota = async () => {
    if (!creditoId || selectedKeys.length === 0) {
      return
    }
    if (importeRecibido == null || importeRecibido < selectedTotal) {
      cajaToastError('El importe recibido debe ser mayor o igual al total de pago')
      return
    }
    try {
      if (!(await assertSinCxcPendiente(creditoId))) {
        return
      }

      if (cuotaMixto) {
        // Mixto usa pago libre (FIFO): debe ser el prefijo de cuotas pendientes (sin saltar).
        const prefijoIds = cuotasPagables
          .slice(0, selectedKeys.length)
          .map((c) => c.planPagoId)
          .filter((id): id is number => id != null && id > 0)
        const seleccion = new Set(selectedKeys.map(Number))
        const esPrefijo =
          prefijoIds.length === seleccion.size &&
          prefijoIds.every((id) => seleccion.has(id))
        if (!esPrefijo) {
          cajaToastError(
            'En cobro mixto seleccione las primeras cuotas pendientes (sin saltar). El sistema aplica el monto FIFO.',
          )
          return
        }
        const esperado = roundMoney(importeRecibido)
        if (!moneyEquals(esperado, roundMoney(selectedTotal))) {
          cajaToastError(
            'En cobro mixto el importe recibido debe coincidir al céntimo con el total de cuotas seleccionadas.',
          )
          return
        }
        const err = validatePagoMedios(mediosCuota, esperado)
        if (err) {
          cajaToastWarning(err)
          return
        }
        const detalle = mediosCuota
          .map((l) => {
            const lab =
              tipoPagoOptions.find((o) => o.value === l.tipoPagoId)?.label ??
              `Tipo ${l.tipoPagoId}`
            return `${lab} ${formatMoney(l.importe ?? 0)}`
          })
          .join(' + ')
        cajaConfirm({
          title: 'Confirmar cobro mixto',
          content: `¿Cobrar ${selectedKeys.length} cuota(s) · ${formatMoney(esperado)} (${detalle})?`,
          onOk: () => pagar.mutateAsync(),
        })
        return
      }

      if (esTipoPagoDigital(tipoPagoId) && !fechaTransferencia.trim()) {
        cajaToastWarning('Indique la hora del voucher en el panel de cobro')
        return
      }
      const tipoLabel =
        tipoPagoOptions.find((o) => o.value === tipoPagoId)?.label ?? 'pago'
      cajaConfirm({
        title: 'Confirmar cobro',
        content:
          esTipoPagoDigital(tipoPagoId)
            ? `¿Cobrar ${selectedKeys.length} cuota(s) · ${formatMoney(selectedTotal)} por ${tipoLabel}?`
            : `¿Cobrar ${selectedKeys.length} cuota(s) por ${formatMoney(selectedTotal)}?`,
        onOk: () => pagar.mutateAsync(),
      })
    } catch (e) {
      cajaToastError(errMsg(e))
    }
  }

  const descargarMovimientosCredito = async (id: number) => {
    try {
      await downloadMovimientosCreditoPdf(id)
    } catch (e) {
      cajaToastError(errMsg(e))
    }
  }

  const handleSelectionChange = useCallback(
    (keys: Key[]) => {
      const valid = keys.filter((k) =>
        cuotasPagables.some((c) => c.planPagoId === k),
      )
      setSelectedKeys(valid)
      const total = sumMoney(
        cuotasPagables
          .filter((c) => c.planPagoId != null && valid.includes(c.planPagoId))
          .map((c) => c.pagoCuota ?? c.cuota ?? 0),
      )
      setImporteRecibido(total)
      if (isMobile && valid.length > 0) {
        setPagoDockExpanded(true)
      }
    },
    [cuotasPagables, isMobile],
  )

  const selectedTotal = useMemo(
    () =>
      sumMoney(
        cuotasPagables
          .filter(
            (c) => c.planPagoId != null && selectedKeys.includes(c.planPagoId),
          )
          .map((c) => c.pagoCuota ?? c.cuota ?? 0),
      ),
    [cuotasPagables, selectedKeys],
  )

  const vuelto =
    importeRecibido != null
      ? roundMoney(importeRecibido - selectedTotal)
      : null

  const showPagoDock = Boolean(creditoId) && !ctx.indCierre
  const dockCollapsed = isMobile && showPagoDock && !pagoDockExpanded
  /** PC: panel en flujo (no tapa la grilla). Móvil: dock fijo inferior. */
  const pagoDockClass = isMobile
    ? 'caja-diario-pago-dock-fixed'
    : 'caja-diario-pago-dock-panel'

  const dockSummary =
    modoPago === 'cuotas'
      ? `Total ${formatMoney(selectedTotal)} · ${selectedKeys.length} cuota(s)`
      : `Libre ${pagoLibre != null ? formatMoney(pagoLibre) : '—'}`

  return (
    <div
      className={[
        'caja-diario-cobranzas',
        showPagoDock && isMobile ? 'caja-diario-cobranzas--dock' : '',
        dockCollapsed ? 'caja-diario-cobranzas--dock-collapsed' : '',
        isMobile ? 'caja-diario-cobranzas--mobile' : '',
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <Alert
        type="info"
        showIcon
        className="caja-diario-cobro-bridge"
        message="Cobro en campo"
        action={
          <Link to="/caja/cobro">
            <Button
              type="primary"
              icon={<TeamOutlined />}
              disabled={ctx.indCierre}
              block={isMobile}
            >
              Ir a Cobro
            </Button>
          </Link>
        }
      />
      <div
        className={[
          'caja-diario-cobranzas-top',
          isMobile && (personaId > 0 || creditoId)
            ? 'caja-diario-cobranzas-top--compact'
            : '',
        ]
          .filter(Boolean)
          .join(' ')}
      >
        <div className="caja-diario-cobranzas-top__search">
          <span className="caja-diario-cobranzas-top__label" id="caja-buscar-cliente-label">
            Buscar cliente
          </span>
          <ClienteBuscarAutoComplete
            value={clienteLabel}
            onChange={setClienteLabel}
            onSelectPersona={(pid, label) => {
              void cargarCreditos(pid, label)
            }}
            autoFocus
            fullWidth
            showSearchButton
            searchButtonLabel={isMobile ? 'Ir' : 'Cargar'}
            ariaLabelledBy="caja-buscar-cliente-label"
            placeholder="DNI, nombre, código o nro. de crédito"
            minChars={2}
            debounceMs={220}
            autoSelectSingle
            onMiss={cargarPorCreditoId}
          />
          {personaId > 0 || creditoId ? (
            <Button
              type="link"
              className="caja-diario-cobranzas-top__reset"
              onClick={() => {
                setClienteLabel('')
                setPersonaId(0)
                setCreditosPersona([])
                setCreditoId(null)
                setCuotas([])
                setSelectedKeys([])
                setImporteRecibido(null)
                setPagoLibre(null)
                setShowCxc(false)
                setModoPago('cuotas')
                setPagoDockExpanded(true)
              }}
            >
              Nuevo cobro
            </Button>
          ) : null}
        </div>
        <div
          className="caja-diario-actions-row"
          role="toolbar"
          aria-label="Acciones de cobranza"
        >
          <Button
            icon={<WalletOutlined />}
            type={showCxc ? 'primary' : 'default'}
            onClick={() => {
              setShowCxc(true)
              setPersonaId(0)
            }}
          >
            {isMobile ? 'GAD' : 'GAD pendientes'}
          </Button>
          <Button
            icon={<UnorderedListOutlined />}
            onClick={() => setCuotasModalOpen(true)}
          >
            {isMobile ? 'Pendientes' : 'Cuotas pendientes'}
          </Button>
        </div>
      </div>

      {showCxc ? (
        <CxcInlinePanel
          ctx={ctx}
          personaId={personaId}
          clienteLabel={clienteLabel}
          onChanged={onChanged}
        />
      ) : null}

      {moraResumenQuery.data?.indMoraProducto &&
      (resumenCuotas.moraVigente > 0 ||
        (moraResumenQuery.data.saldoPostergado ?? 0) > 0) ? (
        <Alert
          type="warning"
          showIcon
          className="caja-diario-mora-banner"
          message={
            <>
              Mora:{' '}
              <strong>{formatMoney(resumenCuotas.moraVigente)}</strong>
              {(moraResumenQuery.data.saldoPostergado ?? 0) > 0
                ? ` · postergada ${formatMoney(moraResumenQuery.data.saldoPostergado)}`
                : ''}
            </>
          }
          action={
            <Button size="small" onClick={() => setMoraModalOpen(true)}>
              Detalle
            </Button>
          }
        />
      ) : null}

      <CajaSection
        tone="credito"
        kicker="Crédito activo"
        title={creditoId ?? '—'}
        subtitle={clienteLabel || creditoSel?.descripcion}
        icon={<FileSearchOutlined />}
      >
        {creditosPersona.length > 1 ? (
          <div className="caja-diario-creditos-chips">
            {creditosPersona.map((c) => (
              <div key={c.creditoId} className="caja-diario-credito-chip-wrap">
                <button
                  type="button"
                  className={`caja-diario-credito-chip${creditoId === c.creditoId ? ' is-active' : ''}`}
                  onClick={() => cargarCuotas(c.creditoId)}
                >
                  <span className="caja-diario-credito-chip-id">
                    Crédito {c.creditoId}
                  </span>
                  <span className="caja-diario-credito-chip-monto">
                    S/. {formatMoney(c.montoCredito)}
                  </span>
                </button>
                {!isMobile ? (
                  <Button
                    size="small"
                    type="link"
                    icon={<FilePdfOutlined />}
                    className="caja-diario-credito-chip-mov"
                    onClick={() => void descargarMovimientosCredito(c.creditoId)}
                  >
                    Movimientos
                  </Button>
                ) : null}
              </div>
            ))}
          </div>
        ) : null}

        {creditoId ? (
          <Space wrap className="caja-diario-credito-filters">
            <Button
              icon={<HistoryOutlined />}
              onClick={() => setMoraModalOpen(true)}
            >
              {isMobile ? 'Mora' : 'Crédito mora'}
            </Button>
            <Button
              onClick={() => {
                const mora =
                  condonacionPendienteQuery.data?.tienePendiente
                    ? condonacionPendienteQuery.data.moraCondonacion
                    : sumarMoraVigente(cuotas)
                setMoraSolicitar(roundMoney(mora))
                setSolicitarOpen(true)
              }}
            >
              {isMobile ? 'Condonar' : 'Solicitar condonación'}
            </Button>
            {creditosPersona.length === 1 && !isMobile ? (
              <Button
                type="link"
                icon={<FilePdfOutlined />}
                onClick={() => void descargarMovimientosCredito(creditoId)}
              >
                Movimientos
              </Button>
            ) : null}
          </Space>
        ) : null}
      </CajaSection>

      <CajaSection
        tone="brand"
        kicker="Plan de pagos"
        title="Plan de cuotas"
        subtitle={
          creditoId
            ? `${resumenCuotas.filas} · ${resumenCuotas.cobrables} cobrable(s)${resumenCuotas.mora > 0 ? ` · ${resumenCuotas.mora} mora` : ''}`
            : 'Seleccione un crédito'
        }
      >
        <CajaCuotasTable
          data={cuotas}
          loading={buscar.isPending}
          selectedKeys={selectedKeys}
          onSelectionChange={handleSelectionChange}
          mobileCards={isMobile}
          initialVista="cobrables"
          showLegend={!isMobile}
        />
      </CajaSection>

      {showPagoDock ? (
        <div
          className={[
            pagoDockClass,
            dockCollapsed ? 'is-collapsed' : '',
          ]
            .filter(Boolean)
            .join(' ')}
          role="region"
          aria-label="Registrar pago"
          style={
            isMobile && dockBox
              ? { left: dockBox.left, width: dockBox.width, right: 'auto' }
              : undefined
          }
        >
          <div className={`${pagoDockClass}__inner`}>
            {isMobile ? (
              <button
                type="button"
                className="caja-diario-pago-dock__handle"
                aria-expanded={pagoDockExpanded}
                aria-controls="caja-diario-pago-dock-body"
                onClick={() => setPagoDockExpanded((v) => !v)}
              >
                <span className="caja-diario-pago-dock__handle-grip" aria-hidden />
                <span className="caja-diario-pago-dock__handle-summary">{dockSummary}</span>
                <span className="caja-diario-pago-dock__handle-chevron" aria-hidden>
                  {pagoDockExpanded ? <DownOutlined /> : <UpOutlined />}
                </span>
                <span className="caja-diario-pago-dock__handle-hint">
                  {pagoDockExpanded ? 'Ocultar pago' : 'Mostrar pago'}
                </span>
              </button>
            ) : null}
            <div
              id="caja-diario-pago-dock-body"
              className={[
                'caja-diario-pago-dock__body',
                dockCollapsed ? 'is-collapsed' : '',
              ]
                .filter(Boolean)
                .join(' ')}
              hidden={dockCollapsed}
            >
            <div
              className="caja-diario-pago-modos"
              role="tablist"
              aria-label="Modo de cobro"
            >
              <button
                type="button"
                role="tab"
                aria-selected={modoPago === 'cuotas'}
                className={
                  modoPago === 'cuotas'
                    ? 'caja-diario-pago-modos__btn is-on'
                    : 'caja-diario-pago-modos__btn'
                }
                onClick={() => setModoPago('cuotas')}
              >
                <CreditCardOutlined /> Cuotas
              </button>
              <button
                type="button"
                role="tab"
                aria-selected={modoPago === 'libre'}
                className={
                  modoPago === 'libre'
                    ? 'caja-diario-pago-modos__btn is-on'
                    : 'caja-diario-pago-modos__btn'
                }
                onClick={() => setModoPago('libre')}
              >
                <DollarOutlined /> Libre
              </button>
            </div>

            {modoPago === 'cuotas' ? (
              <>
                <div className="caja-diario-pago-dock-row">
                  <div className="caja-diario-pago-totals">
                    <span>
                      Total{' '}
                      <strong>{formatMoney(selectedTotal)}</strong>
                      <small> · {selectedKeys.length} cuota(s)</small>
                    </span>
                    <span className="caja-diario-pago-recibido">
                      <span>Recibido</span>
                      <InputNumber
                        min={0}
                        step={0.01}
                        precision={2}
                        size={isMobile ? 'large' : 'middle'}
                        controls={false}
                        inputMode="decimal"
                        prefix="S/"
                        value={importeRecibido}
                        onChange={setImporteRecibido}
                      />
                    </span>
                    <span>
                      Vuelto{' '}
                      <strong
                        className={
                          vuelto != null && vuelto < 0 ? 'caja-vuelto-neg' : ''
                        }
                      >
                        {vuelto != null ? formatMoney(vuelto) : '—'}
                      </strong>
                    </span>
                  </div>
                  <div className="caja-diario-pago-chips-row">
                    <Button
                      size="small"
                      disabled={selectedTotal <= 0}
                      onClick={() => setImporteRecibido(selectedTotal)}
                    >
                      = Selección
                    </Button>
                    <Button
                      size="small"
                      disabled={totalCuotasCredito <= 0}
                      onClick={() => {
                        prepararCobroTodasCuotas()
                      }}
                    >
                      Todas
                    </Button>
                  </div>
                  <Button
                    type="primary"
                    size={isMobile ? 'large' : 'middle'}
                    block={isMobile}
                    className="caja-btn-pagar-cuota"
                    icon={<CreditCardOutlined />}
                    disabled={!creditoId || selectedKeys.length === 0}
                    loading={pagar.isPending}
                    onClick={() => void solicitarPagarCuota()}
                  >
                    {cuotaMixto ? 'Cobro mixto' : 'Pagar cuota'}
                  </Button>
                </div>
                <Checkbox
                  checked={cuotaMixto}
                  disabled={!creditoId || selectedKeys.length === 0}
                  onChange={(e) => {
                    const on = e.target.checked
                    setCuotaMixto(on)
                    if (on) {
                      // Mixto de cuota: suma de medios = total de cuotas seleccionadas (al céntimo).
                      const hint = roundMoney(selectedTotal)
                      setImporteRecibido(hint)
                      setMediosCuota(
                        defaultPagoMediosSplit(
                          tipoPagoOptions,
                          hint > 0 ? hint : undefined,
                        ),
                      )
                    } else {
                      setMediosCuota([])
                    }
                  }}
                  style={{ marginBottom: 8 }}
                >
                  Dividir en varios medios (Yape Gerente/René, Plin, bancos + efectivo)
                </Checkbox>
                {cuotaMixto ? (
                  <PagoMediosSplit
                    lineas={mediosCuota}
                    options={tipoPagoOptions}
                    onChange={setMediosCuota}
                    disabled={pagar.isPending}
                  />
                ) : (
                  <>
                    <TipoPagoChips
                      value={tipoPagoId}
                      options={tipoPagoOptions}
                      onChange={setTipoPagoId}
                      wrap={!isMobile}
                    />
                    {esTipoPagoDigital(tipoPagoId) ? (
                      <FechaHoraDigitalField
                        value={fechaTransferencia}
                        status={!fechaTransferencia ? 'error' : undefined}
                        onChange={setFechaTransferencia}
                        compact={!isMobile}
                      />
                    ) : null}
                  </>
                )}
              </>
            ) : (
              <>
                <Checkbox
                  checked={pagoLibreMixto}
                  disabled={!creditoId}
                  onChange={(e) => {
                    const on = e.target.checked
                    setPagoLibreMixto(on)
                    if (on) {
                      const hint = pagoLibre ?? selectedTotal ?? totalCuotasCredito
                      setMediosLibre(
                        defaultPagoMediosSplit(
                          tipoPagoOptions,
                          hint > 0 ? hint : undefined,
                        ),
                      )
                    } else {
                      setMediosLibre([])
                    }
                  }}
                  style={{ marginBottom: 8 }}
                >
                  Dividir en varios medios (Yape Gerente/René, Plin, bancos + efectivo)
                </Checkbox>
                {pagoLibreMixto ? (
                  <>
                    <PagoMediosSplit
                      lineas={mediosLibre}
                      options={tipoPagoOptions}
                      onChange={setMediosLibre}
                      disabled={ejecutarPagoLibre.isPending}
                    />
                    <Button
                      type="primary"
                      size={isMobile ? 'large' : 'middle'}
                      block={isMobile}
                      className="caja-btn-pago-libre"
                      icon={<DollarOutlined />}
                      disabled={!creditoId || sumPagoMedios(mediosLibre) <= 0}
                      loading={ejecutarPagoLibre.isPending}
                      onClick={() => void solicitarPagoLibre()}
                      style={{ marginTop: 12 }}
                    >
                      Cobro mixto
                    </Button>
                  </>
                ) : (
                  <>
                    <div className="caja-diario-pago-dock-row">
                      <div className="caja-diario-pago-libre-dock">
                        <label className="caja-diario-pago-recibido">
                          <span>Monto libre</span>
                          <InputNumber
                            min={0}
                            step={0.01}
                            precision={2}
                            size={isMobile ? 'large' : 'middle'}
                            controls={false}
                            inputMode="decimal"
                            prefix="S/"
                            value={pagoLibre}
                            onChange={setPagoLibre}
                          />
                        </label>
                        <div className="caja-diario-pago-chips-row">
                          <Button
                            size="small"
                            disabled={selectedTotal <= 0}
                            onClick={() => setPagoLibre(selectedTotal)}
                          >
                            = Selección
                          </Button>
                          <Button
                            size="small"
                            disabled={totalCuotasCredito <= 0}
                            onClick={() => setPagoLibre(totalCuotasCredito)}
                          >
                            Deuda cuotas
                          </Button>
                          <Button
                            size="small"
                            disabled={!pagoLibre}
                            onClick={() => setPagoLibre(null)}
                          >
                            0
                          </Button>
                        </div>
                      </div>
                      <Button
                        type="primary"
                        size={isMobile ? 'large' : 'middle'}
                        block={isMobile}
                        className="caja-btn-pago-libre"
                        icon={<DollarOutlined />}
                        disabled={!creditoId || !pagoLibre}
                        loading={ejecutarPagoLibre.isPending}
                        onClick={() => void solicitarPagoLibre()}
                      >
                        Pago libre
                      </Button>
                    </div>
                    <TipoPagoChips
                      value={tipoPagoLibreId}
                      options={tipoPagoOptions}
                      onChange={setTipoPagoLibreId}
                      wrap={!isMobile}
                    />
                    {esTipoPagoDigital(tipoPagoLibreId) ? (
                      <FechaHoraDigitalField
                        value={fechaPagoLibre}
                        status={!fechaPagoLibre ? 'error' : undefined}
                        onChange={setFechaPagoLibre}
                        compact={!isMobile}
                      />
                    ) : null}
                  </>
                )}
              </>
            )}
            </div>
          </div>
        </div>
      ) : null}

      <details className="caja-diario-excepciones">
        <summary>Cierre y excepciones</summary>
        <div className="caja-diario-excepciones__body">
          <Button
            icon={<CheckCircleOutlined />}
            loading={consultandoImpagos || ejecutarCompletarImpagos.isPending}
            onClick={() => void solicitarCompletarImpagos()}
          >
            Completar impagos
          </Button>
          {creditoId && isMobile ? (
            <Button
              onClick={() => {
                const mora =
                  condonacionPendienteQuery.data?.tienePendiente
                    ? condonacionPendienteQuery.data.moraCondonacion
                    : sumarMoraVigente(cuotas)
                setMoraSolicitar(roundMoney(mora))
                setSolicitarOpen(true)
              }}
            >
              Solicitar condonación
            </Button>
          ) : null}
          <CancelacionCreditoPanel
            ctx={ctx}
            creditoId={creditoId}
            clienteLabel={clienteLabel}
            onChanged={onChanged}
            tipoPagoId={tipoPagoId}
            fechaPagoTransferencia={fechaTransferencia}
          />
        </div>
      </details>

      <CreditosPendientesModal
        open={cuotasModalOpen}
        onClose={() => setCuotasModalOpen(false)}
        onSelectCredito={(id, pid, label) => {
          setCuotasModalOpen(false)
          void (async () => {
            await cargarCreditos(pid, label)
            cargarCuotas(id)
          })()
        }}
      />

      <CajaModal
        title="Solicitar condonación de mora"
        open={solicitarOpen}
        onCancel={() => setSolicitarOpen(false)}
        onOk={() => solicitar.mutateAsync()}
        confirmLoading={solicitar.isPending}
        okText="Registrar solicitud"
      >
        {condonacionPendienteQuery.data?.tienePendiente ? (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message="Ya hay una solicitud pendiente para este crédito."
          />
        ) : null}
        <p>Monto de mora a solicitar.</p>
        <InputNumber
          min={0}
          step={0.01}
          precision={2}
          style={{ width: '100%' }}
          value={moraSolicitar}
          onChange={(v) => setMoraSolicitar(v)}
          prefix="S/"
        />
      </CajaModal>

      <CreditoMoraModal
        open={moraModalOpen}
        creditoId={creditoId}
        cuotas={cuotas}
        onClose={() => setMoraModalOpen(false)}
        onPrepararCobroTodasCuotas={prepararCobroTodasCuotas}
      />
    </div>
  )
}
