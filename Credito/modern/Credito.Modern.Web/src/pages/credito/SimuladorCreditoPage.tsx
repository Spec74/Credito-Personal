import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Descriptions,
  Form,
  Input,
  InputNumber,
  Radio,
  Select,
  Space,
  Steps,
  Tag,
  Typography,
  message,
} from 'antd'
import {
  CalculatorOutlined,
  FileAddOutlined,
  SearchOutlined,
  UserAddOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import { buscarClientes, obtenerCliente } from '../../api/clientes'
import {
  calcularTem,
  crearCredito,
  crearSolicitudCredito,
  downloadRptSimuladorPlanPagosCsv,
  openRptSimuladorPlanPagosPdfInTab,
  simularCredito,
  type RptSimuladorPlanPagosParams,
} from '../../api/creditoPlanes'
import { fetchCreditoContexto, fetchPrendas, fetchSolicitudCredito } from '../../api/creditoGestion'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import {
  CredixDataTable,
  CredixPage,
  CredixPanel,
  type CredixStatItem,
} from '../../components/credix'
import { fetchProductos } from '../../api/productos'
import { consultarDniApiPeru, consultarRucApiPeru } from '../../api/apiperu'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import type { ClienteBuscarItem, SimuladorCreditoCuota } from '../../types/api'
import { formatMoney } from '../../utils/formatMoney'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import {
  esCreditoAdministrador,
  esCreditoAprobador1,
} from '../../utils/creditoOperacionPermisos'
import { prendaAItem, prendaSimuladorDesdeBienes } from '../../utils/prendas'

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

const { Paragraph, Text } = Typography

const FORMAS_PAGO = [
  { value: 'M', label: 'Mensual (M)' },
  { value: 'Q', label: 'Quincenal (Q)' },
  { value: 'S', label: 'Semanal (S)' },
  { value: 'D', label: 'Diario (D)' },
]

/** Paridad legacy `Creditos.cshtml` / `cboGA`: solo ADE (CUO comentado en MVC). */
const IND_GASTOS_ADM = 'ADE' as const

const IND_GASTOS_ADM_OPTIONS = [
  { value: IND_GASTOS_ADM, label: 'Trámite adm. pago adelantado (ADE)' },
]

interface SimForm {
  tipoPersona: 'N' | 'J'
  numeroDocumento?: string
  nombre?: string
  apePaterno?: string
  apeMaterno?: string
  telefono?: string
  direccionCliente?: string
  direccionNegocio?: string
  prendaDescripcion?: string
  monto: number
  formaPago: string
  nroCuotas: number
  interesMensual: number
  fechaPrimerPago: string
  gastosAdm?: number
}

type PrendaPreCarga = {
  descripcion: string
  montoTasacion: number
  fechaRemate: string
  observacion: string | null
}

function defaultFecha(): string {
  const d = new Date()
  d.setMonth(d.getMonth() + 1)
  return d.toISOString().slice(0, 10)
}

function addDaysIso(days: number): string {
  const d = new Date()
  d.setHours(0, 0, 0, 0)
  d.setDate(d.getDate() + days)
  return d.toISOString().slice(0, 10)
}

function addMonthsIso(months: number): string {
  const d = new Date()
  d.setHours(0, 0, 0, 0)
  d.setMonth(d.getMonth() + months)
  return d.toISOString().slice(0, 10)
}

function fechaPorModalidad(formaPago: string, prendario: boolean): string {
  if (prendario) return addDaysIso(30)
  switch (formaPago) {
    case 'D':
      return addDaysIso(1)
    case 'S':
      return addDaysIso(7)
    case 'Q':
      return addDaysIso(15)
    case 'M':
    default:
      return addMonthsIso(1)
  }
}

function clienteProspectoLabel(v: Partial<SimForm>): string {
  if (v.tipoPersona === 'J') return v.nombre?.trim() || 'CLIENTE PROSPECTO'
  return [v.nombre, v.apePaterno, v.apeMaterno].filter(Boolean).join(' ').trim() || 'CLIENTE PROSPECTO'
}

export function SimuladorCreditoPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const roles = useMemo(() => session?.roles ?? [], [session?.roles])
  const puedeIrAAprobar = esCreditoAprobador1(roles) || esCreditoAdministrador(roles)
  const [cuotas, setCuotas] = useState<SimuladorCreditoCuota[]>([])
  const [tem, setTem] = useState<number | null>(null)
  const [personaId, setPersonaId] = useState<number | null>(null)
  const [clienteLabel, setClienteLabel] = useState('')
  const [terminoCliente, setTerminoCliente] = useState('')
  const [solicitudCreditoId, setSolicitudCreditoId] = useState<number | null>(null)
  const [productoId, setProductoId] = useState<number | null>(null)
  const [observacion, setObservacion] = useState('')
  const [indCentralRiesgo, setIndCentralRiesgo] = useState(true)
  const [form] = Form.useForm<SimForm>()
  const tipoPersona = Form.useWatch('tipoPersona', form) ?? 'N'
  const montoActual = Form.useWatch('monto', form)
  const formaPagoActual = Form.useWatch('formaPago', form) ?? 'M'
  const prospectoValues = Form.useWatch([], form) as Partial<SimForm> | undefined

  const personaIdFromUrl = useMemo(() => {
    const q = searchParams.get('personaId')
    if (!q) return null
    const id = Number(q)
    return Number.isNaN(id) || id < 1 ? null : id
  }, [searchParams])

  const solicitudFromUrl = useMemo(() => {
    const q = searchParams.get('solicitudCreditoId')
    if (!q) return null
    const id = Number(q)
    return Number.isNaN(id) || id < 1 ? null : id
  }, [searchParams])

  const productoFromUrl = useMemo(() => {
    const q = searchParams.get('productoId')
    if (!q) return null
    const id = Number(q)
    return Number.isNaN(id) || id < 1 ? null : id
  }, [searchParams])

  const observacionFromUrl = useMemo(() => searchParams.get('observacion'), [searchParams])

  const prendaFromUrl = useMemo<PrendaPreCarga | null>(() => {
    const descripcion = searchParams.get('prendaDescripcion')?.trim()
    const montoRaw = searchParams.get('prendaMontoTasacion')
    const fechaRemate = searchParams.get('prendaFechaRemate')?.trim()
    if (!descripcion || !montoRaw || !fechaRemate) return null
    const montoTasacion = Number(montoRaw)
    if (!Number.isFinite(montoTasacion) || montoTasacion <= 0) return null
    return {
      descripcion,
      montoTasacion,
      fechaRemate,
      observacion: searchParams.get('prendaObservacion')?.trim() || null,
    }
  }, [searchParams])

  const esConsultaPrendario = productoFromUrl === 2

  const prendasGuardadasQuery = useQuery({
    queryKey: ['prendas', oficinaId, solicitudFromUrl],
    queryFn: () => fetchPrendas(oficinaId, solicitudFromUrl!),
    enabled: oficinaId > 0 && solicitudFromUrl != null && esConsultaPrendario,
    staleTime: creditoStaleTime.operacion,
  })

  const contextoPrendarioQuery = useQuery({
    queryKey: ['credito-contexto', solicitudFromUrl],
    queryFn: () => fetchCreditoContexto(solicitudFromUrl!),
    enabled: oficinaId > 0 && solicitudFromUrl != null && esConsultaPrendario,
    staleTime: creditoStaleTime.operacion,
  })

  const prendaPrecarga = useMemo<PrendaPreCarga | null>(() => {
    if (prendaFromUrl) return prendaFromUrl
    const items = prendasGuardadasQuery.data ?? []
    if (items.length === 0) return null
    return prendaSimuladorDesdeBienes(
      items.map(prendaAItem),
      contextoPrendarioQuery.data?.fechaRemate,
    )
  }, [prendaFromUrl, prendasGuardadasQuery.data, contextoPrendarioQuery.data?.fechaRemate])

  const bienesYaGuardados = (prendasGuardadasQuery.data?.length ?? 0) > 0

  useEffect(() => {
    if (observacionFromUrl && !observacion.trim()) {
      setObservacion(observacionFromUrl)
    }
  }, [observacion, observacionFromUrl])

  useEffect(() => {
    if (productoFromUrl && productoFromUrl !== productoId) {
      setProductoId(productoFromUrl)
    }
  }, [productoFromUrl, productoId])

  useEffect(() => {
    if (personaIdFromUrl !== null && personaIdFromUrl !== personaId) {
      setPersonaId(personaIdFromUrl)
      setClienteLabel(`Persona #${personaIdFromUrl}`)
    }
  }, [personaId, personaIdFromUrl])

  useEffect(() => {
    if (solicitudFromUrl !== null && solicitudFromUrl !== solicitudCreditoId) {
      setSolicitudCreditoId(solicitudFromUrl)
    }
  }, [solicitudCreditoId, solicitudFromUrl])

  const productosQuery = useQuery({
    queryKey: ['productos'],
    queryFn: fetchProductos,
    staleTime: creditoStaleTime.master,
  })

  const clienteDetalleQuery = useQuery({
    queryKey: ['cliente-detalle', personaId],
    queryFn: () => obtenerCliente(personaId!),
    enabled: personaId != null && personaId > 0 && clienteLabel.startsWith('Persona #'),
    staleTime: creditoStaleTime.ficha,
  })

  useEffect(() => {
    const c = clienteDetalleQuery.data
    if (!c) return

    const nombreCompleto =
      c.tipoPersona === 'J'
        ? c.nombre
        : [c.nombre, c.apePaterno, c.apeMaterno].filter(Boolean).join(' ')
    setClienteLabel(`${c.numeroDocumento} ${nombreCompleto}`.trim())
  }, [clienteDetalleQuery.data])

  const productoSeleccionado = useMemo(
    () => productosQuery.data?.find((p) => p.productoId === productoId) ?? null,
    [productoId, productosQuery.data],
  )
  const esPrendario = Boolean(
    prendaPrecarga ||
      productoSeleccionado?.denominacion?.toLowerCase().includes('prendario') ||
      esConsultaPrendario,
  )
  const clienteParaReporte =
    personaId != null
      ? clienteLabel
      : clienteProspectoLabel({ tipoPersona, ...(prospectoValues ?? {}) })
  const asesorNombre = session ? `Usuario ${session.usuarioId}` : ''
  const documentoValido =
    tipoPersona === 'J'
      ? (prospectoValues?.numeroDocumento?.trim().length ?? 0) === 11
      : (prospectoValues?.numeroDocumento?.trim().length ?? 0) === 8
  const prospectoListo =
    personaId != null ||
    (Boolean(prospectoValues?.nombre?.trim()) &&
      documentoValido &&
      (prospectoValues?.telefono?.trim().length ?? 0) >= 9)

  const solicitudQuery = useQuery({
    queryKey: ['solicitud-credito', oficinaId, solicitudFromUrl],
    queryFn: () => fetchSolicitudCredito(oficinaId, solicitudFromUrl!),
    enabled: oficinaId > 0 && solicitudFromUrl != null,
    staleTime: creditoStaleTime.operacion,
  })

  useEffect(() => {
    const solicitud = solicitudQuery.data
    if (!solicitud) return

    setPersonaId(solicitud.personaId)
    setClienteLabel(solicitud.cliente)
    setSolicitudCreditoId(solicitud.solicitudCreditoId)

    if (!prendaPrecarga) {
      if (solicitud.productoId && solicitud.productoId > 0) {
        setProductoId(solicitud.productoId)
      }
      if (solicitud.observacion?.trim()) {
        setObservacion(solicitud.observacion)
      }
    }

    setIndCentralRiesgo((solicitud.centralRiesgo ?? 0) > 0)
    form.setFieldsValue({
      monto: solicitud.montoCredito > 0 ? solicitud.montoCredito : undefined,
      formaPago: solicitud.formaPago || 'M',
      nroCuotas: solicitud.numeroCuotas > 0 ? solicitud.numeroCuotas : undefined,
      interesMensual: solicitud.interes >= 0 ? solicitud.interes : undefined,
      fechaPrimerPago: solicitud.fechaPrimerPago?.slice(0, 10) || defaultFecha(),
      gastosAdm: solicitud.montoGastosAdm ?? 0,
    })
  }, [form, prendaPrecarga, solicitudQuery.data])

  useEffect(() => {
    if (!prendaPrecarga) return
    form.setFieldValue('prendaDescripcion', prendaPrecarga.descripcion)
  }, [form, prendaPrecarga])

  const busquedaCliente = useMutation({
    mutationFn: (t: string) => buscarClientes(t),
  })

  const validarDocumento = useMutation({
    mutationFn: async () => {
      const { numeroDocumento, tipoPersona: tipo } = form.getFieldsValue()
      const documento = (numeroDocumento ?? '').replace(/\D/g, '')
      form.setFieldValue('numeroDocumento', documento)
      if (tipo === 'J') {
        if (documento.length !== 11) throw new Error('Ingrese un RUC de 11 dígitos')
        return { tipo, data: await consultarRucApiPeru(documento) }
      }
      if (documento.length !== 8) throw new Error('Ingrese un DNI de 8 dígitos')
      return { tipo: 'N' as const, data: await consultarDniApiPeru(documento) }
    },
    onSuccess: (ret) => {
      if (ret.tipo === 'J') {
        const data = ret.data
        if (!data.success || !data.razonSocial?.trim()) {
          message.info(data.mensaje?.trim() || 'Documento no encontrado. Complete los datos manualmente.')
          return
        }
        form.setFieldsValue({
          nombre: data.razonSocial ?? '',
          apePaterno: '',
          apeMaterno: '',
          direccionNegocio: data.direccion ?? undefined,
        })
      } else {
        const data = ret.data
        if (
          !data.success ||
          !(data.nombres?.trim() || data.apellidoPaterno?.trim() || data.apellidoMaterno?.trim())
        ) {
          message.info(data.mensaje?.trim() || 'Documento no encontrado. Complete los datos manualmente.')
          return
        }
        form.setFieldsValue({
          nombre: data.nombres ?? '',
          apePaterno: data.apellidoPaterno ?? '',
          apeMaterno: data.apellidoMaterno ?? '',
        })
      }
      message.success('Documento validado correctamente')
    },
    onError: (e) => message.info(e instanceof Error ? e.message : errMsg(e)),
  })

  useEffect(() => {
    if (typeof montoActual === 'number' && Number.isFinite(montoActual)) {
      form.setFieldValue('gastosAdm', Number((montoActual * 0.01).toFixed(2)))
    }
  }, [form, montoActual])

  useEffect(() => {
    if (!productoSeleccionado) return
    form.setFieldValue('interesMensual', productoSeleccionado.interesMaxima)
    if (esPrendario) {
      form.setFieldValue('formaPago', 'M')
    }
    form.setFieldValue('fechaPrimerPago', fechaPorModalidad(formaPagoActual, esPrendario))
  }, [esPrendario, form, formaPagoActual, productoSeleccionado])

  const crearSolicitud = useMutation({
    mutationFn: () =>
      crearSolicitudCredito({
        oficinaId,
        personaId: personaId!,
      }),
    onSuccess: (r) => {
      setSolicitudCreditoId(r.solicitudCreditoId)
      message.success(`Solicitud #${r.solicitudCreditoId} en estado CRE`)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const generarCredito = useMutation({
    mutationFn: async () => {
      const v = form.getFieldsValue()
      if (!solicitudCreditoId || !productoId) {
        throw new Error('Falta solicitud o producto')
      }
      if (cuotas.length < 1) {
        throw new Error('Simule el plan antes de generar el crédito')
      }
      return crearCredito({
        oficinaId,
        solicitudCreditoId,
        productoId,
        tipoCuota: 'F',
        montoInicial: 0,
        montoGastosAdm: v.gastosAdm ?? 0,
        indGastosAdm: IND_GASTOS_ADM,
        montoCredito: v.monto,
        modalidad: v.formaPago,
        numeroCuotas: v.nroCuotas,
        interesMensual: v.interesMensual,
        fechaPrimerPago: `${v.fechaPrimerPago}T00:00:00`,
        observacion: observacion.trim() || null,
        indCentralRiesgo,
        prenda:
          bienesYaGuardados || !prendaPrecarga
            ? null
            : {
                descripcion: prendaPrecarga.descripcion,
                montoTasacion: prendaPrecarga.montoTasacion,
                fechaRemate: `${prendaPrecarga.fechaRemate}T00:00:00`,
                observacion: prendaPrecarga.observacion,
              },
      })
    },
    onSuccess: (r) => {
      if (r.mensaje?.trim()) {
        message.info(r.mensaje)
      } else {
        message.success('Crédito generado para aprobación')
      }
      if (puedeIrAAprobar) {
        navigate('/credito/aprobar')
      } else if (personaId) {
        message.info('La solicitud quedó pendiente para que la apruebe un aprobador autorizado.')
        navigate(`/credito/consulta?personaId=${personaId}`)
      } else {
        navigate('/credito/consulta')
      }
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const exportCsv = useMutation({
    mutationFn: (p: RptSimuladorPlanPagosParams) => downloadRptSimuladorPlanPagosCsv(p),
  })

  const exportPdf = useMutation({
    mutationFn: (p: RptSimuladorPlanPagosParams) => openRptSimuladorPlanPagosPdfInTab(p),
  })



  const simular = useMutation({
    mutationFn: async (values: SimForm) => {
      if (!productoId) {
        throw new Error('Seleccione un producto de crédito')
      }
      if (!prospectoListo) {
        throw new Error('Seleccione un cliente o complete los datos del prospecto')
      }
      if (productoSeleccionado) {
        const min = productoSeleccionado.interesMinima
        const max = productoSeleccionado.interesMaxima
        if (values.interesMensual < min || values.interesMensual > max) {
          throw new Error(`El interés debe estar entre ${min.toFixed(2)}% y ${max.toFixed(2)}%`)
        }
      }
      if (esPrendario && !values.prendaDescripcion?.trim() && !prendaPrecarga) {
        throw new Error('Ingrese la descripción de la prenda')
      }
      // Paridad CreditoController.Simulador con cboGA=ADE: gastos no van al SP (solo en cabecera informe).
      const gastosSp = 0
      const rows = await simularCredito({
        monto: values.monto,
        formaPago: values.formaPago,
        nroCuotas: values.nroCuotas,
        interesMensual: values.interesMensual,
        fechaPrimerPago: `${values.fechaPrimerPago}T00:00:00`,
        gastosAdm: gastosSp,
      })
      try {
        const temRes = await calcularTem(values.interesMensual, values.formaPago)
        setTem(temRes.tem)
      } catch {
        setTem(null)
      }
      return rows
    },
    onSuccess: (data) => {
      setCuotas(data)
      message.success(`${data.length} cuota(s) simuladas`)
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'Error al simular'),
  })

  const pasoActual =
    solicitudCreditoId != null && cuotas.length > 0
      ? 3
      : cuotas.length > 0
        ? 2
        : prospectoListo
          ? 1
          : 0

  const columns: ColumnsType<SimuladorCreditoCuota> = [
    { title: 'Nro', dataIndex: 'numero', width: 60 },
    {
      title: 'Capital',
      dataIndex: 'capital',
      width: 110,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Fecha pago',
      dataIndex: 'fechaPago',
      width: 110,
      render: (v: string | null) => v?.slice(0, 10) ?? '—',
    },
    {
      title: 'Amort.',
      dataIndex: 'amortizacion',
      width: 110,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Interés',
      dataIndex: 'interes',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'G.A.',
      dataIndex: 'gastosAdm',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cuota',
      dataIndex: 'cuota',
      width: 110,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo',
      dataIndex: 'saldo',
      width: 110,
      align: 'right',
      render: (v: number | null) => formatMoney(v ?? 0),
    },
  ]

  const totalCuota = cuotas.reduce((s, c) => s + (c.cuota ?? 0), 0)
  const totalInteres = cuotas.reduce((s, c) => s + (c.interes ?? 0), 0)
  const montoSim = montoActual as number | undefined
  const nroCuotasSim = Form.useWatch('nroCuotas', form) as number | undefined
  const gastosAdmSim = Form.useWatch('gastosAdm', form) as number | undefined

  const simStats: CredixStatItem[] = useMemo(() => {
    const items: CredixStatItem[] = []
    if (personaId != null) {
      items.push({
        value: clienteLabel || `Persona #${personaId}`,
        label: 'Cliente',
      })
    } else {
      items.push({
        value: clienteParaReporte,
        label: 'Prospecto',
      })
    }
    if (solicitudCreditoId != null) {
      items.push({ value: solicitudCreditoId, label: 'Solicitud' })
    }
    if (prendaPrecarga) {
      items.push({ value: formatMoney(prendaPrecarga.montoTasacion), label: 'Tasación prenda' })
    }
    if (cuotas.length > 0) {
      items.push(
        { value: cuotas.length, label: 'Cuotas simuladas' },
        { value: formatMoney(totalInteres), label: 'Intereses' },
        { value: formatMoney(totalCuota), label: 'Total a devolver', tone: 'green' },
      )
      if (tem != null) {
        items.push({ value: `${tem.toFixed(2)}%`, label: 'TEM' })
      }
      if (montoSim != null) {
        items.push({ value: formatMoney(montoSim), label: 'Monto crédito' })
      }
      if (gastosAdmSim != null) {
        items.push({ value: formatMoney(gastosAdmSim), label: 'Gastos adm.' })
      }
    } else if (montoSim != null && nroCuotasSim != null) {
      items.push(
        { value: formatMoney(montoSim), label: 'Monto' },
        { value: nroCuotasSim, label: 'Cuotas' },
      )
    }
    return items
  }, [
    personaId,
    clienteLabel,
    clienteParaReporte,
    solicitudCreditoId,
    prendaPrecarga,
    cuotas.length,
    totalInteres,
    totalCuota,
    tem,
    montoSim,
    nroCuotasSim,
    gastosAdmSim,
  ])

  const productoOpts =
    productosQuery.data?.map((p) => ({
      value: p.productoId,
      label: p.denominacion,
    })) ?? []

  const buildReporteParams = (): RptSimuladorPlanPagosParams | null => {
    if (!productoId) return null
    const v = form.getFieldsValue()
    return {
      productoId,
      monto: v.monto,
      nroCuotas: v.nroCuotas,
      interesMensual: v.interesMensual,
      fechaPrimerPago: v.fechaPrimerPago,
      formaPago: v.formaPago,
      gastosAdm: v.gastosAdm ?? 0,
      ga: IND_GASTOS_ADM,
      cliente: clienteParaReporte,
      tipoDocumento: v.tipoPersona ?? tipoPersona,
      nroDocumento: v.numeroDocumento,
      direccionCliente: v.direccionCliente,
      direccionNegocio: v.direccionNegocio,
      prendaDescripcion: prendaPrecarga?.descripcion ?? v.prendaDescripcion,
      asesor: asesorNombre,
      telefonoCliente: v.telefono,
    }
  }

  return (
    <CredixPage
      title="Simulador y originación de crédito"
      subtitle="Cliente, plan de pagos, solicitud y alta para aprobación."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Simulador' },
      ]}
      stats={simStats}
    >
      <Steps
        size="small"
        current={pasoActual}
        style={{ marginBottom: 24, maxWidth: 720 }}
        items={[
          { title: 'Cliente' },
          { title: 'Simular' },
          { title: 'Solicitud' },
          { title: 'Generar crédito' },
        ]}
      />

      {prendaPrecarga ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="Crédito prendario"
          description={`Prenda: ${prendaPrecarga.descripcion.toUpperCase()} | Tasación: ${formatMoney(prendaPrecarga.montoTasacion)} | Fecha remate: ${prendaPrecarga.fechaRemate}`}
        />
      ) : null}

      <CredixPanel title="1. Cliente o prospecto">
        <Space direction="vertical" style={{ width: '100%' }} size="middle">
          <Card size="small" title="Cliente existente">
            <Space wrap style={{ marginBottom: 12 }}>
              <Input
                placeholder="Buscar cliente (DNI, nombre, código o celular)"
                prefix={<SearchOutlined />}
                value={terminoCliente}
                onChange={(e) => setTerminoCliente(e.target.value)}
                onPressEnter={() => {
                  if (terminoCliente.trim().length >= 2) {
                    busquedaCliente.mutate(terminoCliente.trim())
                  }
                }}
                style={{ width: '100%', maxWidth: 420, minWidth: 220 }}
              />
              <Button
                onClick={() => {
                  if (terminoCliente.trim().length < 2) {
                    message.warning('Escriba al menos 2 caracteres')
                    return
                  }
                  busquedaCliente.mutate(terminoCliente.trim())
                }}
                loading={busquedaCliente.isPending}
              >
                Buscar
              </Button>
              {personaId ? (
                <Button
                  onClick={() => {
                    setPersonaId(null)
                    setClienteLabel('')
                    setSolicitudCreditoId(null)
                  }}
                >
                  Usar prospecto
                </Button>
              ) : null}
            </Space>
            {personaId ? (
              <Alert
                type="success"
                showIcon
                message={`Cliente seleccionado: ${clienteLabel} (persona #${personaId})`}
                description="Para clientes existentes no se consulta API Perú; se usan los datos registrados en la base."
              />
            ) : (
              <CredixDataTable<ClienteBuscarItem>
                size="small"
                rowKey="personaId"
                loading={busquedaCliente.isPending}
                dataSource={busquedaCliente.data ?? []}
                pagination={false}
                locale={{ emptyText: 'Busque un cliente o complete el prospecto abajo' }}
                columns={[
                  { title: 'Cliente', dataIndex: 'label', ellipsis: true },
                  {
                    title: '',
                    width: 90,
                    render: (_, row) => (
                      <Button
                        size="small"
                        type="link"
                        onClick={() => {
                          setPersonaId(row.personaId)
                          setClienteLabel(row.label)
                          setSolicitudCreditoId(null)
                          busquedaCliente.reset()
                        }}
                      >
                        Elegir
                      </Button>
                    ),
                  },
                ]}
              />
            )}
          </Card>

          {!personaId ? (
            <Card size="small" title="Prospecto para simulación rápida">
              <Form form={form} layout="vertical">
                <div className="simulador-prospecto-grid">
                  <Form.Item name="tipoPersona" label="Tipo persona">
                    <Radio.Group
                      options={[
                        { value: 'N', label: 'Natural' },
                        { value: 'J', label: 'Jurídica' },
                      ]}
                      onChange={() => {
                        form.setFieldsValue({
                          numeroDocumento: '',
                          nombre: '',
                          apePaterno: '',
                          apeMaterno: '',
                          direccionNegocio: '',
                        })
                      }}
                    />
                  </Form.Item>
                  <Form.Item
                    name="numeroDocumento"
                    label={tipoPersona === 'J' ? 'RUC' : 'DNI'}
                    rules={[
                      {
                        len: tipoPersona === 'J' ? 11 : 8,
                        message: tipoPersona === 'J' ? 'RUC de 11 dígitos' : 'DNI de 8 dígitos',
                      },
                    ]}
                  >
                    <Input
                      maxLength={tipoPersona === 'J' ? 11 : 8}
                      inputMode="numeric"
                      pattern="[0-9]*"
                      placeholder={tipoPersona === 'J' ? '11 dígitos' : '8 dígitos'}
                      onChange={(e) =>
                        form.setFieldValue('numeroDocumento', e.target.value.replace(/\D/g, ''))
                      }
                      onPressEnter={() => validarDocumento.mutate()}
                    />
                  </Form.Item>
                  <Form.Item label="Validar">
                    <Button
                      icon={<SearchOutlined />}
                      loading={validarDocumento.isPending}
                      onClick={() => validarDocumento.mutate()}
                    >
                      API Perú
                    </Button>
                  </Form.Item>
                  <Form.Item
                    name="telefono"
                    label="Teléfono"
                    rules={[{ pattern: /^9\d{8}$/, message: 'Celular peruano de 9 dígitos' }]}
                  >
                    <Input
                      maxLength={9}
                      inputMode="numeric"
                      pattern="[0-9]*"
                      placeholder="9 dígitos"
                      onChange={(e) =>
                        form.setFieldValue('telefono', e.target.value.replace(/\D/g, ''))
                      }
                    />
                  </Form.Item>
                  <Form.Item
                    name="nombre"
                    label={tipoPersona === 'J' ? 'Razón social' : 'Nombres'}
                    rules={[{ required: !personaId, message: 'Dato requerido' }]}
                  >
                    <Input />
                  </Form.Item>
                  {tipoPersona === 'N' ? (
                    <>
                      <Form.Item name="apePaterno" label="Apellido paterno">
                        <Input />
                      </Form.Item>
                      <Form.Item name="apeMaterno" label="Apellido materno">
                        <Input />
                      </Form.Item>
                    </>
                  ) : null}
                  <Form.Item name="direccionCliente" label="Dirección domicilio">
                    <Input />
                  </Form.Item>
                  <Form.Item name="direccionNegocio" label="Dirección negocio / empresa">
                    <Input />
                  </Form.Item>
                </div>
              </Form>
            </Card>
          ) : null}
        </Space>
      </CredixPanel>

      <CredixPanel title="2. Simular plan">
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            tipoPersona: 'N',
            monto: 1000,
            formaPago: 'M',
            nroCuotas: 26,
            interesMensual: 8,
            fechaPrimerPago: defaultFecha(),
            gastosAdm: 10,
          }}
          onFinish={(v) => simular.mutate(v)}
        >
          <div className="simulador-parametros-grid">
            <Form.Item label="Producto" required>
              <Select
                placeholder="Producto de crédito"
                className="simulador-field-fluid"
                loading={productosQuery.isLoading}
                options={productoOpts}
                value={productoId ?? undefined}
                onChange={(v) => setProductoId(v)}
              />
              {productoSeleccionado ? (
                <Text type="secondary" style={{ display: 'block', marginTop: 4, fontSize: 12 }}>
                  Interés min: {productoSeleccionado.interesMinima.toFixed(2)}% · max:{' '}
                  {productoSeleccionado.interesMaxima.toFixed(2)}%
                </Text>
              ) : null}
            </Form.Item>
            <Form.Item
              name="monto"
              label="Monto crédito"
              rules={[{ required: true, type: 'number', min: 0.01 }]}
            >
              <InputNumber min={0.01} step={100} className="simulador-field-fluid" />
            </Form.Item>
            <Form.Item name="formaPago" label="Modalidad" rules={[{ required: true }]}>
              <Select options={FORMAS_PAGO} className="simulador-field-fluid" />
            </Form.Item>
            <Form.Item
              name="nroCuotas"
              label="Cuotas"
              rules={[{ required: true, type: 'number', min: 1 }]}
            >
              <InputNumber min={1} className="simulador-field-fluid" />
            </Form.Item>
            <Form.Item
              name="interesMensual"
              label="Interés mensual (%)"
              rules={[{ required: true, type: 'number', min: 0 }]}
            >
              <InputNumber min={0} step={0.1} className="simulador-field-fluid" />
            </Form.Item>
            {esPrendario ? (
              <Form.Item
                name="prendaDescripcion"
                label="Descripción de prenda"
                rules={[{ required: !prendaPrecarga, message: 'Prenda obligatoria' }]}
              >
                <Input
                  placeholder="Ej. laptop, joya, artefacto..."
                  disabled={Boolean(prendaPrecarga)}
                  style={{ width: 300 }}
                />
              </Form.Item>
            ) : null}
            <Form.Item
              name="fechaPrimerPago"
              label="Primer pago"
              rules={[{ required: true }]}
            >
              <Input type="date" style={{ width: 160 }} />
            </Form.Item>
            <Form.Item name="gastosAdm" label="Trámite adm.">
              <InputNumber min={0} step={1} style={{ width: 120 }} />
            </Form.Item>
          </div>
          <Form.Item style={{ marginBottom: 0 }}>
            <Button
              type="primary"
              icon={<CalculatorOutlined />}
              htmlType="submit"
              loading={simular.isPending}
              disabled={!prospectoListo || !productoId}
            >
              Simular
            </Button>
          </Form.Item>
        </Form>
        {tem != null && (
          <Text type="secondary" style={{ display: 'block', marginTop: 12 }}>
            TEM calculado: <Text strong>{tem.toFixed(4)}%</Text>
          </Text>
        )}
      </CredixPanel>

      <CredixPanel
        className="simulador-plan-panel"
        title={
          cuotas.length > 0
            ? `Plan simulado — total: ${formatMoney(totalCuota)}`
            : 'Plan simulado'
        }
      >
        <CredixDataTable<SimuladorCreditoCuota>
          mode="operacion"
          className="simulador-plan-table"
          rowKey={(r, i) => String(r.numero ?? i)}
          columns={columns}
          dataSource={cuotas}
          loading={simular.isPending}
          pagination={false}
          size="small"
          tableLayout="fixed"
          scroll={{ x: 820, y: cuotas.length > 12 ? 440 : undefined }}
          locale={{ emptyText: 'Seleccione cliente/prospecto, producto y pulse Simular' }}
        />
        {cuotas.length > 0 ? (
          <div style={{ marginTop: 16 }}>
            <Descriptions
              size="small"
              column={{ xs: 1, sm: 2, md: 3 }}
              style={{ marginBottom: 12 }}
              items={[
                { key: 'producto', label: 'Producto', children: productoSeleccionado?.denominacion ?? '—' },
                { key: 'cliente', label: 'Cliente/prospecto', children: clienteParaReporte },
                { key: 'saldo', label: 'Saldo final', children: formatMoney(cuotas.at(-1)?.saldo ?? 0) },
              ]}
            />
            <InformeExportBar
              csvLoading={exportCsv.isPending}
              pdfLoading={exportPdf.isPending}
              csvDisabled={!productoId}
              pdfDisabled={!productoId}
              onCsv={async () => {
                const params = buildReporteParams()
                if (params) exportCsv.mutate(params)
              }}
              onPdfTabular={async () => {
                const params = buildReporteParams()
                if (params) exportPdf.mutate(params)
              }}
            />
          </div>
        ) : null}
      </CredixPanel>

      <CredixPanel title="3. Solicitud (estado CRE)">
        {!personaId ? (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message="Simulación para prospecto"
            description="Puede imprimir o exportar el plan. Para generar crédito debe registrar o seleccionar el cliente."
          />
        ) : null}
        {solicitudCreditoId ? (
          <Alert
            type="info"
            showIcon
            message={`Solicitud #${solicitudCreditoId} lista para generar crédito`}
          />
        ) : (
          <>
            <Paragraph type="secondary">
              Crea la solicitud asociada al cliente antes de generar el crédito.
            </Paragraph>
            <Button
              type="primary"
              icon={<UserAddOutlined />}
              loading={crearSolicitud.isPending}
              disabled={!personaId || oficinaId < 1}
              onClick={() => crearSolicitud.mutate()}
            >
              Crear solicitud
            </Button>
          </>
        )}
      </CredixPanel>

      <CredixPanel title="4. Generar crédito">
        <Space direction="vertical" style={{ width: '100%' }} size="middle">
          <Tag color={productoId ? 'blue' : 'default'}>
            Producto: {productoSeleccionado?.denominacion ?? 'pendiente'}
          </Tag>
          <Select
            style={{ width: 320 }}
            value="ADE"
            disabled
            options={IND_GASTOS_ADM_OPTIONS}
          />
          <Input.TextArea
            rows={2}
            placeholder="Observación (opcional)"
            value={observacion}
            onChange={(e) => setObservacion(e.target.value)}
          />
          <Checkbox
            checked={indCentralRiesgo}
            onChange={(e) => setIndCentralRiesgo(e.target.checked)}
          >
            Incluir en central de riesgo
          </Checkbox>
          <Button
            type="primary"
            icon={<FileAddOutlined />}
            loading={generarCredito.isPending}
            disabled={
              !solicitudCreditoId ||
              !productoId ||
              cuotas.length < 1 ||
              oficinaId < 1
            }
            onClick={() => generarCredito.mutate()}
          >
            Generar crédito para aprobación
          </Button>
        </Space>
      </CredixPanel>
    </CredixPage>
  )
}
