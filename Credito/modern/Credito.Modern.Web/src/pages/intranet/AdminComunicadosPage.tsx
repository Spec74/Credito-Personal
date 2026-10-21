import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, DatePicker, Form, Input, Radio, Switch, Upload } from 'antd'
import { UploadOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import {
  bajaComunicado,
  fetchComunicados,
  saveComunicado,
  subirComunicadoAdjunto,
} from '../../api/intranet'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage } from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'
import { esCreditoAdministrador } from '../../utils/creditoOperacionPermisos'

type FormValues = {
  titulo: string
  cuerpo: string
  alcance: 'RED' | 'OFICINA'
  permanente: boolean
  fechaFin?: dayjs.Dayjs
  archivos?: { originFileObj?: File }[]
}

export function AdminComunicadosPage() {
  const { session } = useAuth()
  const admin = esCreditoAdministrador(session?.roles ?? [])
  const qc = useQueryClient()
  const [form] = Form.useForm<FormValues>()
  const [error, setError] = useState<string | null>(null)

  const lista = useQuery({
    queryKey: ['comunicados', true],
    queryFn: () => fetchComunicados(true),
  })

  const publicar = useMutation({
    mutationFn: async (v: FormValues) => {
      const saved = await saveComunicado({
        titulo: v.titulo.trim(),
        cuerpo: v.cuerpo.trim(),
        alcance: v.alcance,
        fechaIni: dayjs().toISOString(),
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
      form.resetFields()
      setError(null)
      void qc.invalidateQueries({ queryKey: ['comunicados'] })
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : 'No se pudo publicar.'),
  })

  const retirar = useMutation({
    mutationFn: bajaComunicado,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['comunicados'] }),
  })

  return (
    <CredixPage
      title="Publicar aviso"
      subtitle="Lo verán en la campana y en Avisos. Si lo deja siempre visible, queda como reglamento."
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
        initialValues={{ alcance: admin ? 'RED' : 'OFICINA', permanente: true }}
        onFinish={(v) => publicar.mutate(v)}
      >
        <Form.Item name="titulo" label="Título" rules={[{ required: true, message: 'Escriba el título' }]}>
          <Input maxLength={160} placeholder="Ej. Horario del sábado / Reglamento de caja" />
        </Form.Item>
        <Form.Item name="cuerpo" label="Aviso" rules={[{ required: true, message: 'Escriba el aviso' }]}>
          <Input.TextArea rows={4} maxLength={4000} placeholder="Texto que verá todo el personal" />
        </Form.Item>
        <Form.Item name="alcance" label="Quién lo ve">
          <Radio.Group>
            {admin ? <Radio value="RED">Todas las sedes</Radio> : null}
            <Radio value="OFICINA">Solo esta sede</Radio>
          </Radio.Group>
        </Form.Item>
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
        <Form.Item
          name="archivos"
          label="PDF o imagen"
          extra="Imagen hasta 2 MB (jpg, png, webp). PDF hasta 8 MB."
          valuePropName="fileList"
          getValueFromEvent={(e) => e?.fileList}
        >
          <Upload beforeUpload={() => false} multiple accept=".pdf,.jpg,.jpeg,.png,.webp">
            <Button icon={<UploadOutlined />}>Adjuntar</Button>
          </Upload>
        </Form.Item>
        <Button type="primary" htmlType="submit" loading={publicar.isPending}>
          Publicar
        </Button>
      </Form>

      <CredixDataTable
        rowKey="comunicadoId"
        style={{ marginTop: 24 }}
        dataSource={lista.data ?? []}
        pagination={false}
        columns={[
          { title: 'Título', dataIndex: 'titulo' },
          {
            title: 'Vigencia',
            render: (_, r) => (r.permanente ? 'Permanente' : formatFecha(r.fechaFin)),
          },
          {
            title: 'Alcance',
            dataIndex: 'alcance',
            render: (v: string) => (v === 'RED' ? 'Red' : 'Sede'),
          },
          {
            title: '',
            width: 100,
            render: (_, r) => (
              <Button
                size="small"
                danger
                onClick={() => retirar.mutate(r.comunicadoId)}
                loading={retirar.isPending && retirar.variables === r.comunicadoId}
              >
                Retirar
              </Button>
            ),
          },
        ]}
      />
    </CredixPage>
  )
}
