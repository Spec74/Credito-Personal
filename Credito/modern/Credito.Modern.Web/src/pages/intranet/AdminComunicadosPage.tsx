import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  DatePicker,
  Drawer,
  Form,
  Grid,
  Input,
  Modal,
  Radio,
  Select,
  Space,
  Switch,
  Upload,
  message,
  type UploadFile,
} from 'antd'
import { UploadOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import {
  bajaAdjunto,
  bajaComunicado,
  fetchComunicado,
  fetchComunicados,
  saveComunicado,
  subirComunicadoAdjunto,
  type ComunicadoAdjunto,
  type ComunicadoRow,
} from '../../api/intranet'
import { archivosDeUpload } from '../../api/intranetParse'
import { fetchOficinas } from '../../api/oficinas'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixPage } from '../../components/credix'
import {
  esCreditoAdministrador,
  puedeMutarAviso,
  puedePublicarAvisos,
} from '../../utils/creditoOperacionPermisos'
import { destinoAviso, vigenciaAviso } from './intranetLabels'

function validarAdjunto(file: File) {
  const nombre = file.name.toLowerCase()
  const esPdf = nombre.endsWith('.pdf')
  const esImg = /\.(jpe?g|png|webp)$/.test(nombre)
  if (!esPdf && !esImg) {
    message.error('Solo imagen (jpg, png, webp) o PDF.')
    return Upload.LIST_IGNORE
  }
  const maxMb = esPdf ? 8 : 2
  if (file.size > maxMb * 1024 * 1024) {
    message.error(esPdf ? 'El PDF no puede superar 8 MB.' : 'La imagen no puede superar 2 MB.')
    return Upload.LIST_IGNORE
  }
  return false
}

type FormValues = {
  titulo: string
  cuerpo: string
  alcance: 'RED' | 'OFICINA'
  oficinaDestinoId?: number
  permanente: boolean
  fechaFin?: dayjs.Dayjs
  archivos?: UploadFile[]
}

