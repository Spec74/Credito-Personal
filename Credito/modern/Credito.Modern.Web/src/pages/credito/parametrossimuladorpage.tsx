import { useEffect, useMemo } from 'react'

import { Link } from 'react-router-dom'

import { useMutation, useQuery } from '@tanstack/react-query'

import { Button, Form, Input, Spin, message } from 'antd'

import {

  actualizarParametrosSimulador,

  fetchParametrosSimulador,

} from '../../api/parametrosSimulador'

import { ApiError } from '../../api/errors'

import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'



export function ParametrosSimuladorPage() {

  const [form] = Form.useForm<{ factorVariable: string; factorFijo: string }>()



  const query = useQuery({

    queryKey: ['parametros-simulador'],

    queryFn: fetchParametrosSimulador,

    staleTime: creditoStaleTime.master,

  })



  const guardar = useMutation({

    mutationFn: actualizarParametrosSimulador,

    onSuccess: () => {

      message.success('Parámetros guardados')

      void query.refetch()

    },

    onError: (e: unknown) =>

      message.error(e instanceof ApiError ? e.message : 'Error al guardar'),

  })



  useEffect(() => {

    if (!query.data) return

    form.setFieldsValue({

      factorVariable: query.data.factorVariable,

      factorFijo: query.data.factorFijo,

    })

  }, [query.data, form])

  const stats = useMemo((): CredixStatItem[] => [
    { value: query.data ? 2 : 0, label: 'Parámetros' },
    { value: query.isLoading ? 'Cargando' : query.data ? 'Listo' : '—', label: 'Estado' },
  ], [query.data, query.isLoading])



  return (

    <CredixPage

      title="Parámetros del simulador"

      subtitle="Factores de compensación de cuota para el simulador de crédito."

      stats={stats}

      breadcrumb={[

        { title: <Link to="/inicio">Inicio</Link> },

        { title: <Link to="/credito">Crédito</Link> },

        { title: 'Parámetros simulador' },

      ]}

    >

      <CredixPanel title="Factores">

        <Spin spinning={query.isLoading}>

          <Form

            form={form}

            layout="vertical"

            onFinish={(v) => guardar.mutate(v)}

            style={{ maxWidth: 420 }}

          >

            <Form.Item

              name="factorVariable"

              label="F. compensación cuota variable"

              rules={[{ required: true, message: 'Obligatorio' }]}

            >

              <Input inputMode="decimal" />

            </Form.Item>

            <Form.Item

              name="factorFijo"

              label="F. compensación cuota fijo"

              rules={[{ required: true, message: 'Obligatorio' }]}

            >

              <Input inputMode="decimal" />

            </Form.Item>

            <Button type="primary" htmlType="submit" loading={guardar.isPending}>

              Guardar parámetros

            </Button>

          </Form>

        </Spin>

      </CredixPanel>

    </CredixPage>

  )

}

