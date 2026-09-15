import {
  AppstoreOutlined,
  AuditOutlined,
  BankOutlined,
  BarChartOutlined,
  CalculatorOutlined,
  CalendarOutlined,
  CheckCircleOutlined,
  CheckOutlined,
  CreditCardOutlined,
  DashboardOutlined,
  DeleteOutlined,
  DollarOutlined,
  EnvironmentOutlined,
  FileTextOutlined,
  FolderOutlined,
  GoldOutlined,
  HomeOutlined,
  InboxOutlined,
  LockOutlined,
  PercentageOutlined,
  PlusOutlined,
  PrinterOutlined,
  QrcodeOutlined,
  SafetyOutlined,
  SaveOutlined,
  SettingOutlined,
  ShopOutlined,
  ShoppingCartOutlined,
  SyncOutlined,
  TableOutlined,
  TagsOutlined,
  TeamOutlined,
  UnorderedListOutlined,
  UserOutlined,
  WalletOutlined,
} from '@ant-design/icons'
import type { ReactNode } from 'react'

type IconFactory = () => ReactNode

const LEGACY_ICON_MAP: Record<string, IconFactory> = {
  list: () => <UnorderedListOutlined />,
  'list-alt': () => <UnorderedListOutlined />,
  'th-list': () => <UnorderedListOutlined />,
  table: () => <TableOutlined />,
  user: () => <UserOutlined />,
  users: () => <TeamOutlined />,
  cog: () => <SettingOutlined />,
  gear: () => <SettingOutlined />,
  home: () => <HomeOutlined />,
  file: () => <FileTextOutlined />,
  book: () => <FileTextOutlined />,
  print: () => <PrinterOutlined />,
  ok: () => <CheckOutlined />,
  check: () => <CheckOutlined />,
  remove: () => <DeleteOutlined />,
  trash: () => <DeleteOutlined />,
  lock: () => <LockOutlined />,
  repeat: () => <SyncOutlined />,
  refresh: () => <SyncOutlined />,
  calculator: () => <CalculatorOutlined />,
  inbox: () => <InboxOutlined />,
  'inbox-document': () => <InboxOutlined />,
  calendar: () => <CalendarOutlined />,
  'bar-chart': () => <BarChartOutlined />,
  chart: () => <BarChartOutlined />,
  'credit-card': () => <CreditCardOutlined />,
  money: () => <DollarOutlined />,
  eur: () => <DollarOutlined />,
  'shopping-cart': () => <ShoppingCartOutlined />,
  shop: () => <ShopOutlined />,
  'map-marker': () => <EnvironmentOutlined />,
  qrcode: () => <QrcodeOutlined />,
  plus: () => <PlusOutlined />,
  save: () => <SaveOutlined />,
  legal: () => <AuditOutlined />,
  play: () => <CheckCircleOutlined />,
  folder: () => <FolderOutlined />,
  briefcase: () => <AuditOutlined />,
  'application-sidebar-list': () => <UnorderedListOutlined />,
  application: () => <AppstoreOutlined />,
  export: () => <InboxOutlined />,
  import: () => <InboxOutlined />,
  gold: () => <GoldOutlined />,
}

function normalizeLegacyIconKey(icono: string): string | null {
  let raw = icono.trim().toLowerCase()
  if (!raw) return null
  raw = raw.replace(/^icon\s+/, '')
  if (raw.startsWith('icon-')) {
    return raw.slice(5)
  }
  if (raw.startsWith('i24_')) {
    return raw.slice(4)
  }
  return raw
}

function iconFromDenominacion(denominacion: string | null | undefined): ReactNode {
  const d = (denominacion ?? '').toUpperCase().normalize('NFD').replace(/\p{M}/gu, '')
  if (d.includes('PRENDARIO') || d.includes('PREDARIO') || d.includes('PRENDA')) {
    return <GoldOutlined />
  }
  if (d.includes('COBRO') || d.includes('COBRANZA') || d.includes('PAGO')) {
    return <DollarOutlined />
  }
  if (d.includes('CAJA') || d.includes('ARQUEO') || d.includes('SALDO')) {
    return <WalletOutlined />
  }
  if (d.includes('BOVEDA') || d.includes('TESOR')) {
    return <BankOutlined />
  }
  if (d.includes('CREDIT') || d.includes('SIMUL') || d.includes('PLAN')) {
    return <BankOutlined />
  }
  if (d.includes('APROB')) {
    return <CheckCircleOutlined />
  }
  if (d.includes('CLIENT') || d.includes('PERSONA')) {
    return <TeamOutlined />
  }
  if (d.includes('REPORT') || d.includes('INFORM') || d.includes('RPT')) {
    return <FileTextOutlined />
  }
  if (d.includes('USUARIO') || d.includes('ROL')) {
    return <UserOutlined />
  }
  if (d.includes('OFICINA') || d.includes('SEGUR') || d.includes('COMISION')) {
    return d.includes('COMISION') ? <PercentageOutlined /> : <SafetyOutlined />
  }
  if (d.includes('VENT') || d.includes('PRECIO') || d.includes('ORDEN')) {
    return <ShopOutlined />
  }
  if (d.includes('ALMACEN') || d.includes('KARDEX') || d.includes('ENTRADA') || d.includes('SALIDA')) {
    return <InboxOutlined />
  }
  if (d.includes('MARCA') || d.includes('MODELO') || d.includes('ARTICUL')) {
    return <TagsOutlined />
  }
  if (d.includes('TAREA')) {
    return <AuditOutlined />
  }
  if (d.includes('VERIFIC')) {
    return <CheckOutlined />
  }
  return <AppstoreOutlined />
}

function iconFromModulo(modulo: string | null | undefined): ReactNode | null {
  const key = (modulo ?? '')
    .trim()
    .toUpperCase()
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
  const map: Record<string, IconFactory> = {
    CREDITO: () => <BankOutlined />,
    REPORTES: () => <FileTextOutlined />,
    REPORTE: () => <FileTextOutlined />,
    CAJA: () => <WalletOutlined />,
    VENTAS: () => <ShopOutlined />,
    ALMACEN: () => <InboxOutlined />,
    ADMINISTRACION: () => <SafetyOutlined />,
    TESORERIA: () => <BankOutlined />,
    CLIENTE: () => <TeamOutlined />,
    CLIENTES: () => <TeamOutlined />,
    MAESTRO: () => <SettingOutlined />,
    MAESTROS: () => <SettingOutlined />,
  }
  const factory = map[key]
  return factory ? factory() : null
}

/** Icono vectorial Ant Design para ítems del menú (reemplaza Fugue/FA pixelados). */
export function menuItemIcon(
  icono: string | null | undefined,
  denominacion?: string | null,
  modulo?: string | null,
): ReactNode {
  const key = icono ? normalizeLegacyIconKey(icono) : null
  if (key) {
    const exact = LEGACY_ICON_MAP[key]
    if (exact) return exact()
    const partial = Object.entries(LEGACY_ICON_MAP).find(([k]) => key.includes(k))
    if (partial) return partial[1]()
  }
  const fromMod = iconFromModulo(modulo)
  if (fromMod) return fromMod
  return iconFromDenominacion(denominacion)
}

export function parentMenuIcon(denominacion: string | null | undefined): ReactNode {
  return menuItemIcon(null, denominacion, null)
}

export const dashboardMenuIcon = <DashboardOutlined />
