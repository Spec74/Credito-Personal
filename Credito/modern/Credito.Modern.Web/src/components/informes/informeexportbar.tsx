import { Button, Space, Tooltip } from 'antd'
import { DownloadOutlined, FileExcelOutlined, FilePdfOutlined } from '@ant-design/icons'

export interface InformeExportBarProps {
  onCsv: () => void
  onPdfTabular: () => void
  csvLoading?: boolean
  pdfLoading?: boolean
  csvDisabled?: boolean
  pdfDisabled?: boolean
  /** Etiqueta del botón CSV (compatibilidad Excel). */
  csvLabel?: string
  pdfLabel?: string
  /** true = archivo .xlsx nativo (p. ej. cobranza). */
  nativeExcel?: boolean
}

/** Exportación CSV/PDF con estilo Credix (marca #114885). */
export function InformeExportBar({
  onCsv,
  onPdfTabular,
  csvLoading,
  pdfLoading,
  csvDisabled,
  pdfDisabled,
  csvLabel,
  pdfLabel = 'PDF',
  nativeExcel = false,
}: InformeExportBarProps) {
  const excelLabel = csvLabel ?? (nativeExcel ? 'Excel' : 'Excel (CSV)')
  const excelTip = nativeExcel
    ? 'Descargar libro Excel (.xlsx)'
    : 'Descargar CSV UTF-8 (abre en Excel; mismos datos que la tabla)'

  return (
    <Space wrap className="credix-export-bar credix-informe-export-bar">
      <Tooltip title={excelTip}>
        <Button
          icon={nativeExcel ? <FileExcelOutlined /> : <DownloadOutlined />}
          loading={csvLoading}
          disabled={csvDisabled}
          className="credix-report-btn credix-report-btn--xls"
          onClick={onCsv}
        >
          {excelLabel}
        </Button>
      </Tooltip>
      <Tooltip title="PDF con logo y columnas alineadas al informe legacy">
        <Button
          icon={<FilePdfOutlined />}
          loading={pdfLoading}
          disabled={pdfDisabled}
          className="credix-report-btn credix-report-btn--pdf"
          onClick={onPdfTabular}
        >
          {pdfLabel}
        </Button>
      </Tooltip>
    </Space>
  )
}
