import type { ReactElement } from 'react'
import { Button, Space, Tooltip } from 'antd'
import { DownloadOutlined, FileExcelOutlined, FilePdfOutlined } from '@ant-design/icons'

export interface InformeExportBarProps {
  onCsv: () => void
  onPdfTabular: () => void
  csvLoading?: boolean
  pdfLoading?: boolean
  csvDisabled?: boolean
  pdfDisabled?: boolean
  /** Motivo cuando Excel/CSV está deshabilitado (tooltip en botón deshabilitado). */
  csvDisabledReason?: string
  /** Motivo cuando PDF está deshabilitado. */
  pdfDisabledReason?: string
  /** Etiqueta del botón CSV (compatibilidad Excel). */
  csvLabel?: string
  pdfLabel?: string
  /** true = archivo .xlsx nativo (p. ej. cobranza). */
  nativeExcel?: boolean
}

function DisabledAwareButton({
  disabled,
  tip,
  children,
}: {
  disabled?: boolean
  tip: string
  children: ReactElement
}) {
  if (disabled) {
    return (
      <Tooltip title={tip}>
        <span style={{ display: 'inline-block' }}>{children}</span>
      </Tooltip>
    )
  }
  return <Tooltip title={tip}>{children}</Tooltip>
}

/** Exportación CSV/PDF con estilo Credix (marca #114885). */
export function InformeExportBar({
  onCsv,
  onPdfTabular,
  csvLoading,
  pdfLoading,
  csvDisabled,
  pdfDisabled,
  csvDisabledReason = 'Consulte primero o no hay filas para exportar',
  pdfDisabledReason = 'Consulte primero o no hay filas para exportar',
  csvLabel,
  pdfLabel = 'PDF',
  nativeExcel = false,
}: InformeExportBarProps) {
  const excelLabel = csvLabel ?? (nativeExcel ? 'Excel' : 'Excel (CSV)')
  const excelTip = csvDisabled
    ? csvDisabledReason
    : nativeExcel
      ? 'Descargar libro Excel (.xlsx)'
      : 'Descargar CSV UTF-8 (abre en Excel; mismos datos que la tabla)'
  const pdfTip = pdfDisabled
    ? pdfDisabledReason
    : 'PDF con logo y columnas alineadas al informe legacy'

  return (
    <Space wrap className="credix-export-bar credix-informe-export-bar">
      <DisabledAwareButton disabled={csvDisabled} tip={excelTip}>
        <Button
          icon={nativeExcel ? <FileExcelOutlined /> : <DownloadOutlined />}
          loading={csvLoading}
          disabled={csvDisabled}
          className="credix-report-btn credix-report-btn--xls"
          onClick={onCsv}
        >
          {excelLabel}
        </Button>
      </DisabledAwareButton>
      <DisabledAwareButton disabled={pdfDisabled} tip={pdfTip}>
        <Button
          icon={<FilePdfOutlined />}
          loading={pdfLoading}
          disabled={pdfDisabled}
          className="credix-report-btn credix-report-btn--pdf"
          onClick={onPdfTabular}
        >
          {pdfLabel}
        </Button>
      </DisabledAwareButton>
    </Space>
  )
}