export function AdminComunicadosPage() {
  const { session } = useAuth()
  const roles = session?.roles ?? []
  const admin = esCreditoAdministrador(roles)
  const publica = puedePublicarAvisos(roles)
  const movil = !Grid.useBreakpoint().md
  const qc = useQueryClient()
  const [form] = Form.useForm<FormValues>()
  const [error, setError] = useState<string | null>(null)
  const [editando, setEditando] = useState<ComunicadoRow | null>(null)
  const [adjuntos, setAdjuntos] = useState<ComunicadoAdjunto[]>([])
  const [open, setOpen] = useState(false)
  const [busqueda, setBusqueda] = useState('')
  const alcance = Form.useWatch('alcance', form)
  const permanente = Form.useWatch('permanente', form)

  const oficinas = useQuery({
    queryKey: ['oficinas'],
    queryFn: fetchOficinas,
    enabled: admin,
  })

  const lista = useQuery({
    queryKey: ['comunicados', 'gestion'],
    queryFn: () => fetchComunicados(true, true),
    enabled: publica,
  })

  const visibles = useMemo(() => {
    const term = busqueda.trim().toLowerCase()
    return (lista.data ?? []).filter((r) => {
      if (!term) return true
      return `${r.titulo} ${r.cuerpo} ${r.oficinaNombre ?? ''}`.toLowerCase().includes(term)
    })
  }, [lista.data, busqueda])

  const resetFormulario = () => {
    setEditando(null)
    setAdjuntos([])
    setError(null)
    form.resetFields()
    form.setFieldsValue({
      alcance: admin ? 'RED' : 'OFICINA',
      permanente: true,
      oficinaDestinoId: session?.oficinaId,
    })
  }

  const cerrar = () => {
    setOpen(false)
    resetFormulario()
  }

  const publicar = useMutation({
    mutationFn: async (v: FormValues) => {
      const saved = await saveComunicado({
        comunicadoId: editando?.comunicadoId ?? null,
        titulo: v.titulo.trim(),
        cuerpo: v.cuerpo.trim(),
        alcance: admin ? v.alcance : 'OFICINA',
        oficinaDestinoId: admin && v.alcance === 'OFICINA' ? v.oficinaDestinoId ?? session?.oficinaId ?? null : session?.oficinaId ?? null,
        fechaIni: editando?.fechaIni ?? dayjs().toISOString(),
        fechaFin: v.permanente ? null : (v.fechaFin?.endOf('day').toISOString() ?? null),
      })
      const fallidos: string[] = []
      for (const file of archivosDeUpload(v.archivos)) {
        try {
          await subirComunicadoAdjunto(saved.id, file)
        } catch {
          fallidos.push(file.name)
        }
      }
      return { id: saved.id, fallidos }
    },
    onSuccess: (r) => {
      const eraEdicion = editando != null
      cerrar()
      if (r.fallidos.length === 1) {
        message.warning(`Aviso publicado. No se pudo adjuntar “${r.fallidos[0]}”. Edítelo y vuelva a adjuntar.`)
      } else if (r.fallidos.length > 1) {
        message.warning(`Aviso publicado. No se pudieron adjuntar ${r.fallidos.length} archivos.`)
      } else {
        message.success(eraEdicion ? 'Aviso actualizado' : 'Aviso publicado')
      }
      void qc.invalidateQueries({ queryKey: ['comunicados'] })
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
    },
    onError: (e) => {
      void qc.invalidateQueries({ queryKey: ['comunicados'] })
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
      if (e instanceof ApiError) {
        setError(e.message)
        return
      }
      setError(
        e instanceof TypeError
          ? 'La conexión se cortó al publicar. Si el aviso aparece en la lista, ya quedó guardado; el archivo hay que adjuntarlo al editar.'
          : 'No se pudo guardar el aviso.',
      )
    },
  })

  const cargarEdicion = async (row: ComunicadoRow) => {
    if (!puedeMutarAviso(roles, row.alcance)) return
    setError(null)
    try {
      const detalle = await fetchComunicado(row.comunicadoId)
      setEditando(detalle.cabecera)
      setAdjuntos(detalle.adjuntos)
      form.setFieldsValue({
        titulo: detalle.cabecera.titulo,
        cuerpo: detalle.cabecera.cuerpo,
        alcance: detalle.cabecera.alcance === 'RED' ? 'RED' : 'OFICINA',
        oficinaDestinoId: detalle.cabecera.oficinaId ?? session?.oficinaId,
        permanente: detalle.cabecera.permanente,
        fechaFin: detalle.cabecera.fechaFin ? dayjs(detalle.cabecera.fechaFin) : undefined,
        archivos: [],
      })
      setOpen(true)
    } catch (e) {
      message.error(e instanceof ApiError ? e.message : 'No se pudo abrir el aviso.')
    }
  }

  const retirar = useMutation({
    mutationFn: bajaComunicado,
    onSuccess: () => {
      message.success('Aviso retirado')
      if (editando) cerrar()
      void qc.invalidateQueries({ queryKey: ['comunicados'] })
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
    },
    onError: (e) => message.error(e instanceof ApiError ? e.message : 'No se pudo retirar.'),
  })

  const quitarArchivo = useMutation({
    mutationFn: bajaAdjunto,
    onSuccess: (_, id) => {
      setAdjuntos((prev) => prev.filter((a) => a.adjuntoId !== id))
      void qc.invalidateQueries({ queryKey: ['comunicados'] })
    },
    onError: (e) => message.error(e instanceof ApiError ? e.message : 'No se pudo quitar el archivo.'),
  })

  const confirmarRetiro = (row: ComunicadoRow) => {
    Modal.confirm({
      title: '¿Retirar este aviso?',
      content: `Dejará de verse en la campana y en la bandeja: “${row.titulo}”.`,
      okText: 'Retirar',
      okButtonProps: { danger: true },
      cancelText: 'Cancelar',
      onOk: () => retirar.mutateAsync(row.comunicadoId),
    })
  }

  const puedeMutar = (row: ComunicadoRow) => puedeMutarAviso(roles, row.alcance)

  const abrirNuevo = () => {
    resetFormulario()
    setOpen(true)
  }

  return (
    <CredixPage
      title="Publicar aviso"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/comunicados">Avisos</Link> },
        { title: 'Publicar' },
      ]}
      actions={
        publica ? (
          <Button type="primary" onClick={abrirNuevo}>
            Nuevo aviso
          </Button>
        ) : undefined
      }
      stats={[
        { value: lista.data?.length ?? 0, label: 'Publicados' },
        { value: (lista.data ?? []).filter((r) => r.alcance === 'RED').length, label: 'Toda la red' },
      ]}
    >
      <div className="credix-inbox-toolbar">
        <Input
          allowClear
          placeholder="Buscar publicado"
          value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
          aria-label="Buscar publicado"
        />
      </div>

      {lista.isLoading ? null : visibles.length === 0 ? (
        <div className="credix-inbox__empty">
          <p>Aún no hay avisos. Publique el primero.</p>
        </div>
      ) : (
        <ul className="credix-gestion-list">
          {visibles.map((r) => (
            <li key={r.comunicadoId} className="credix-gestion-card">
              <div className="credix-gestion-card__body">
                <button type="button" onClick={() => void cargarEdicion(r)} disabled={!puedeMutar(r)}>
                  <strong>{r.titulo}</strong>
                  <div className="credix-chip-row">
                    <span className="credix-chip credix-chip--brand">{destinoAviso(r)}</span>
                    <span className={`credix-chip${r.permanente ? ' credix-chip--ok' : ''}`}>
                      {vigenciaAviso(r)}
                    </span>
                    {r.adjuntos > 0 ? <span className="credix-chip">{r.adjuntos} archivo{r.adjuntos === 1 ? '' : 's'}</span> : null}
                    {!puedeMutar(r) ? <span className="credix-chip">Solo lectura</span> : null}
                  </div>
                </button>
              </div>
              {puedeMutar(r) ? (
                <div className="credix-gestion-card__actions">
                  <Button onClick={() => void cargarEdicion(r)}>Editar</Button>
                  <Button
                    danger
                    loading={retirar.isPending && retirar.variables === r.comunicadoId}
                    onClick={() => confirmarRetiro(r)}
                  >
                    Retirar
                  </Button>
                </div>
              ) : null}
            </li>
          ))}
        </ul>
      )}

      <Drawer
        title={editando ? 'Editar aviso' : 'Nuevo aviso'}
        open={open}
        onClose={cerrar}
        width={movil ? '100%' : 480}
        destroyOnHidden
        extra={
          <Space>
            <Button onClick={cerrar}>Cancelar</Button>
            <Button type="primary" loading={publicar.isPending} onClick={() => form.submit()}>
              {editando ? 'Guardar' : 'Publicar'}
            </Button>
          </Space>
        }
      >
        {error ? <p className="credix-field-error">{error}</p> : null}
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            alcance: admin ? 'RED' : 'OFICINA',
            permanente: true,
            oficinaDestinoId: session?.oficinaId,
          }}
          onFinish={(v) => publicar.mutate(v)}
        >
          <Form.Item name="titulo" label="Título" rules={[{ required: true, message: 'Escriba el título' }]}>
            <Input maxLength={160} placeholder="Ej. Horario del sábado" />
          </Form.Item>
          <Form.Item name="cuerpo" label="Texto" rules={[{ required: true, message: 'Escriba el aviso' }]}>
            <Input.TextArea rows={6} maxLength={4000} placeholder="Lo que verá el personal" />
          </Form.Item>
          <Form.Item name="alcance" label="Quién lo ve">
            <Radio.Group>
              {admin ? <Radio value="RED">Todas las sedes</Radio> : null}
              <Radio value="OFICINA">{admin ? 'Una sede' : 'Solo esta sede'}</Radio>
            </Radio.Group>
          </Form.Item>
          {admin && alcance === 'OFICINA' ? (
            <Form.Item name="oficinaDestinoId" label="Sede" rules={[{ required: true, message: 'Elija la sede' }]}>
              <Select
                placeholder="Elija la oficina"
                options={(oficinas.data ?? [])
                  .filter((o) => o.estado)
                  .map((o) => ({
                    value: o.oficinaId,
                    label: o.denominacion?.trim() || `Oficina ${o.oficinaId}`,
                  }))}
              />
            </Form.Item>
          ) : null}
          <Form.Item name="permanente" label="Siempre visible" valuePropName="checked">
            <Switch />
          </Form.Item>
          {permanente ? null : (
            <Form.Item
              name="fechaFin"
              label="Hasta cuándo"
              rules={[{ required: true, message: 'Elija la fecha de fin' }]}
            >
              <DatePicker style={{ width: '100%' }} />
            </Form.Item>
          )}
          {adjuntos.length > 0 ? (
            <div className="credix-comunicado-adjuntos-edit">
              <p>Archivos actuales</p>
              <ul>
                {adjuntos.map((a) => (
                  <li key={a.adjuntoId}>
                    <span>{a.nombreOriginal}</span>
                    <Button
                      type="link"
                      danger
                      size="small"
                      loading={quitarArchivo.isPending && quitarArchivo.variables === a.adjuntoId}
                      onClick={() => quitarArchivo.mutate(a.adjuntoId)}
                    >
                      Quitar
                    </Button>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
          <Form.Item
            name="archivos"
            label={editando ? 'Agregar PDF o imagen' : 'PDF o imagen'}
            extra="Imagen hasta 2 MB. PDF hasta 8 MB."
            valuePropName="fileList"
            getValueFromEvent={(e: { fileList?: UploadFile[] } | UploadFile[]) =>
              Array.isArray(e) ? e : (e?.fileList ?? [])
            }
          >
            <Upload
              multiple
              accept=".pdf,.jpg,.jpeg,.png,.webp"
              beforeUpload={(file) => validarAdjunto(file)}
            >
              <Button icon={<UploadOutlined />}>Adjuntar</Button>
            </Upload>
          </Form.Item>
        </Form>
      </Drawer>
    </CredixPage>
  )
}
