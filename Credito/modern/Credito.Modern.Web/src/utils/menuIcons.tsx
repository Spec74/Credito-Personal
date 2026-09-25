import {
  AlertOutlined,
  AppstoreOutlined,
  AuditOutlined,
  BankOutlined,
  BarChartOutlined,
  BlockOutlined,
  CalculatorOutlined,
  CalendarOutlined,
  CheckCircleOutlined,
  CheckOutlined,
  ClockCircleOutlined,
  CreditCardOutlined,
  DashboardOutlined,
  DatabaseOutlined,
  DeleteOutlined,
  DollarOutlined,
  EnvironmentOutlined,
  EyeOutlined,
  FileProtectOutlined,
  FileSearchOutlined,
  FileTextOutlined,
  FundProjectionScreenOutlined,
  FundOutlined,
  GiftOutlined,
  GoldOutlined,
  HomeOutlined,
  InboxOutlined,
  LockOutlined,
  PercentageOutlined,
  PlusOutlined,
  PrinterOutlined,
  ProfileOutlined,
  QrcodeOutlined,
  SafetyOutlined,
  SaveOutlined,
  ScheduleOutlined,
  SettingOutlined,
  ShopOutlined,
  ShoppingCartOutlined,
  SolutionOutlined,
  StopOutlined,
  SwapOutlined,
  SyncOutlined,
  TableOutlined,
  TagsOutlined,
  TeamOutlined,
  ToolOutlined,
  UnorderedListOutlined,
  UserAddOutlined,
  UserDeleteOutlined,
  UserOutlined,
  WalletOutlined,
  WarningOutlined,
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
  folder: () => <AppstoreOutlined />,
  briefcase: () => <AuditOutlined />,
  'application-sidebar-list': () => <UnorderedListOutlined />,
  application: () => <AppstoreOutlined />,
  export: () => <InboxOutlined />,
  import: () => <InboxOutlined />,
  gold: () => <GoldOutlined />,
}

/** Iconos genéricos del menú MVC — no aportan semántica. */
const GENERIC_LEGACY_ICONS = new Set([
  'list',
  'list-alt',
  'th-list',
  'application-sidebar-list',
  'folder',
  'file',
  'application',
])

