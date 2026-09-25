import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  Descriptions,
  Form,
  Input,
  InputNumber,
  Modal,
  Radio,
  Row,
  Select,
  Space,
  Switch,
  Typography,
  Upload,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { getAccessToken } from '../../auth/tokenStorage'
import { buscarClientes, crearPersonaRapida } from '../../api/clientes'
import { fetchEstadoPlanPago } from '../../api/creditoPlanes'
import { fetchCondonacionPendienteCredito } from '../../api/creditoCondonacion'
import {
  actualizarAvalCredito,
  actualizarIrrecuperableCredito,
  actualizarTopeCredito,
  cambiarAnalistaCredito,
  condonarCredito,
  eliminarEvidenciaCredito,
  fetchCargosCredito,
  fetchCreditoContexto,
  fetchPrendas,
  fetchEvidenciasCredito,
  guardarPrendas,
  guardarCargoCredito,
  modificarCentralRiesgoCredito,
  modificarTramiteAdmCredito,
  observarCredito,
  subirEvidenciaCredito,
  type CargoCreditoRow,
  type CreditoEvidencia,
  type PrendaItem,
} from '../../api/creditoGestion'
import { PrendasEditor } from '../../components/credito/PrendasEditor'
import { CredixDataTable } from '../../components/credix'
import { prendaAItem, prendaVacia, prendasValidas } from '../../utils/prendas'
import { abrirWhatsAppPrendario } from '../../utils/prendarioWhatsapp'
import {
  downloadActaEntregaPrendarioPdf,
  downloadContratoPrendarioPdf,
} from '../../api/prendario'
import { fetchRptCliente } from '../../api/creditoPlanes'
import { fetchValoresTabla } from '../../api/maestros'
import { fetchUsuariosGestion } from '../../api/usuariosAdmin'
import { ApiError } from '../../api/errors'
import { consultarDniApiPeru } from '../../api/apiperu'
import { formatMoney } from '../../utils/formatMoney'
import {
  puedeCambiarAnalistaCreditoUi,
  puedeCondonarCreditoUi,
  puedeEditarTopeCreditoUi,
  puedeEditarTramiteCentralAvalUi,
  puedeCompletarBienesPrendarioUi,
  esCreditoProductoPrendario,
  puedeOperarCicloCredito,
  tieneCreditoModoLectura,
} from '../../utils/creditoOperacionPermisos'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import type { EstadoPlanPagoCuota } from '../../types/api'

