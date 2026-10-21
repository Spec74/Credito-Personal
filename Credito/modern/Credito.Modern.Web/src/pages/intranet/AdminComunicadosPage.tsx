import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, DatePicker, Form, Input, Modal, Radio, Select, Space, Switch, Upload, message } from 'antd'
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
import { fetchOficinas } from '../../api/oficinas'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage } from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'
import { esCreditoAdministrador } from '../../utils/creditoOperacionPermisos'

type FormValues = {
  titulo: string
  cuerpo: string
  alcance: 'RED' | 'OFICINA'
  oficinaDestinoId?: number
  permanente: boolean
  fechaFin?: dayjs.Dayjs
  archivos?: { originFileObj?: File }[]
}

function destinoLabel(r: ComunicadoRow) {
  if (r.alcance === 'RED') return 'Todas las sedes'
  return r.oficinaNombre?.trim() || (r.oficinaId ? `Oficina ${r.oficinaId}` : 'Una sede')
}

export function AdminComunicadosPage() {
  const { session } = useAuth()
  const admin = esCreditoAdministrador(session?.roles ?? [])
  const qc = useQueryClient()
  const [form] = Form.useForm<FormValues>()
  const [error, setError] = useState<string | null>(null)
  const [editando, setEditando] = useState<ComunicadoRow | null>(null)
  const [adjuntos, setAdjuntos] = useState<ComunicadoAdjunto[]>([])
  const alcance = Form.useWatch('alcance', form)

  const oficinas = useQuery({
    queryKey: ['oficinas'],
    queryFn: fetchOficinas,
    enabled: admin,
  })

  const lista = useQuery({
    queryKey: ['comunicados', 'gestion'],
    queryFn: () => fetchComunicados(true, true),
  })

  const resetFormulario = () => {
    setEditando(null)
    setAdjuntos([])
    form.resetFields()
    form.setFieldsValue({
      alcance: admin ? 'RED' : 'OFICINA',
      permanente: true,
      oficinaDestinoId: session?.oficinaId,
    })
  }

  const publicar = useMutation({
    mutationFn: async (v: FormValues) => {
      const saved = await saveComunicado({
        comunicadoId: editando?.comunicadoId ?? null,
        titulo: v.titulo.trim(),
        cuerpo: v.cuerpo.trim(),
        alcance: v.alcance,
        oficinaDestinoId: v.alcance === 'OFICINA' ? v.oficinaDestinoId ?? session?.oficinaId ?? null : null,
        fechaIni: editando?.fechaIni ?? dayjs().toISOString(),
        fechaFin: v.permanente ? null : (v.fechaFin?.endOf('day').toISOString() ?? null),
      })
      const files = (v.archivos ?? [])
        .map((f) => f.originFileObj)
        .filter((f): f is File => !!f)
      for (const file of files) {
        await subirComunicadoAdjunto(saved.id, file)
      }
      return saved
    },
    onSuccess: () => {
      const eraEdicion = editando != null
      resetFormulario()
      setError(null)
      message.success(eraEdicion ? 'Aviso actualizado' : 'Aviso publicado')
      void qc.invalidateQueries({ queryKey: ['comunicados'] })
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'No se pudo guardar el aviso.'),
  })

  const cargarEdicion = async (row: ComunicadoRow) => {
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
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo abrir el aviso para editar.')
    }
  }

  const retirar = useMutation({
    mutationFn: bajaComunicado,
    onSuccess: () => {
      message.success('Aviso retirado')
      if (editando) resetFormulario()
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

  const puedeMutar = (row: ComunicadoRow) => admin || row.alcance !== 'RED'

  return (
    <CredixPage
      title={editando ? 'Editar aviso' : 'Publicar aviso'}
      subtitle="Elija si lo ven todas las sedes o una sola. Puede editarlo o retirarlo después."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/comunicados">Avisos</Link> },
        { title: 'Publicar' },
      ]}
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
          <Input maxLength={160} placeholder="Ej. Horario del sábado / Reglamento de caja" />
        </Form.Item>
        <Form.Item name="cuerpo" label="Aviso" rules={[{ required: true, message: 'Escriba el aviso' }]}>
          <Input.TextArea rows={4} maxLength={4000} placeholder="Texto que verá el personal" />
        </Form.Item>
        <Form.Item
          name="alcance"
          label="Dónde se ve"
          extra={admin ? 'Todas las sedes, o elija una (Huamanga, Pichari, Huanta, Andahuaylas).' : 'El encargado publica solo en su sede.'}
        >
          <Radio.Group>
            {admin ? <Radio value="RED">Todas las sedes</Radio> : null}
            <Radio value="OFICINA">{admin ? 'Una sede' : 'Solo esta sede'}</Radio>
          </Radio.Group>
        </Form.Item>
        {admin && alcance === 'OFICINA' ? (
          <Form.Item
            name="oficinaDestinoId"
            label="Sede"
            rules={[{ required: true, message: 'Elija la sede' }]}
          >
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
        <Form.Item
          name="permanente"
          label="Siempre visible"
          extra="Actívalo para reglamentos o archivos que no vencen."
          valuePropName="checked"
        >
          <Switch />
        </Form.Item>
        <Form.Item noStyle shouldUpdate={(a, b) => a.permanente !== b.permanente}>
          {() =>
            form.getFieldValue('permanente') ? null : (
              <Form.Item
                name="fechaFin"
                label="Hasta cuándo se muestra"
                rules={[{ required: true, message: 'Elija la fecha de fin' }]}
              >
                <DatePicker style={{ width: '100%', maxWidth: 280 }} />
              </Form.Item>
            )
          }
        </Form.Item>
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
          extra="Imagen hasta 2 MB (jpg, png, webp). PDF hasta 8 MB."
          valuePropName="fileList"
          getValueFromEvent={(e) => e?.fileList}
        >
          <Upload beforeUpload={() => false} multiple accept=".pdf,.jpg,.jpeg,.png,.webp">
            <Button icon={<UploadOutlined />}>Adjuntar</Button>
          </Upload>
        </Form.Item>
        <Space wrap>
          <Button type="primary" htmlType="submit" loading={publicar.isPending}>
            {editando ? 'Guardar cambios' : 'Publicar'}
          </Button>
          {editando ? (
            <Button onClick={resetFormulario}>Cancelar</Button>
          ) : null}
        </Space>
      </Form>

      <CredixDataTable
        rowKey="comunicadoId"
        style={{ marginTop: 24 }}
        dataSource={lista.data ?? []}
        pagination={false}
        columns={[
          { title: 'Título', dataIndex: 'titulo' },
          { title: 'Visible en', render: (_, r) => destinoLabel(r) },
          {
            title: 'Vigencia',
            render: (_, r) => (r.permanente ? 'Permanente' : formatFecha(r.fechaFin)),
          },
          {
            title: '',
            width: 180,
            render: (_, r) =>
              puedeMutar(r) ? (
                <Space wrap>
                  <Button size="small" onClick={() => void cargarEdicion(r)}>
                    Editar
                  </Button>
                  <Button
                    size="small"
                    danger
                    loading={retirar.isPending && retirar.variables === r.comunicadoId}
                    onClick={() => confirmarRetiro(r)}
                  >
                    Eliminar
                  </Button>
                </Space>
              ) : (
                <span>Solo lectura</span>
              ),
          },
        ]}
      />
    </CredixPage>
  )
}
