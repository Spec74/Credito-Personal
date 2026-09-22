import type { ColumnsType } from 'antd/es/table'
import type { CentralRiesgoGenerarRow } from '../api/creditoPlanes'

/** Columnas — mismo detalle que catálogo CentralRiesgoGenerar (nombres separados). */
export function buildCentralRiesgoInformeColumns(): ColumnsType<CentralRiesgoGenerarRow> {
  return [
    { title: 'Año', dataIndex: 'anio', width: 60 },
    { title: 'Mes', dataIndex: 'mes', width: 50 },
    { title: 'Crédito', dataIndex: 'creditoId', width: 75, fixed: 'left' },
    { title: 'Periodo', dataIndex: 'periodo', width: 80 },
    { title: 'Entidad', dataIndex: 'entidad', width: 70 },
    { title: 'Tipo doc.', dataIndex: 'tipoDoc', width: 70 },
    { title: 'Doc.', dataIndex: 'numDoc', width: 100 },
    { title: 'Razón social', dataIndex: 'razonSocial', width: 120, ellipsis: true },
    { title: 'Ap. paterno', dataIndex: 'apePat', width: 100, ellipsis: true },
    { title: 'Ap. materno', dataIndex: 'apeMat', width: 100, ellipsis: true },
    { title: 'Nombres', dataIndex: 'nombres', width: 110, ellipsis: true },
    { title: 'Tipo persona', dataIndex: 'tipoPersona', width: 90 },
    { title: 'Modalidad', dataIndex: 'modalidadCredito', width: 85 },
    { title: 'Deuda <30', dataIndex: 'deudaMenor30', width: 95 },
    { title: 'Deuda >30', dataIndex: 'deudaMayor30', width: 95 },
    { title: 'Calif.', dataIndex: 'calificacion', width: 55 },
    { title: 'Días', dataIndex: 'diasAtrazo', width: 55 },
    { title: 'Dirección', dataIndex: 'direccion', width: 140, ellipsis: true },
    { title: 'Celular', dataIndex: 'celular', width: 100 },
  ]
}
