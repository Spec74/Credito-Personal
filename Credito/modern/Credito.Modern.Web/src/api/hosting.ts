import { apiFetch } from './client'

export interface HostingUiConfig {
  useSpaForModule: boolean
  defaultLoginToSpa: boolean
  spaBasePath: string
  observacionDias: number
  fase: string
}

export function fetchHostingUiConfig(): Promise<HostingUiConfig> {
  return apiFetch<HostingUiConfig>('/hosting/ui-config')
}
