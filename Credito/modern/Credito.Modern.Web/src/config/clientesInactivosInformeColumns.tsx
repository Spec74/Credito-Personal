import type { ColumnsType } from 'antd/es/table'
import type { RptClientesInactivosRow } from '../types/api'

/** Columnas — mismo orden que <c>rptClienteInactivo.rdlc</c> / catálogo legacy. */
export function buildClientesInactivosInformeColumns(): ColumnsType<RptClientesInactivosRow> {
  return [
    { title: 'Persona', dataIndex: 'personaId', minWidth: 72, align: 'right' },
    { title: 'Agente', dataIndex: 'agente', ellipsis: true, minWidth: 120 },
    { title: 'Código', dataIndex: 'codigo', minWidth: 88 },
    { title: 'DNI', dataIndex: 'dni', minWidth: 100 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true, minWidth: 140 },
    { title: 'Dirección', dataIndex: 'direccion', ellipsis: true, minWidth: 160 },
    { title: 'Dir. ref.', dataIndex: 'direccionRef', ellipsis: true, minWidth: 100 },
    { title: 'Celular', dataIndex: 'celular', minWidth: 100 },
    { title: 'Calificación', dataIndex: 'calificacion', minWidth: 96 },
    { title: 'Dir. negocio', dataIndex: 'direccionNegocio', ellipsis: true, minWidth: 140 },
    { title: 'Dir. neg. ref.', dataIndex: 'direccionNegocioRef', ellipsis: true, minWidth: 100 },
  ]
}
