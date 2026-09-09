import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { FilePdfOutlined, PlusOutlined, WhatsAppOutlined } from '@ant-design/icons'
import { Alert, Button, Input, Space, Typography, message } from 'antd'
import {
  fetchCreditoContexto,
  fetchCreditosGrillaPersona,
  fetchPersonaCreditoFicha,
  fetchPrendas,
  type PrendaItem,
} from '../../api/creditoGestion'
import {
  crearSolicitudPrendaria,
  downloadActaEntregaPrendarioPdf,
  downloadContratoPrendarioPdf,
  guardarBienesPrendario,
} from '../../api/prendario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { PrendasEditor } from '../../components/credito/PrendasEditor'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { prendaAItem, prendaVacia, prendasValidas, totalTasacion } from '../../utils/prendas'
import { abrirWhatsAppPrendario } from '../../utils/prendarioWhatsapp'

const { Paragraph, Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : e instanceof Error ? e.message : 'Error desconocido'
}

export function CreditoPrendarioGestionPage() {
  const navigate = useNavigate()
  const { personaId: personaParam } = useParams()
  const [searchParams] = useSearchParams()
  const { session } = useAuth()
  const queryClient = useQueryClient()
  const oficinaId = session?.oficinaId ?? 0
  const personaId = Number(personaParam)
  const creditoIdUrl = Number(searchParams.get('creditoId') ?? 0)

  const [prendas, setPrendas] = useState<PrendaItem[]>([prendaVacia()])
  const [fechaRemate, setFechaRemate] = useState('')

  const ficha = useQuery({
    queryKey: ['persona-credito-ficha', oficinaId, personaId],
    queryFn: () => fetchPersonaCreditoFicha(oficinaId, personaId),
    enabled: oficinaId > 0 && personaId > 0,
  })

  const grilla = useQuery({
    queryKey: ['creditos-grilla-persona', oficinaId, personaId],
    queryFn: () =>
      fetchCreditosGrillaPersona({ oficinaId, personaId, grupoActivo: true, page: 1, pageSize: 50 }),
    enabled: oficinaId > 0 && personaId > 0,
  })

  const creditoId = useMemo(() => {
    if (creditoIdUrl > 0) {
      return creditoIdUrl
    }
    const items = grilla.data?.items ?? []
    return items[0]?.creditoId ?? ficha.data?.solicitudCreditoId ?? 0
  }, [creditoIdUrl, grilla.data, ficha.data])

  const contexto = useQuery({
    queryKey: ['credito-contexto', creditoId],
    queryFn: () => fetchCreditoContexto(creditoId),
    enabled: creditoId > 0,
  })

  const bienes = useQuery({
    queryKey: ['prendas', oficinaId, creditoId],
    queryFn: () => fetchPrendas(oficinaId, creditoId),
    enabled: oficinaId > 0 && creditoId > 0,
  })

  useEffect(() => {
    setFechaRemate(contexto.data?.fechaRemate?.slice(0, 10) ?? '')
    const guardadas = bienes.data ?? []
    setPrendas(guardadas.length > 0 ? guardadas.map(prendaAItem) : [prendaVacia()])
  }, [contexto.data, bienes.data])

  const crearSolicitud = useMutation({
    mutationFn: () => crearSolicitudPrendaria({ oficinaId, personaId }),
    onSuccess: (r) => {
      message.success(`Solicitud #${r.solicitudCreditoId} lista`)
      navigate(`/credito/prendario/gestionar/${personaId}?creditoId=${r.solicitudCreditoId}`, { replace: true })
      void queryClient.invalidateQueries({ queryKey: ['creditos-grilla-persona', oficinaId, personaId] })
      void queryClient.invalidateQueries({ queryKey: ['persona-credito-ficha', oficinaId, personaId] })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const guardar = useMutation({
    mutationFn: () =>
      guardarBienesPrendario({
        oficinaId,
        creditoId,
        prendas: prendasValidas(prendas),
        fechaRemate: fechaRemate || null,
      }),
    onSuccess: () => {
      message.success('Bienes en custodia guardados')
      void queryClient.invalidateQueries({ queryKey: ['prendas', oficinaId, creditoId] })
      void queryClient.invalidateQueries({ queryKey: ['credito-contexto', creditoId] })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const imprimirContrato = useMutation({
    mutationFn: () =>
      downloadContratoPrendarioPdf(
        oficinaId,
        creditoId,
        contexto.data?.numeroContratoPrendario || String(creditoId),
      ),
    onError: (e) => message.error(errMsg(e)),
  })

  const imprimirActa = useMutation({
    mutationFn: () =>
      downloadActaEntregaPrendarioPdf(
        oficinaId,
        creditoId,
        contexto.data?.numeroContratoPrendario || String(creditoId),
      ),
    onError: (e) => message.error(errMsg(e)),
  })

  const stats: CredixStatItem[] = []
  if (ficha.data) {
    stats.push({ label: 'Cliente', value: ficha.data.nombreCompleto })
    stats.push({ label: 'DNI', value: ficha.data.numeroDocumento })
  }
  if (contexto.data?.numeroContratoPrendario) {
    stats.push({ label: 'Contrato', value: contexto.data.numeroContratoPrendario })
  }
  const tasacion = totalTasacion(prendas)
  if (tasacion > 0) {
    stats.push({ label: 'Tasación', value: formatMoney(tasacion), tone: 'green' })
  }

  if (personaId < 1) {
    return (
      <CredixPage title="Gestión prendaria" breadcrumb={[{ title: <Link to="/credito/prendario">Prendario</Link> }]}>
        <Alert type="error" message="Persona no indicada." />
      </CredixPage>
    )
  }

  return (
    <CredixPage
      title="Gestión prendaria"
      subtitle="Bienes en custodia, contrato y acta de entrega. El monto del préstamo se define en el simulador."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: <Link to="/credito/prendario">Prendario</Link> },
        { title: ficha.data?.nombreCompleto ?? 'Gestión' },
      ]}
      stats={stats}
      actions={
        <Space wrap>
          <Button icon={<PlusOutlined />} onClick={() => navigate('/credito/prendario/nuevo')}>
            Nuevo
          </Button>
          {creditoId > 0 ? (
            <Button
              onClick={() =>
                navigate(`/credito/simulador?personaId=${personaId}&solicitudCreditoId=${creditoId}&productoId=2`)
              }
            >
              Simulador
            </Button>
          ) : null}
        </Space>
      }
    >
      {!creditoId ? (
        <Alert
          type="info"
          showIcon
          message="Este cliente no tiene una solicitud prendaria."
          action={
            <Button type="primary" loading={crearSolicitud.isPending} onClick={() => crearSolicitud.mutate()}>
              Crear solicitud
            </Button>
          }
        />
      ) : (
        <CredixPanel
          title={`Crédito #${creditoId}`}
          extra={
            <Space wrap>
              <Button
                icon={<FilePdfOutlined />}
                loading={imprimirContrato.isPending}
                disabled={prendasValidas(prendas).length === 0}
                onClick={() => imprimirContrato.mutate()}
              >
                Contrato
              </Button>
              <Button
                icon={<FilePdfOutlined />}
                loading={imprimirActa.isPending}
                disabled={prendasValidas(prendas).length === 0}
                onClick={() => imprimirActa.mutate()}
              >
                Acta de entrega
              </Button>
              <Button
                icon={<WhatsAppOutlined />}
                onClick={() => {
                  const ok = abrirWhatsAppPrendario(
                    contexto.data?.personaCelular,
                    ficha.data?.nombreCompleto ?? '',
                    creditoId,
                  )
                  if (!ok) {
                    message.warning('Este cliente no tiene celular registrado')
                  }
                }}
              >
                WhatsApp
              </Button>
            </Space>
          }
        >
          <Paragraph type="secondary">
            Estado {contexto.data ? '' : '…'}
            {contexto.data ? (
              <>
                . Vence {formatFecha(contexto.data.fechaVencimiento)}. Contrato{' '}
                {contexto.data.numeroContratoPrendario ?? '(se asigna al guardar bienes)'}.
              </>
            ) : null}
          </Paragraph>
          <Paragraph type="secondary">Fecha de remate (vacía = vencimiento + 30 días)</Paragraph>
          <Input
            type="date"
            style={{ maxWidth: 220, marginBottom: 12 }}
            value={fechaRemate}
            onChange={(e) => setFechaRemate(e.target.value)}
          />
          <PrendasEditor value={prendas} onChange={setPrendas} />
          <Button
            type="primary"
            style={{ marginTop: 12 }}
            disabled={prendasValidas(prendas).length === 0}
            loading={guardar.isPending}
            onClick={() => guardar.mutate()}
          >
            Guardar bienes
          </Button>
        </CredixPanel>
      )}
    </CredixPage>
  )
}