const { Paragraph, Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

interface Props {
  creditoId: number
  oficinaId: number
  roles: string[]
  /** Carga datos solo cuando la pestaña Gestión está visible. */
  activo?: boolean
}

type ConceptoCondonacion = 'capital' | 'interes' | 'mora' | 'cargos'

export function CreditoConsultaGestionPanel({
  creditoId,
  oficinaId,
  roles,
  activo = true,
}: Props) {
  const soloLectura = tieneCreditoModoLectura(roles)
  const puedeGestion = puedeOperarCicloCredito(roles)
  const puedeCondonar = puedeCondonarCreditoUi(roles)
  const puedeTope = puedeEditarTopeCreditoUi(roles)
  const puedeAnalista = puedeCambiarAnalistaCreditoUi(roles)
  const puedeTramite = puedeEditarTramiteCentralAvalUi(roles)
  const puedeAval = !soloLectura && (puedeTramite || puedeAnalista || puedeGestion)
  const bloqueadoGestion = soloLectura || !puedeGestion
  const spaBase = import.meta.env.BASE_URL || '/'
  const queryClient = useQueryClient()
  const [modalCondonar, setModalCondonar] = useState(false)
  const [modalObservar, setModalObservar] = useState(false)
  const [modalCargo, setModalCargo] = useState(false)
  const [modalNuevoAval, setModalNuevoAval] = useState(false)
  const [montoCxc, setMontoCxc] = useState(0)
  const [montoCond, setMontoCond] = useState(0)
  const [obsCondonar, setObsCondonar] = useState('')
  const [condonarPartes, setCondonarPartes] = useState({
    capital: true,
    interes: true,
    mora: true,
    cargos: true,
  })
  const [condonarMontos, setCondonarMontos] = useState({
    capital: 0,
    interes: 0,
    mora: 0,
    cargos: 0,
  })
  const [observacion, setObservacion] = useState('')
  const [tipoCargoId, setTipoCargoId] = useState<number | null>(null)
  const [montoCargo, setMontoCargo] = useState(0)
  const [descCargo, setDescCargo] = useState('')
  const [cargoFinal, setCargoFinal] = useState(false)
  const [analistaId, setAnalistaId] = useState<number | null>(null)
  const [tope, setTope] = useState(0)
  const [tramiteAdm, setTramiteAdm] = useState(0)
  const [centralRiesgo, setCentralRiesgo] = useState(0)
  const [personaAvalId, setPersonaAvalId] = useState<number | null>(null)
  const [avalSearch, setAvalSearch] = useState('')
  const [nuevoAvalDni, setNuevoAvalDni] = useState('')
  const [nuevoAvalNombre, setNuevoAvalNombre] = useState('')
  const [nuevoAvalPaterno, setNuevoAvalPaterno] = useState('')
  const [nuevoAvalMaterno, setNuevoAvalMaterno] = useState('')
  const [nuevoAvalCelular, setNuevoAvalCelular] = useState('')
  const [fechaRematePrenda, setFechaRematePrenda] = useState('')
  const [prendas, setPrendas] = useState<PrendaItem[]>([prendaVacia()])

  const puedeBienesPrendario = puedeCompletarBienesPrendarioUi(roles)

  const contexto = useQuery({
    queryKey: ['credito-contexto', creditoId],
    queryFn: () => fetchCreditoContexto(creditoId),
    enabled: activo,
    staleTime: creditoStaleTime.operacion,
  })
  const ctx = contexto.data

  const fichaCliente = useQuery({
    queryKey: ['rpt-cliente-gestion', ctx?.personaId],
    queryFn: () => fetchRptCliente(ctx!.personaId),
    enabled: activo && (ctx?.personaId ?? 0) > 0,
    staleTime: creditoStaleTime.ficha,
  })

  const cargos = useQuery({
    queryKey: ['cargos-credito', oficinaId, creditoId],
    queryFn: () => fetchCargosCredito(oficinaId, creditoId),
    enabled: activo,
    staleTime: creditoStaleTime.operacion,
  })

  const evidencias = useQuery({
    queryKey: ['evidencias-credito', oficinaId, creditoId],
    queryFn: () => fetchEvidenciasCredito(oficinaId, creditoId),
    enabled: activo,
    staleTime: creditoStaleTime.operacion,
  })

  const prendasCredito = useQuery({
    queryKey: ['prendas', oficinaId, creditoId],
    queryFn: () => fetchPrendas(oficinaId, creditoId),
    enabled: activo && oficinaId > 0 && creditoId > 0,
    staleTime: creditoStaleTime.operacion,
  })

  const tiposCargo = useQuery({
    queryKey: ['valores-tabla', 2],
    queryFn: () => fetchValoresTabla(2),
    enabled: activo,
    staleTime: creditoStaleTime.master,
  })

  const planPago = useQuery({
    queryKey: ['estado-plan-pago', creditoId],
    queryFn: () => fetchEstadoPlanPago(creditoId),
    enabled: activo && puedeCondonar,
    staleTime: creditoStaleTime.operacion,
  })

  const condonacionPendiente = useQuery({
    queryKey: ['condonacion-pendiente', creditoId],
    queryFn: () => fetchCondonacionPendienteCredito(creditoId),
    enabled: activo && puedeCondonar,
    staleTime: creditoStaleTime.operacion,
  })

  const usuariosGestion = useQuery({
    queryKey: ['usuarios-gestion-credito-ajustes'],
    queryFn: () => fetchUsuariosGestion({ page: 1, pageSize: 500, incluirInactivos: false }),
    enabled: activo && puedeAnalista,
    staleTime: creditoStaleTime.master,
  })

  const buscarAval = useMutation({
    mutationFn: (term: string) => buscarClientes(term),
  })

  const refrescar = () => {
    void queryClient.invalidateQueries({ queryKey: ['credito-contexto', creditoId] })
    void queryClient.invalidateQueries({ queryKey: ['cargos-credito', oficinaId, creditoId] })
    void queryClient.invalidateQueries({ queryKey: ['evidencias-credito', oficinaId, creditoId] })
    void queryClient.invalidateQueries({ queryKey: ['condonacion-pendiente', creditoId] })
  }

  useEffect(() => {
    if (!contexto.data) return
    setTramiteAdm(contexto.data.montoGastosAdm ?? 0)
    setCentralRiesgo(contexto.data.centralRiesgo ?? 0)
    setPersonaAvalId(contexto.data.personaAvalId ?? null)
    setFechaRematePrenda(contexto.data.fechaRemate?.slice(0, 10) ?? '')
    const guardadas = prendasCredito.data ?? []
    setPrendas(guardadas.length > 0 ? guardadas.map(prendaAItem) : [prendaVacia()])
  }, [contexto.data, prendasCredito.data])

  const condonar = useMutation({
    mutationFn: () => {
      const detalle = [
        condonarPartes.capital ? `Capital ${formatMoney(condonarMontos.capital)}` : null,
        condonarPartes.interes ? `Interés ${formatMoney(condonarMontos.interes)}` : null,
        condonarPartes.mora ? `Mora ${formatMoney(condonarMontos.mora)}` : null,
        condonarPartes.cargos ? `Cargos ${formatMoney(condonarMontos.cargos)}` : null,
      ].filter(Boolean)
      const observacionDetalle = [
        `CONDONACION: ${detalle.join(' · ') || 'Sin conceptos seleccionados'}`,
        obsCondonar.trim(),
      ]
        .filter(Boolean)
        .join(' | ')

      return condonarCredito({
        oficinaId,
        creditoId,
        montoCxc,
        montoCondonacion: montoCond,
        observacion: observacionDetalle,
      })
    },
    onSuccess: () => {
      message.success('Crédito condonado')
      setModalCondonar(false)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const observar = useMutation({
    mutationFn: () => observarCredito({ oficinaId, creditoId, observacion }),
    onSuccess: () => {
      message.success('Observación guardada')
      setModalObservar(false)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const guardarCargo = useMutation({
    mutationFn: () =>
      guardarCargoCredito({
        oficinaId,
        creditoId,
        tipoCargoId: tipoCargoId!,
        monto: montoCargo,
        descripcion: descCargo,
        final: cargoFinal,
      }),
    onSuccess: () => {
      message.success('Cargo registrado')
      setModalCargo(false)
      setTipoCargoId(null)
      setMontoCargo(0)
      setDescCargo('')
      setCargoFinal(false)
      refrescar()
      void queryClient.invalidateQueries({ queryKey: ['estado-plan-pago', creditoId] })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const subirImg = useMutation({
    mutationFn: (file: File) => subirEvidenciaCredito(oficinaId, creditoId, file),
    onSuccess: () => {
      message.success('Imagen subida')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const eliminarImg = useMutation({
    mutationFn: (creditoImagenId: number) =>
      eliminarEvidenciaCredito({ oficinaId, creditoImagenId }),
    onSuccess: () => {
      message.success('Evidencia eliminada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const irrecuperable = useMutation({
    mutationFn: (val: boolean) =>
      actualizarIrrecuperableCredito({ oficinaId, creditoId, indIrrecuperable: val }),
    onSuccess: () => {
      message.success('Indicador actualizado')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const cambiarAnalista = useMutation({
    mutationFn: () =>
      cambiarAnalistaCredito({ oficinaId, creditoId, analistaId: analistaId! }),
    onSuccess: () => {
      message.success('Analista actualizado')
      setAnalistaId(null)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const actualizarTope = useMutation({
    mutationFn: () =>
      actualizarTopeCredito({
        oficinaId,
        personaId: contexto.data!.personaId,
        topeCredito: tope,
      }),
    onSuccess: () => message.success('Tope de crédito actualizado'),
    onError: (e) => message.error(errMsg(e)),
  })

  const guardarTramite = useMutation({
    mutationFn: () =>
      modificarTramiteAdmCredito({ oficinaId, creditoId, valor: tramiteAdm }),
    onSuccess: () => {
      message.success('Trámite administrativo actualizado')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const guardarCentral = useMutation({
    mutationFn: () =>
      modificarCentralRiesgoCredito({ oficinaId, creditoId, valor: centralRiesgo }),
    onSuccess: () => {
      message.success('Central de riesgo actualizada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const guardarAval = useMutation({
    mutationFn: (avalId: number | null = personaAvalId) =>
      actualizarAvalCredito({ oficinaId, creditoId, personaAvalId: avalId }),
    onSuccess: (_r, avalId) => {
      message.success(avalId == null ? 'Aval quitado' : 'Aval actualizado')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const crearNuevoAval = useMutation({
    mutationFn: () =>
      crearPersonaRapida({
        dni: nuevoAvalDni.trim(),
        nombre: nuevoAvalNombre.trim().toUpperCase(),
        apePaterno: nuevoAvalPaterno.trim().toUpperCase(),
        apeMaterno: nuevoAvalMaterno.trim().toUpperCase(),
        celular: nuevoAvalCelular.trim() || null,
      }),
    onSuccess: (r) => {
      if (!r.success || r.personaId < 1) {
        message.error(r.label || 'No se pudo crear el aval')
        return
      }
      setPersonaAvalId(r.personaId)
      setAvalSearch(r.label)
      setModalNuevoAval(false)
      setNuevoAvalDni('')
      setNuevoAvalNombre('')
      setNuevoAvalPaterno('')
      setNuevoAvalMaterno('')
      setNuevoAvalCelular('')
      message.success('Aval creado; asignando al crédito')
      guardarAval.mutate(r.personaId)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const validarDniAval = useMutation({
    mutationFn: () => consultarDniApiPeru(nuevoAvalDni.trim()),
    onSuccess: (r) => {
      if (!r.success) {
        message.warning(r.mensaje || 'DNI no encontrado en API Perú')
        return
      }
      setNuevoAvalNombre(r.nombres ?? '')
      setNuevoAvalPaterno(r.apellidoPaterno ?? '')
      setNuevoAvalMaterno(r.apellidoMaterno ?? '')
      message.success('DNI validado')
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const guardarPrendario = useMutation({
    mutationFn: () =>
      guardarPrendas({
        oficinaId,
        creditoId,
        prendas: prendasValidas(prendas),
        fechaRemate: fechaRematePrenda || null,
      }),
    onSuccess: () => {
      message.success('Bienes en custodia guardados')
      refrescar()
      void queryClient.invalidateQueries({ queryKey: ['prendas', oficinaId, creditoId] })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const cargoCols: ColumnsType<CargoCreditoRow> = [
    { title: 'Tipo', dataIndex: 'tipoCargo', ellipsis: true },
    { title: 'Cuota', dataIndex: 'numCuota', width: 60 },
    { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true },
    {
      title: 'Importe',
      dataIndex: 'importe',
      align: 'right',
      render: formatMoney,
    },
    { title: 'Estado', dataIndex: 'estado', width: 70 },
  ]

  const analistaOptions = useMemo(
    () =>
      (usuariosGestion.data?.rows ?? []).map((u) => ({
        value: u.usuarioId,
        label: `${u.nombreCompleto || u.nombreUsuario} (#${u.usuarioId})`,
      })),
    [usuariosGestion.data],
  )
  const avalOptions = useMemo(() => {
    const options = (buscarAval.data ?? []).map((c) => ({
      value: c.personaId,
      label: c.label,
    }))
    if (
      ctx?.personaAvalId &&
      ctx.personaAvalNombre &&
      !options.some((o) => o.value === ctx.personaAvalId)
    ) {
      options.unshift({
        value: ctx.personaAvalId,
        label: `${ctx.personaAvalNombre} (#${ctx.personaAvalId})`,
      })
    }
    return options
  }, [buscarAval.data, ctx?.personaAvalId, ctx?.personaAvalNombre])

  const condonacionResumen = useMemo(() => {
    const pendientes = (planPago.data ?? []).filter((c: EstadoPlanPagoCuota) =>
      c.estado?.toUpperCase() === 'PEN',
    )
    const capital = pendientes.reduce((s, c) => s + (c.amortizacion ?? c.capital ?? 0), 0)
    const interes = pendientes.reduce((s, c) => s + (c.interes ?? 0), 0)
    const mora = pendientes.reduce((s, c) => s + (c.importeMora ?? 0), 0)
    const cargos = pendientes.reduce((s, c) => s + (c.cargo ?? 0), 0)
    const descuentos = pendientes.reduce((s, c) => s + (c.descuento ?? 0), 0)
    const total = Math.max(0, capital + interes + mora + cargos - descuentos)
    return { capital, interes, mora, cargos, descuentos, total, cuotas: pendientes.length }
  }, [planPago.data])

  const totalCondonacionSeleccionado = useMemo(() => {
    const total =
      (condonarPartes.capital ? condonarMontos.capital : 0) +
      (condonarPartes.interes ? condonarMontos.interes : 0) +
      (condonarPartes.mora ? condonarMontos.mora : 0) +
      (condonarPartes.cargos ? condonarMontos.cargos : 0)
    return Math.max(0, Number(total.toFixed(2)))
  }, [condonarMontos, condonarPartes])

  const abrirModalCondonar = () => {
    const montos = {
      capital: Number(condonacionResumen.capital.toFixed(2)),
      interes: Number(condonacionResumen.interes.toFixed(2)),
      mora: Number(condonacionResumen.mora.toFixed(2)),
      cargos: Number(condonacionResumen.cargos.toFixed(2)),
    }
    setCondonarMontos(montos)
    setCondonarPartes({
      capital: montos.capital > 0,
      interes: montos.interes > 0,
      mora: montos.mora > 0,
      cargos: montos.cargos > 0,
    })
    const pendiente = condonacionPendiente.data
    if (pendiente?.tienePendiente) {
      montos.mora = Number(pendiente.moraCondonacion.toFixed(2))
      setCondonarMontos({ ...montos })
      setCondonarPartes({
        capital: montos.capital > 0,
        interes: montos.interes > 0,
        mora: true,
        cargos: montos.cargos > 0,
      })
    }
    const total = montos.capital + montos.interes + montos.mora + montos.cargos
    setMontoCxc(Number(total.toFixed(2)))
    setMontoCond(Number(total.toFixed(2)))
    setObsCondonar('')
    setModalCondonar(true)
  }

  useEffect(() => {
    if (!modalCondonar) return
    setMontoCxc(totalCondonacionSeleccionado)
    setMontoCond(totalCondonacionSeleccionado)
  }, [modalCondonar, totalCondonacionSeleccionado])

  const condonacionConceptos: Array<{
    key: ConceptoCondonacion
    label: string
    deuda: number
  }> = [
    { key: 'capital', label: 'Capital', deuda: condonacionResumen.capital },
    { key: 'interes', label: 'Interés', deuda: condonacionResumen.interes },
    { key: 'mora', label: 'Mora', deuda: condonacionResumen.mora },
    { key: 'cargos', label: 'Cargos', deuda: condonacionResumen.cargos },
  ]

  return (
    <>
      <div className="credito-gestion-tab">
      {soloLectura ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Modo solo lectura"
          description="Su rol no permite condonar, observar, cargos ni otras operaciones de gestión."
        />
      ) : null}

      <Card
        size="small"
        className="credito-gestion-card credito-gestion-context-card"
        loading={contexto.isLoading}
      >
        <Paragraph style={{ marginBottom: 8 }}>
          <Text strong>Cliente:</Text> {ctx?.personaNombre ?? '—'} (persona #{ctx?.personaId})
        </Paragraph>
        {ctx?.observacion ? (
          <Paragraph type="secondary" style={{ marginBottom: 8 }}>
            <Text strong>Observación:</Text> {ctx.observacion}
          </Paragraph>
        ) : null}
        {condonacionPendiente.data?.tienePendiente ? (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 8 }}
            message="Hay una solicitud de condonación pendiente"
            description={`Mora pedida S/ ${formatMoney(condonacionPendiente.data.moraCondonacion)}. Al condonar se aprueba y se cobra la cuenta por cobrar en la caja que la originó.`}
          />
        ) : null}
        <Space wrap className="credito-gestion-context-card__actions">
          {puedeCondonar ? (
            <Button disabled={bloqueadoGestion} onClick={abrirModalCondonar}>
              Condonar
            </Button>
          ) : null}
          <Button
            disabled={bloqueadoGestion}
            onClick={() => {
              setObservacion(ctx?.observacion ?? '')
              setModalObservar(true)
            }}
          >
            Observar
          </Button>
          <Button
            disabled={bloqueadoGestion}
            onClick={() => {
              setTipoCargoId(null)
              setMontoCargo(0)
              setDescCargo('')
              setCargoFinal(false)
              setModalCargo(true)
            }}
          >
            Nuevo cargo
          </Button>
          <Switch
            disabled={bloqueadoGestion}
            checkedChildren="Irrecuperable"
            unCheckedChildren="Irrecuperable"
            checked={ctx?.indIrrecuperable ?? false}
            loading={irrecuperable.isPending}
            onChange={(v) => irrecuperable.mutate(v)}
          />
        </Space>
      </Card>

      <Card
        title="Datos completos del cliente"
        size="small"
        className="credito-gestion-cliente-card"
        loading={fichaCliente.isLoading}
      >
        {fichaCliente.data?.ficha ? (
          <Descriptions
            size="small"
            bordered
            column={{ xs: 1, md: 2, xl: 3 }}
            className="credito-gestion-cliente-card__desc"
          >
            <Descriptions.Item label="Cliente" span={2}>
              {fichaCliente.data.ficha.cliente}
            </Descriptions.Item>
            <Descriptions.Item label="DNI">
              {fichaCliente.data.ficha.numeroDocumento}
            </Descriptions.Item>
            <Descriptions.Item label="Celular">
              {fichaCliente.data.ficha.celular || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="F. nacimiento">
              {fichaCliente.data.ficha.fechaNacimiento || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Sexo">
              {fichaCliente.data.ficha.sexo || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Estado civil">
              {fichaCliente.data.ficha.estadoCivil || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Tipo vivienda">
              {fichaCliente.data.ficha.tipoVivienda || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Distrito">
              {fichaCliente.data.ficha.distrito || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Actividad económica">
              {fichaCliente.data.ficha.actividadEconomica || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Dirección" span={2}>
              {fichaCliente.data.ficha.direccion || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Referencia">
              {fichaCliente.data.ficha.direccionRef || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Dirección negocio" span={2}>
              {fichaCliente.data.ficha.direccionNegocio || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Ref. negocio">
              {fichaCliente.data.ficha.direccionNegocioRef || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Cónyuge">
              {fichaCliente.data.ficha.conyugue || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="DNI cónyuge">
              {fichaCliente.data.ficha.conyugueDni || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Celular cónyuge">
              {fichaCliente.data.ficha.conyugueCelular || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Nota" span={3}>
              {fichaCliente.data.ficha.nota || '—'}
            </Descriptions.Item>
          </Descriptions>
        ) : (
          <Text type="secondary">Seleccione un crédito para ver la ficha del cliente.</Text>
        )}
      </Card>

      <Row gutter={[16, 16]} className="credito-gestion-two-col-grid">
        <Col xs={24} lg={12}>
          <Card title="Cargos" size="small" className="credito-gestion-card credito-gestion-table-card">
            <CredixDataTable<CargoCreditoRow>
              mode="operacion"
              rowKey="cargoId"
              columns={cargoCols}
              dataSource={cargos.data ?? []}
              loading={cargos.isLoading}
              pagination={false}
              scroll={{ x: 560 }}
            />
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card
            title="Evidencias"
            size="small"
            className="credito-gestion-card credito-gestion-evidencias-card"
            extra={
              <Upload
                disabled={bloqueadoGestion}
                showUploadList={false}
                accept="image/*"
                beforeUpload={(file) => {
                  subirImg.mutate(file)
                  return false
                }}
              >
                <Button size="small" loading={subirImg.isPending}>
                  Subir
                </Button>
              </Upload>
            }
          >
            <Space direction="vertical" style={{ width: '100%' }}>
              {(evidencias.data ?? []).map((ev: CreditoEvidencia) => (
                <EvidenciaThumb
                  key={ev.id}
                  ev={ev}
                  onDelete={() => eliminarImg.mutate(ev.id)}
                  deleting={eliminarImg.isPending}
                />
              ))}
              {!evidencias.isLoading && (evidencias.data?.length ?? 0) === 0 ? (
                <Text type="secondary">Sin imágenes</Text>
              ) : null}
            </Space>
          </Card>
        </Col>
      </Row>

      {puedeTramite || puedeAnalista || puedeTope || puedeAval ? (
        <Card
          title="Ajustes administrativos"
          size="small"
          className="credito-gestion-card credito-ajustes-card"
        >
          {puedeTramite || puedeAval ? (
            <Row gutter={[16, 16]} className="credito-ajustes-grid">
              {puedeTramite ? (
                <>
                  <Col xs={24} md={8} className="credito-ajustes-item">
                    <Paragraph strong>Trámite administrativo</Paragraph>
                    <InputNumber
                      style={{ width: '100%', marginBottom: 8 }}
                      min={0}
                      precision={2}
                      value={tramiteAdm}
                      onChange={(v) => setTramiteAdm(v ?? 0)}
                    />
                    <Button
                      block
                      disabled={bloqueadoGestion}
                      loading={guardarTramite.isPending}
                      onClick={() => guardarTramite.mutate()}
                    >
                      Guardar trámite adm.
                    </Button>
                  </Col>
                  <Col xs={24} md={8} className="credito-ajustes-item">
                    <Paragraph strong>Central de riesgo</Paragraph>
                    <InputNumber
                      style={{ width: '100%', marginBottom: 8 }}
                      min={0}
                      precision={2}
                      value={centralRiesgo}
                      onChange={(v) => setCentralRiesgo(v ?? 0)}
                    />
                    <Button
                      block
                      disabled={bloqueadoGestion}
                      loading={guardarCentral.isPending}
                      onClick={() => guardarCentral.mutate()}
                    >
                      Guardar central riesgo
                    </Button>
                  </Col>
                </>
              ) : null}
              {puedeAval ? (
                <Col xs={24} md={puedeTramite ? 8 : 12} className="credito-ajustes-item">
                  <Paragraph strong>Aval del crédito</Paragraph>
                  {ctx?.personaAvalNombre ? (
                    <Paragraph type="secondary" style={{ marginBottom: 8 }}>
                      Actual: {ctx.personaAvalNombre}
                    </Paragraph>
                  ) : null}
                  <Select
                    style={{ width: '100%', marginBottom: 8 }}
                    showSearch
                    allowClear
                    filterOption={false}
                    placeholder="Buscar aval por DNI, código o nombre"
                    notFoundContent={
                      avalSearch.trim().length < 2 ? 'Ingrese al menos 2 caracteres' : null
                    }
                    loading={buscarAval.isPending}
                    options={avalOptions}
                    value={personaAvalId ?? undefined}
                    onSearch={(term) => {
                      setAvalSearch(term)
                      if (term.trim().length >= 2) {
                        buscarAval.mutate(term.trim())
                      }
                    }}
                    onChange={(v) => setPersonaAvalId(v ?? null)}
                  />
                  <div className="credito-ajustes-actions">
                    <Button
                      block
                      disabled={soloLectura}
                      onClick={() => setModalNuevoAval(true)}
                    >
                      Nuevo aval
                    </Button>
                    <Button
                      block
                      disabled={!personaAvalId}
                      href={
                        personaAvalId
                          ? `${spaBase}informes/reporte-cliente?personaId=${personaAvalId}`
                          : undefined
                      }
                      target="_blank"
                    >
                      Detalle aval
                    </Button>
                    <Button
                      block
                      disabled={soloLectura}
                      loading={guardarAval.isPending}
                      onClick={() => guardarAval.mutate(personaAvalId)}
                    >
                      Guardar aval
                    </Button>
                    <Button
                      block
                      disabled={soloLectura}
                      loading={guardarAval.isPending}
                      onClick={() => {
                        setPersonaAvalId(null)
                        guardarAval.mutate(null)
                      }}
                    >
                      Quitar aval
                    </Button>
                  </div>
                </Col>
              ) : null}
            </Row>
          ) : null}
          <Row gutter={[16, 16]} className="credito-ajustes-grid credito-ajustes-grid--secondary">
            {puedeAnalista ? (
              <Col xs={24} md={8} className="credito-ajustes-item">
                <Paragraph strong>Cambiar analista</Paragraph>
                <Select
                  style={{ width: '100%', marginBottom: 8 }}
                  showSearch
                  allowClear
                  optionFilterProp="label"
                  loading={usuariosGestion.isLoading}
                  options={analistaOptions}
                  placeholder="Buscar gestor/analista activo"
                  value={analistaId ?? undefined}
                  onChange={(v) => setAnalistaId(v ?? null)}
                />
                <Button
                  block
                  disabled={soloLectura || !analistaId}
                  loading={cambiarAnalista.isPending}
                  onClick={() => cambiarAnalista.mutate()}
                >
                  Asignar analista
                </Button>
              </Col>
            ) : null}
            {puedeTope ? (
              <Col xs={24} md={8} className="credito-ajustes-item">
                <Paragraph strong>Tope de crédito (persona)</Paragraph>
                <InputNumber
                  style={{ width: '100%', marginBottom: 8 }}
                  min={0}
                  value={tope}
                  onChange={(v) => setTope(v ?? 0)}
                />
                <Button
                  block
                  loading={actualizarTope.isPending}
                  disabled={soloLectura || !ctx?.personaId}
                  onClick={() => actualizarTope.mutate()}
                >
                  Guardar tope
                </Button>
              </Col>
            ) : null}
          </Row>
        </Card>
      ) : null}

      {esCreditoProductoPrendario(ctx ?? {}) && (puedeBienesPrendario || puedeTramite) ? (
        <Card
          title="Crédito prendario"
          size="small"
          className="credito-gestion-card credito-prendario-card"
        >
          {(() => {
            const bienesPersistidos = (prendasCredito.data ?? []).length > 0
            const puedeEditarBienes =
              puedeBienesPrendario && !bienesPersistidos && !bloqueadoGestion
            const hayBienesValidos = prendasValidas(prendas).length > 0
            return (
              <>
                {!bienesPersistidos ? (
                  <Alert
                    type="warning"
                    showIcon
                    style={{ marginBottom: 12 }}
                    message="Sin bienes en custodia"
                    description={
                      puedeEditarBienes
                        ? 'Complete los bienes una sola vez. Luego podrá emitir contrato, acta y WhatsApp.'
                        : 'Este crédito prendario aún no tiene bienes registrados.'
                    }
                  />
                ) : (
                  <Alert
                    type="success"
                    showIcon
                    style={{ marginBottom: 12 }}
                    message="Bienes registrados"
                    description="Consulta y documentos. Los bienes no se pueden modificar después de guardados."
                  />
                )}
                <Row gutter={[16, 16]} style={{ marginBottom: 12 }}>
                  <Col xs={24} md={12}>
                    <Paragraph strong>Fecha remate</Paragraph>
                    <Input
                      type="date"
                      disabled={!puedeEditarBienes}
                      value={fechaRematePrenda}
                      onChange={(e) => setFechaRematePrenda(e.target.value)}
                    />
                    <Text type="secondary">Si se deja vacía, el vencimiento más 30 días.</Text>
                  </Col>
                  <Col xs={24} md={12}>
                    <Paragraph strong>Contrato</Paragraph>
                    <Text>
                      {contexto.data?.numeroContratoPrendario ??
                        (bienesPersistidos ? String(creditoId) : '(se asigna al guardar)')}
                    </Text>
                  </Col>
                </Row>
                <PrendasEditor
                  value={prendas}
                  onChange={setPrendas}
                  disabled={!puedeEditarBienes}
                  readOnly={!puedeEditarBienes && bienesPersistidos}
                />
                <Space wrap className="credito-prendario-actions" style={{ marginTop: 12 }}>
                  {puedeEditarBienes ? (
                    <Button
                      type="primary"
                      disabled={!hayBienesValidos}
                      loading={guardarPrendario.isPending}
                      onClick={() => guardarPrendario.mutate()}
                    >
                      Guardar bienes
                    </Button>
                  ) : null}
                  <Button
                    disabled={!bienesPersistidos}
                    onClick={() => {
                      void downloadContratoPrendarioPdf(
                        oficinaId,
                        creditoId,
                        contexto.data?.numeroContratoPrendario || String(creditoId),
                      ).catch((e) => message.error(errMsg(e)))
                    }}
                  >
                    Contrato PDF
                  </Button>
                  <Button
                    disabled={!bienesPersistidos}
                    onClick={() => {
                      void downloadActaEntregaPrendarioPdf(
                        oficinaId,
                        creditoId,
                        contexto.data?.numeroContratoPrendario || String(creditoId),
                      ).catch((e) => message.error(errMsg(e)))
                    }}
                  >
                    Acta PDF
                  </Button>
                  <Button
                    disabled={bloqueadoGestion}
                    title="Abre WhatsApp con mensaje de aviso prendario"
                    onClick={() => {
                      const celular = contexto.data?.personaCelular
                      const nombre = contexto.data?.personaNombre ?? ''
                      const ok = abrirWhatsAppPrendario(celular, nombre, creditoId)
                      if (!ok) {
                        message.warning('Este cliente no tiene celular registrado')
                      }
                    }}
                  >
                    WhatsApp (aviso)
                  </Button>
                </Space>
              </>
            )
          })()}
        </Card>
      ) : null}
      </div>

      <Modal
        title="Nuevo aval"
        open={modalNuevoAval}
        onCancel={() => setModalNuevoAval(false)}
        onOk={() => {
          if (nuevoAvalDni.trim().length !== 8) {
            message.warning('Ingrese DNI de 8 dígitos')
            return
          }
          if (!nuevoAvalNombre.trim() || !nuevoAvalPaterno.trim() || !nuevoAvalMaterno.trim()) {
            message.warning('Complete nombres y apellidos del aval')
            return
          }
          crearNuevoAval.mutate()
        }}
        confirmLoading={crearNuevoAval.isPending || guardarAval.isPending}
        okText="Crear y asignar"
      >
        <Paragraph type="secondary">
          Paridad con <strong>Nuevo Aval</strong> del MVC. Valide el DNI con API Perú,
          cree la persona y asígnela como aval del crédito actual.
        </Paragraph>
        <Form layout="vertical">
          <Form.Item label="DNI">
            <Space.Compact style={{ width: '100%' }}>
              <Input
                maxLength={8}
                value={nuevoAvalDni}
                onChange={(e) => setNuevoAvalDni(e.target.value.replace(/\D/g, ''))}
              />
              <Button
                loading={validarDniAval.isPending}
                disabled={nuevoAvalDni.trim().length !== 8}
                onClick={() => validarDniAval.mutate()}
              >
                Validar DNI
              </Button>
            </Space.Compact>
          </Form.Item>
          <Form.Item label="Nombres">
            <Input value={nuevoAvalNombre} onChange={(e) => setNuevoAvalNombre(e.target.value)} />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item label="Apellido paterno">
                <Input
                  value={nuevoAvalPaterno}
                  onChange={(e) => setNuevoAvalPaterno(e.target.value)}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item label="Apellido materno">
                <Input
                  value={nuevoAvalMaterno}
                  onChange={(e) => setNuevoAvalMaterno(e.target.value)}
                />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item label="Celular">
            <Input
              maxLength={15}
              value={nuevoAvalCelular}
              onChange={(e) => setNuevoAvalCelular(e.target.value)}
            />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Observar crédito"
        open={modalObservar}
        onCancel={() => setModalObservar(false)}
        onOk={() => observar.mutate()}
        confirmLoading={observar.isPending}
      >
        <Input.TextArea
          rows={4}
          value={observacion}
          onChange={(e) => setObservacion(e.target.value)}
        />
      </Modal>

      <Modal
        title="Condonar crédito"
        open={modalCondonar}
        onCancel={() => setModalCondonar(false)}
        onOk={() => {
          if (totalCondonacionSeleccionado <= 0) {
            message.warning('Seleccione al menos un concepto con monto mayor a cero')
            return
          }
          condonar.mutate()
        }}
        confirmLoading={condonar.isPending}
        okText="Crear cuenta por cobrar y condonar"
        width={720}
        className="credito-condonar-modal"
      >
        <div className="credito-condonar">
          <div className="credito-condonar__summary">
            <div>
              <Text type="secondary">Deuda calculada</Text>
              <Paragraph strong className="credito-condonar__amount">
                {formatMoney(
                  condonacionResumen.capital +
                    condonacionResumen.interes +
                    condonacionResumen.mora +
                    condonacionResumen.cargos,
                )}
              </Paragraph>
            </div>
            <div>
              <Text type="secondary">Cuotas pendientes</Text>
              <Paragraph strong className="credito-condonar__amount">
                {condonacionResumen.cuotas}
              </Paragraph>
            </div>
            <div>
              <Text type="secondary">A condonar</Text>
              <Paragraph strong className="credito-condonar__amount">
                {formatMoney(totalCondonacionSeleccionado)}
              </Paragraph>
            </div>
          </div>

          {condonacionResumen.descuentos > 0 ? (
            <Alert
              type="info"
              showIcon
              message={`Descuentos ya registrados: ${formatMoney(condonacionResumen.descuentos)}`}
              className="credito-condonar__alert"
            />
          ) : null}

          <div className="credito-condonar__toolbar">
            <Button
              size="small"
              onClick={() => {
                setCondonarPartes({ capital: true, interes: true, mora: true, cargos: true })
                setCondonarMontos({
                  capital: Number(condonacionResumen.capital.toFixed(2)),
                  interes: Number(condonacionResumen.interes.toFixed(2)),
                  mora: Number(condonacionResumen.mora.toFixed(2)),
                  cargos: Number(condonacionResumen.cargos.toFixed(2)),
                })
              }}
            >
              Seleccionar todo
            </Button>
            <Button
              size="small"
              onClick={() => {
                setCondonarPartes({ capital: false, interes: false, mora: true, cargos: true })
                setCondonarMontos((prev) => ({
                  ...prev,
                  mora: Number(condonacionResumen.mora.toFixed(2)),
                  cargos: Number(condonacionResumen.cargos.toFixed(2)),
                }))
              }}
            >
              Solo mora y cargos
            </Button>
            <Button
              size="small"
              onClick={() =>
                setCondonarPartes({ capital: false, interes: false, mora: false, cargos: false })
              }
            >
              Limpiar selección
            </Button>
          </div>

          <div className="credito-condonar__table">
            <div className="credito-condonar__row credito-condonar__row--head">
              <span>Concepto</span>
              <span>Deuda</span>
              <span>Condonar</span>
              <span>Monto</span>
            </div>
            {condonacionConceptos.map((concepto) => (
              <div className="credito-condonar__row" key={concepto.key}>
                <Text strong>{concepto.label}</Text>
                <Text>{formatMoney(concepto.deuda)}</Text>
                <Checkbox
                  checked={condonarPartes[concepto.key]}
                  disabled={concepto.deuda <= 0}
                  onChange={(e) =>
                    setCondonarPartes((prev) => ({
                      ...prev,
                      [concepto.key]: e.target.checked,
                    }))
                  }
                >
                  Aplicar
                </Checkbox>
                <InputNumber
                  min={0}
                  max={Math.max(0, concepto.deuda)}
                  precision={2}
                  disabled={!condonarPartes[concepto.key] || concepto.deuda <= 0}
                  value={condonarMontos[concepto.key]}
                  onChange={(v) =>
                    setCondonarMontos((prev) => ({
                      ...prev,
                      [concepto.key]: Number(v ?? 0),
                    }))
                  }
                />
              </div>
            ))}
          </div>

          <div className="credito-condonar__total">
            <Text>Cuenta por cobrar a crear</Text>
            <Text strong>{formatMoney(montoCxc)}</Text>
          </div>

          <Form.Item label="Observación">
            <Input.TextArea
              rows={3}
              placeholder="Motivo o sustento de la condonación"
              value={obsCondonar}
              onChange={(e) => setObsCondonar(e.target.value)}
            />
          </Form.Item>
        </div>
      </Modal>

      <Modal
        title="Nuevo cargo"
        open={modalCargo}
        onCancel={() => setModalCargo(false)}
        onOk={() => {
          if (!tipoCargoId) {
            message.warning('Seleccione tipo de cargo')
            return
          }
          if (montoCargo <= 0) {
            message.warning('Ingrese un monto mayor a cero')
            return
          }
          if (!descCargo.trim()) {
            message.warning('Ingrese la descripción del cargo')
            return
          }
          guardarCargo.mutate()
        }}
        confirmLoading={guardarCargo.isPending}
        okText="Crear cargo"
      >
        <Select
          style={{ width: '100%', marginBottom: 12 }}
          placeholder="Tipo de cargo"
          options={(tiposCargo.data ?? []).map((t) => ({
            value: t.itemId,
            label: t.denominacion,
          }))}
          value={tipoCargoId ?? undefined}
          onChange={setTipoCargoId}
        />
        <InputNumber
          style={{ width: '100%', marginBottom: 12 }}
          min={0}
          placeholder="Monto"
          value={montoCargo}
          onChange={(v) => setMontoCargo(v ?? 0)}
        />
        <Input
          style={{ marginBottom: 12 }}
          placeholder="Descripción"
          value={descCargo}
          onChange={(e) => setDescCargo(e.target.value)}
        />
        <Form.Item label="Aplicar cargo">
          <Radio.Group
            value={cargoFinal ? 'final' : 'actual'}
            onChange={(e) => setCargoFinal(e.target.value === 'final')}
          >
            <Radio value="actual">En la cuota actual</Radio>
            <Radio value="final">En la última cuota pendiente</Radio>
          </Radio.Group>
        </Form.Item>
      </Modal>
    </>
  )
}

function EvidenciaThumb({
  ev,
  onDelete,
  deleting,
}: {
  ev: CreditoEvidencia
  onDelete: () => void
  deleting: boolean
}) {
  const [src, setSrc] = useState<string | null>(null)
  const base = import.meta.env.VITE_API_BASE_URL as string
  const path = ev.url?.replace('/api/v1', '') ?? `/credito/evidencia-archivo/${ev.id}`

  useEffect(() => {
    let objectUrl: string | null = null
    const token = getAccessToken()
    if (!token) return

    void fetch(`${base}${path}`, {
      headers: { Authorization: `Bearer ${token}`, Accept: 'image/*' },
    })
      .then((r) => (r.ok ? r.blob() : null))
      .then((blob) => {
        if (blob) {
          objectUrl = URL.createObjectURL(blob)
          setSrc(objectUrl)
        }
      })

    return () => {
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [base, path, ev.id])

  return (
    <Space>
      {src ? (
        <a href={src} target="_blank" rel="noreferrer">
          <img src={src} alt={ev.imagen} style={{ maxHeight: 80, maxWidth: 120 }} />
        </a>
      ) : (
        <Text>{ev.imagen}</Text>
      )}
      <Button size="small" danger loading={deleting} onClick={onDelete}>
        Eliminar
      </Button>
    </Space>
  )
}