function normalizeText(value: string | null | undefined): string {
  return (value ?? '')
    .trim()
    .toUpperCase()
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
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

/**
 * Icono por función del ítem (denominación). Orden: reglas específicas primero.
 * Evita que todos los hijos de CREDITO hereden el mismo icono del módulo.
 */
function iconFromDenominacion(denominacion: string | null | undefined): ReactNode | null {
  const d = normalizeText(denominacion)
  if (!d) return null

  // —— Crédito / operaciones ——
  if (d.includes('PRENDARIO') || d.includes('PREDARIO') || d.includes('PRENDA')) {
    return <GoldOutlined />
  }
  if (d.includes('SIMUL') || d.includes('PARAMETRO')) {
    return <CalculatorOutlined />
  }
  if (d.includes('APROB')) {
    return <CheckCircleOutlined />
  }
  if (d.includes('TAREA')) {
    return <ScheduleOutlined />
  }
  if (d.includes('CONSULT') || d === 'CREDITOS' || d === 'CREDITO') {
    return <SolutionOutlined />
  }
  if (d.includes('PLAN') && d.includes('PAGO')) {
    return <ProfileOutlined />
  }
  if (d.includes('ESTADO') && d.includes('CREDIT')) {
    return <FileSearchOutlined />
  }
  if (d.includes('CONDON')) {
    return <PercentageOutlined />
  }
  if (d.includes('OBSERV')) {
    return <EyeOutlined />
  }
  if (d.includes('VENCID') || d.includes('MOROS') || d.includes('MORA')) {
    return <WarningOutlined />
  }
  if (d.includes('ACTIVO') && d.includes('CREDIT')) {
    return <CheckOutlined />
  }
  if (d.includes('CIERRE') && (d.includes('CREDIT') || d.includes('GERENC'))) {
    return <FundProjectionScreenOutlined />
  }
  if (d.includes('RENTABIL')) {
    return <FundOutlined />
  }
  if (d.includes('CENTRAL') && d.includes('RIESGO')) {
    return <SafetyOutlined />
  }
  if (d.includes('AVAL')) {
    return <FileProtectOutlined />
  }
  if (d.includes('TOPE')) {
    return <BlockOutlined />
  }

  // —— Caja / tesorería ——
  if (d.includes('CAJA DIARIO') || (d.includes('CAJA') && d.includes('DIARIO'))) {
    return <WalletOutlined />
  }
  if (d.includes('CAJA CHICA') || (d.includes('CAJA') && d.includes('CHICA'))) {
    return <DollarOutlined />
  }
  if (d.includes('ASIGNAR') && d.includes('CAJA')) {
    return <SwapOutlined />
  }
  if (d.includes('VERIFIC') && d.includes('PAGO')) {
    return <CheckOutlined />
  }
  if (d.includes('SALDO') && (d.includes('CAJA') || d.includes('CIERRE') || d.includes('CARTERA'))) {
    return <BarChartOutlined />
  }
  if (d.includes('ARQUEO')) {
    return <AuditOutlined />
  }
  if (d.includes('BOVEDA') || d.includes('TESOR')) {
    return <BankOutlined />
  }
  if (d.includes('MOVIMIENTO') && (d.includes('BOVED') || d.includes('CAJA') || d.includes('ALMACEN'))) {
    return <SwapOutlined />
  }
  if (d.includes('CAJA') || d.includes('MAESTRO CAJA')) {
    return <WalletOutlined />
  }

  // —— Cobranza / rutas ——
  if (d.includes('COBRO DIARIO') || (d.includes('COBRO') && d.includes('DETALLE'))) {
    return <EnvironmentOutlined />
  }
  if (d.includes('COBRO') || d.includes('COBRANZA') || d.includes('PAGO')) {
    return <DollarOutlined />
  }

  // —— Clientes ——
  if (d.includes('INACTIV')) {
    return <UserDeleteOutlined />
  }
  if (d.includes('BLOQUEAD') || d.includes('BLOQUEO')) {
    return <StopOutlined />
  }
  if (d.includes('NUEVO') && d.includes('CLIENT')) {
    return <UserAddOutlined />
  }
  if (d.includes('CLIENT') || d.includes('PERSONA') || d.includes('FICHA')) {
    return <TeamOutlined />
  }

  // —— Ventas ——
  if (d.includes('VENTA RAPIDA') || (d.includes('VENTA') && d.includes('RAPIDA'))) {
    return <ShoppingCartOutlined />
  }
  if (d.includes('ORDEN') && d.includes('VENTA')) {
    return <FileTextOutlined />
  }
  if (d.includes('LISTA') && d.includes('PRECIO')) {
    return <TagsOutlined />
  }
  if (d.includes('CANJE') || d.includes('PUNTO')) {
    return <GiftOutlined />
  }
  if (d.includes('VENT') || d.includes('PRECIO')) {
    return <ShopOutlined />
  }

  // —— Almacén ——
  if (d.includes('KARDEX')) {
    return <TableOutlined />
  }
  if (d.includes('ENTRADA')) {
    return <InboxOutlined />
  }
  if (d.includes('SALIDA') && d.includes('ALMACEN')) {
    return <SwapOutlined />
  }
  if (d.includes('TRANSFERENCIA')) {
    return <SwapOutlined />
  }
  if (d.includes('STOCK') || d.includes('INVENTAR')) {
    return <DatabaseOutlined />
  }
  if (d.includes('CODIGO') && d.includes('BARRA')) {
    return <QrcodeOutlined />
  }
  if (d.includes('CONSTANCIA')) {
    return <FileTextOutlined />
  }
  if (d.includes('ALMACEN') || d.includes('ARTICUL')) {
    return <InboxOutlined />
  }
  if (d.includes('MARCA') || d.includes('MODELO') || d.includes('TIPO ARTICUL')) {
    return <TagsOutlined />
  }

  // —— Reportes / informes ——
  if (d.includes('COBERTURA')) {
    return <BarChartOutlined />
  }
  if (d.includes('INFORME') || d.includes('REPORTE') || d.includes('RPT')) {
    return <FileTextOutlined />
  }

  // —— Admin / seguridad / maestros ——
  if (d.includes('USUARIO')) {
    return <UserOutlined />
  }
  if (d.includes('ROL') || d.includes('PERMISO')) {
    return <SafetyOutlined />
  }
  if (d.includes('OFICINA')) {
    return <HomeOutlined />
  }
  if (d.includes('COMISION')) {
    return <PercentageOutlined />
  }
  if (d.includes('SEGUR') || d.includes('CLAVE') || d.includes('PASSWORD')) {
    return <LockOutlined />
  }
  if (d.includes('MANTEN') || d.includes('MAESTRO') || d.includes('CONFIG') || d.includes('PARAMET')) {
    return <SettingOutlined />
  }
  if (d.includes('HERRAMIENT') || d.includes('UTILIDAD')) {
    return <ToolOutlined />
  }

  // —— Tiempo / calendario ——
  if (d.includes('CALENDAR') || d.includes('AGENDA') || d.includes('HORARIO')) {
    return <CalendarOutlined />
  }
  if (d.includes('PENDIENTE') || d.includes('ESPERA')) {
    return <ClockCircleOutlined />
  }
  if (d.includes('ALERT') || d.includes('AVISO')) {
    return <AlertOutlined />
  }

  // Fallback genérico de crédito (solo si el nombre lo menciona y no hubo match específico)
  if (d.includes('CREDIT')) {
    return <CreditCardOutlined />
  }

  return null
}

function iconFromModulo(modulo: string | null | undefined): ReactNode | null {
  const key = normalizeText(modulo)
  const map: Record<string, IconFactory> = {
    CREDITO: () => <BankOutlined />,
    REPORTES: () => <FileTextOutlined />,
    REPORTE: () => <FileTextOutlined />,
    CAJA: () => <WalletOutlined />,
    VENTAS: () => <ShopOutlined />,
    ALMACEN: () => <InboxOutlined />,
    ADMINISTRACION: () => <SafetyOutlined />,
    ADMIN: () => <SafetyOutlined />,
    TESORERIA: () => <BankOutlined />,
    CLIENTE: () => <TeamOutlined />,
    CLIENTES: () => <TeamOutlined />,
    MAESTRO: () => <SettingOutlined />,
    MAESTROS: () => <SettingOutlined />,
    MANTENIMIENTO: () => <ToolOutlined />,
    SEGURIDAD: () => <LockOutlined />,
  }
  const factory = map[key]
  return factory ? factory() : null
}

function iconFromLegacyKey(icono: string | null | undefined): ReactNode | null {
  const key = icono ? normalizeLegacyIconKey(icono) : null
  if (!key || GENERIC_LEGACY_ICONS.has(key)) {
    return null
  }
  const exact = LEGACY_ICON_MAP[key]
  if (exact) {
    return exact()
  }
  const partial = Object.entries(LEGACY_ICON_MAP).find(
    ([k]) => k.length >= 4 && key.includes(k) && !GENERIC_LEGACY_ICONS.has(k),
  )
  return partial ? partial[1]() : null
}

/**
 * Icono del ítem de menú.
 * Prioridad: 1) función por denominación  2) icono legacy semántico  3) módulo  4) genérico.
 * Así los hijos de CREDITO no heredan todos el BankOutlined del padre.
 */
export function menuItemIcon(
  icono: string | null | undefined,
  denominacion?: string | null,
  modulo?: string | null,
): ReactNode {
  const fromName = iconFromDenominacion(denominacion)
  if (fromName) {
    return fromName
  }

  const fromLegacy = iconFromLegacyKey(icono)
  if (fromLegacy) {
    return fromLegacy
  }

  const fromMod = iconFromModulo(modulo)
  if (fromMod) {
    return fromMod
  }

  return <AppstoreOutlined />
}

/** Icono del grupo padre (módulo / categoría). */
export function parentMenuIcon(denominacion: string | null | undefined): ReactNode {
  const d = normalizeText(denominacion)
  if (d.includes('CREDIT')) return <BankOutlined />
  if (d.includes('REPORT') || d.includes('INFORM')) return <FileTextOutlined />
  if (d.includes('CAJA')) return <WalletOutlined />
  if (d.includes('VENT')) return <ShopOutlined />
  if (d.includes('ALMACEN')) return <InboxOutlined />
  if (d.includes('TESOR') || d.includes('BOVED')) return <BankOutlined />
  if (d.includes('CLIENT')) return <TeamOutlined />
  if (d.includes('ADMIN') || d.includes('SEGUR')) return <SafetyOutlined />
  if (d.includes('MANTEN') || d.includes('MAESTRO')) return <SettingOutlined />
  if (d.includes('PRENDAR')) return <GoldOutlined />
  return iconFromDenominacion(denominacion) ?? <AppstoreOutlined />
}

export const dashboardMenuIcon = <DashboardOutlined />
