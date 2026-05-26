import { Link } from 'react-router-dom'
import { Button, Space, Tooltip } from 'antd'
import {
  FileExcelOutlined,
  FilePdfOutlined,
  EyeOutlined,
} from '@ant-design/icons'
import type { ReactNode } from 'react'
import { buildSpaInformeUrl } from '../../utils/spaReportNavigation'

export type ReportExportItem = {
  label: string
  format?: 'pdf' | 'xls' | 'txt' | 'primary'
  onClick: () => void
  disabled?: boolean
  loading?: boolean
  title?: string
}

export type ReportScreenLink = {
  label: string
  to: string
  searchParams?: Record<string, string | number | undefined | null>
}

export function ReportExportActions({
  exports,
  screenTo,
  screenSearchParams,
  screenOpenInNewTab = false,
  screenLabel = 'Ver pantalla',
  screenLinks,
  extra,
}: {
  exports: ReportExportItem[]
  screenTo?: string
  /** Parámetros en query (API o legacy) para la pantalla SPA. */
  screenSearchParams?: Record<string, string | number | undefined | null>
  /** Si true, abre la SPA en pestaña nueva; por defecto navega en la misma pestaña. */
  screenOpenInNewTab?: boolean
  screenLabel?: string
  /** Enlaces adicionales «Ver pantalla» con otros filtros del mismo cuadro. */
  screenLinks?: ReportScreenLink[]
  extra?: ReactNode
}) {
  const screenHref =
    screenTo != null ? buildSpaInformeUrl(screenTo, screenSearchParams) : undefined

  const screenLinkTo =
    screenTo != null
      ? (() => {
          const search = new URLSearchParams()
          if (screenSearchParams) {
            for (const [key, value] of Object.entries(screenSearchParams)) {
              if (value != null && value !== '') {
                search.set(key, String(value))
              }
            }
          }
          const qs = search.toString()
          return { pathname: screenTo, search: qs ? `?${qs}` : '' }
        })()
      : undefined

  return (
    <div className="credix-report-actions">
      <Space wrap size={[6, 6]} className="credix-report-actions__exports">
        {exports.map((item) => {
          const icon =
            item.format === 'pdf' ? (
              <FilePdfOutlined />
            ) : item.format === 'xls' ? (
              <FileExcelOutlined />
            ) : undefined
          const btn = (
            <Button
              key={item.label}
              type={item.format === 'primary' ? 'primary' : 'default'}
              size="small"
              icon={icon}
              disabled={item.disabled}
              loading={item.loading}
              className={
                item.format === 'pdf'
                  ? 'credix-report-btn credix-report-btn--pdf'
                  : item.format === 'xls'
                    ? 'credix-report-btn credix-report-btn--xls'
                    : 'credix-report-btn'
              }
              onClick={item.onClick}
            >
              {item.label}
            </Button>
          )
          return item.title ? (
            <Tooltip key={item.label} title={item.title}>
              {btn}
            </Tooltip>
          ) : (
            btn
          )
        })}
        {extra}
      </Space>
      <span className="credix-report-actions__screens">
        {screenHref &&
          (screenOpenInNewTab ? (
            <a
              href={screenHref}
              target="_blank"
              rel="noopener noreferrer"
              className="credix-report-actions__screen"
            >
              <EyeOutlined /> {screenLabel}
            </a>
          ) : screenLinkTo ? (
            <Link to={screenLinkTo} className="credix-report-actions__screen">
              <EyeOutlined /> {screenLabel}
            </Link>
          ) : null)}
        {screenLinks?.map((link) => {
          const search = new URLSearchParams()
          if (link.searchParams) {
            for (const [key, value] of Object.entries(link.searchParams)) {
              if (value != null && value !== '') {
                search.set(key, String(value))
              }
            }
          }
          const qs = search.toString()
          return (
            <Link
              key={link.to + link.label}
              to={{ pathname: link.to, search: qs ? `?${qs}` : '' }}
              className="credix-report-actions__screen"
            >
              <EyeOutlined /> {link.label}
            </Link>
          )
        })}
      </span>
    </div>
  )
}
