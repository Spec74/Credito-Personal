import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, Input, Modal, Space, Statistic, Tag, Typography, message } from 'antd'
import {
  FileAddOutlined,
  FilePdfOutlined,
  LockOutlined,
  PlayCircleOutlined,
  StopOutlined,
  UnlockOutlined,
  UserDeleteOutlined,
  UserOutlined,
} from '@ant-design/icons'
import { Link, useNavigate } from 'react-router-dom'
import { crearSolicitudCredito } from '../../api/creditoPlanes'
import { toggleClienteBloqueado } from '../../api/clientes'
import { depurarPersonaCredito, fetchPersonaCreditoFicha } from '../../api/creditoGestion'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { cajaConfirm } from '../caja/cajaConfirm'
import {
  puedeCrearSolicitudCreditoUi,
  puedeDepurarClienteCredito,
  puedeEditarTopeCreditoUi,
} from '../../utils/creditoOperacionPermisos'
import {
  calificacionTagColor,
  sbsRiesgoClassName,
} from './personaCreditoSbs'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'

const { Text, Title } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

type Props = {
  oficinaId: number
  personaId: number
  clienteLabel?: string
  onSolicitudCreada?: (solicitudCreditoId: number) => void
  onDepurado?: () => void
}

export function CreditoPersonaCabecera({
  oficinaId,
  personaId,
  clienteLabel,
  onSolicitudCreada,
  onDepurado,
}: Props) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { session } = useAuth()
  const roles = session?.roles ?? []
  const [modalDepurar, setModalDepurar] = useState(false)
  const [obsDepurar, setObsDepurar] = useState('')

  const fichaQuery = useQuery({
    queryKey: ['persona-credito-ficha', oficinaId, personaId],
    queryFn: () => fetchPersonaCreditoFicha(oficinaId, personaId),
    enabled: oficinaId > 0 && personaId > 0,
    staleTime: creditoStaleTime.ficha,
  })

  const crearSolicitud = useMutation({
    mutationFn: () =>
      crearSolicitudCredito({
        oficinaId,
        personaId,
      }),
    onSuccess: (r) => {
      message.success(`Solicitud #${r.solicitudCreditoId} creada (estado CRE)`)
      void queryClient.invalidateQueries({
        queryKey: ['persona-credito-ficha', oficinaId, personaId],
      })
      void queryClient.invalidateQueries({
        queryKey: ['creditos-grilla-persona', oficinaId, personaId],
      })
      onSolicitudCreada?.(r.solicitudCreditoId)
      navigate(
        `/credito/simulador?personaId=${personaId}&solicitudCreditoId=${r.solicitudCreditoId}`,
      )
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const depurar = useMutation({
    mutationFn: () =>
      depurarPersonaCredito({
        oficinaId,
        personaId,
        observacion: obsDepurar.trim().toUpperCase(),
      }),
    onSuccess: () => {
      message.success('Cliente depurado')
      setModalDepurar(false)
      setObsDepurar('')
      void queryClient.invalidateQueries({
        queryKey: ['persona-credito-ficha', oficinaId, personaId],
      })
      void queryClient.invalidateQueries({
        queryKey: ['creditos-grilla-persona', oficinaId, personaId],
      })
      onDepurado?.()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const f = fichaQuery.data
  const depurado = Boolean(f?.depuradoDescripcion)
  const puedeDepurar = f ? puedeDepurarClienteCredito(roles, depurado) : false
  const puedeBloquear = f ? puedeEditarTopeCreditoUi(roles) : false
  const puedeCrear = f
    ? puedeCrearSolicitudCreditoUi(roles, {
        bloqueado: f.bloqueado,
        depuradoDescripcion: f.depuradoDescripcion,
        puedeCrearSolicitud: f.puedeCrearSolicitud,
      })
    : false

  const bloquearCliente = useMutation({
    mutationFn: () => toggleClienteBloqueado(personaId),
    onSuccess: (bloqueado) => {
      message.success(bloqueado ? 'Cliente bloqueado' : 'Cliente desbloqueado')
      void queryClient.invalidateQueries({
        queryKey: ['persona-credito-ficha', oficinaId, personaId],
      })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  if (fichaQuery.isLoading) {
    return (
      <div className="credito-persona-cabecera credito-persona-cabecera--loading">
        <Text type="secondary">Cargando ficha del cliente…</Text>
      </div>
    )
  }

  if (fichaQuery.isError || !f) {
    return (
      <Alert
        type="error"
        showIcon
        message="No se pudo cargar la ficha del cliente"
        description={fichaQuery.error instanceof ApiError ? fichaQuery.error.message : undefined}
      />
    )
  }

  const titulo =
    clienteLabel?.trim() ||
    `${f.numeroDocumento} ${f.nombreCompleto}${f.codigo ? ` [${f.codigo}]` : ''}`

  const irSimulador = (solicitudId?: number | null) => {
    const q = new URLSearchParams({ personaId: String(personaId) })
    if (solicitudId != null && solicitudId > 0) {
      q.set('solicitudCreditoId', String(solicitudId))
    }
    navigate(`/credito/simulador?${q}`)
  }

  const solicitarCrearSolicitud = () => {
    cajaConfirm({
      title: 'Crear solicitud de crédito',
      content: '¿Desea crear la solicitud de crédito para este cliente? (estado CRE, paridad MVC).',
      onOk: () => crearSolicitud.mutateAsync(),
    })
  }

  return (
    <header className="credito-persona-cabecera credito-persona-cabecera--profile">
      <div className="credito-persona-cabecera__profile-grid">
        <div className="credito-persona-cabecera__avatar" aria-hidden>
          <UserOutlined />
          <span className="credito-persona-cabecera__avatar-caption">Cliente</span>
        </div>
        <div className="credito-persona-cabecera__main">
        <div className="credito-persona-cabecera__identity">
          <Title level={4} className="credito-persona-cabecera__nombre">
            <Link to={`/informes/reporte-cliente?personaId=${personaId}`}>{titulo}</Link>
          </Title>
          <Space wrap size={[6, 6]}>
            <Tag>{f.estadoCliente}</Tag>
            {f.bloqueado ? <Tag color="error">Bloqueado</Tag> : null}
            <Tag color={calificacionTagColor(f.calificacion)}>
              Calificación: {f.calificacionLabel}
              {f.calificacion ? ` (${f.calificacion})` : ''}
            </Tag>
            {f.clasificacionRiesgoSbsLabel ? (
              <span
                className={sbsRiesgoClassName(f.clasificacionRiesgoSbsCodigo)}
                title={f.clasificacionRiesgoSbsObs ?? undefined}
              >
                SBS: {f.clasificacionRiesgoSbsLabel}
              </span>
            ) : null}
            {f.solicitudCreditoId ? (
              <Tag color="gold">Solicitud CRE #{f.solicitudCreditoId}</Tag>
            ) : null}
          </Space>
        </div>

        <Space wrap className="credito-persona-cabecera__actions">
          {f.solicitudCreditoId ? (
            <Button
              type="primary"
              icon={<PlayCircleOutlined />}
              onClick={() => irSimulador(f.solicitudCreditoId)}
            >
              Continuar solicitud
            </Button>
          ) : puedeCrear ? (
            <Button
              type="primary"
              icon={<FileAddOutlined />}
              loading={crearSolicitud.isPending}
              onClick={solicitarCrearSolicitud}
            >
              Crear solicitud de crédito
            </Button>
          ) : null}
          {puedeDepurar ? (
            <Button
              icon={<UserDeleteOutlined />}
              onClick={() => {
                setObsDepurar('')
                setModalDepurar(true)
              }}
            >
              Depurar cliente
            </Button>
          ) : null}
          {puedeBloquear ? (
            <Button
              danger={!f.bloqueado}
              icon={f.bloqueado ? <UnlockOutlined /> : <LockOutlined />}
              loading={bloquearCliente.isPending}
              onClick={() => {
                cajaConfirm({
                  title: f.bloqueado ? 'Desbloquear cliente' : 'Bloquear cliente',
                  content: f.bloqueado
                    ? '¿Desea desbloquear este cliente? Volverá a estar disponible para operaciones.'
                    : '¿Desea bloquear este cliente? Se impedirá crear nuevas solicitudes de crédito.',
                  onOk: () => bloquearCliente.mutateAsync(),
                })
              }}
            >
              {f.bloqueado ? 'Desbloquear cliente' : 'Bloquear cliente'}
            </Button>
          ) : null}
          <Link to={`/informes/reporte-cliente?personaId=${personaId}`}>
            <Button icon={<FilePdfOutlined />}>Ficha cliente</Button>
          </Link>
          <Link to={`/credito/simulador?personaId=${personaId}`}>
            <Button>Simulador</Button>
          </Link>
        </Space>
      </div>
      </div>

      {f.depuradoDescripcion ? (
        <Alert
          type="error"
          showIcon
          icon={<StopOutlined />}
          className="credito-persona-cabecera__depurado"
          message="Cliente depurado"
          description={f.depuradoDescripcion}
        />
      ) : null}

      <ul className="credito-persona-cabecera__info-strip">
        <li className="credito-persona-cabecera__info-item">
          <strong>{f.totalCreditos}</strong>
          <small>Total créditos</small>
        </li>
        <li className="credito-persona-cabecera__info-item">
          <strong>{f.creditosPendientes}</strong>
          <small>Créditos pendientes</small>
        </li>
        <li className="credito-persona-cabecera__info-item credito-persona-cabecera__info-item--calif">
          <strong>{f.calificacion || '—'}</strong>
          <small>Calificación ({f.calificacionLabel})</small>
        </li>
        <li className="credito-persona-cabecera__info-item credito-persona-cabecera__info-item--tope">
          <Statistic
            title="Tope crédito"
            prefix="S/."
            precision={2}
            value={f.topeCredito}
            className="credito-persona-cabecera__kpi-tope"
          />
        </li>
      </ul>

      <Modal
        title="Depurar cliente"
        open={modalDepurar}
        onCancel={() => setModalDepurar(false)}
        onOk={() => {
          if (!obsDepurar.trim()) {
            message.warning('Indique el motivo de depuración')
            return
          }
          cajaConfirm({
            title: 'Confirmar depuración',
            content:
              '¿Depurar este cliente? No podrá crear solicitudes de crédito (paridad MVC).',
            onOk: () => depurar.mutateAsync(),
          })
        }}
        confirmLoading={depurar.isPending}
        okText="Depurar"
        okButtonProps={{ danger: true }}
      >
        <Typography.Paragraph type="secondary">
          Paridad <strong>btnDepurarCliente</strong> / <code>DepurarCredito</code> del sistema
          clásico. El motivo se guardará en mayúsculas.
        </Typography.Paragraph>
        <Input.TextArea
          rows={3}
          placeholder="Motivo de depuración (obligatorio)"
          value={obsDepurar}
          onChange={(e) => setObsDepurar(e.target.value)}
        />
      </Modal>
    </header>
  )
}
