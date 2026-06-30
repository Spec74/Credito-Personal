import {
  AuditOutlined,
  BankOutlined,
  BarChartOutlined,
  CalculatorOutlined,
  CheckCircleOutlined,
  CreditCardOutlined,
  DollarOutlined,
  FileTextOutlined,
  GoldOutlined,
  InboxOutlined,
  PlusOutlined,
  QrcodeOutlined,
  SafetyOutlined,
  SearchOutlined,
  SettingOutlined,
  ShopOutlined,
  ShoppingCartOutlined,
  SwapOutlined,
  TableOutlined,
  TeamOutlined,
  UserOutlined,
  WalletOutlined,
} from '@ant-design/icons'
import type { ReactNode } from 'react'

function renderAprobarIcon() {
  // Icono moderno de "aprobación": check en escudo.
  return (
    <svg
      width={18}
      height={18}
      viewBox="0 0 24 24"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      <path
        d="M12 2.5C8.4 5 5.4 5.2 4 5.2V11.3C4 16.6 7.6 20.2 12 21.5C16.4 20.2 20 16.6 20 11.3V5.2C18.6 5.2 15.6 5 12 2.5Z"
        stroke="currentColor"
        strokeWidth="1.9"
        strokeLinejoin="round"
      />
      <path
        d="M8.4 12.1L10.9 14.6L16.1 9.4"
        stroke="currentColor"
        strokeWidth="2.0"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function renderSaldosCajaIcon() {
  // Icono moderno de "saldos/cierres": caja/moneda en bloque.
  return (
    <svg
      width={18}
      height={18}
      viewBox="0 0 24 24"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      <rect
        x="3.5"
        y="6.5"
        width="17"
        height="14"
        rx="3"
        stroke="currentColor"
        strokeWidth="1.9"
      />
      <path
        d="M7 6.8V5.2C7 4.5 7.5 4 8.2 4H15.8C16.5 4 17 4.5 17 5.2V6.8"
        stroke="currentColor"
        strokeWidth="1.9"
        strokeLinecap="round"
      />
      <circle
        cx="12"
        cy="13.1"
        r="3.4"
        stroke="currentColor"
        strokeWidth="1.9"
      />
      <path
        d="M11.1 11.9L13.3 14.1"
        stroke="currentColor"
        strokeWidth="1.9"
        strokeLinecap="round"
      />
      <path
        d="M13.3 11.9L11.1 14.1"
        stroke="currentColor"
        strokeWidth="1.9"
        strokeLinecap="round"
      />
    </svg>
  )
}

/** Iconos (modern) por ruta SPA para tarjetas de módulos. */
export function hubLinkIcon(to: string, label?: string): ReactNode {
  const path = to.toLowerCase()
  const text = (label ?? '').toLowerCase()

  if (path.includes('/caja/diario') || text.includes('caja diario')) {
    return <DollarOutlined />
  }
  if (path.includes('/caja/chica')) return <WalletOutlined />
  if (path.includes('/caja/saldos') || text.includes('saldos')) return renderSaldosCajaIcon()
  if (path.includes('/caja/asignar')) return <CheckCircleOutlined />
  if (path.includes('/caja/verificar')) return <AuditOutlined />
  if (path.includes('/caja/maestro')) return <SettingOutlined />
  if (path.includes('/credito/simulador')) return <CalculatorOutlined />
  if (path.includes('/credito/prendario') || text.includes('prendario')) return <GoldOutlined />
  if (path.includes('/credito/consulta')) return <SearchOutlined />
  if (path.includes('/credito/aprobar')) return renderAprobarIcon()
  if (path.includes('/credito/tareas')) return <TableOutlined />
  if (path.includes('/clientes')) return <TeamOutlined />
  if (path.includes('/ventas/venta-rapida')) return <ShoppingCartOutlined />
  if (path.includes('/ventas/orden')) return <ShopOutlined />
  if (path.includes('/canjear')) return <CreditCardOutlined />
  if (path.includes('/lista-precio')) return <BarChartOutlined />
  if (path.includes('/almacen/entrada')) return <InboxOutlined />
  if (path.includes('/almacen/salida')) return <SwapOutlined />
  if (path.includes('/transferencia')) return <SwapOutlined />
  if (path.includes('/kardex') || path.includes('/stock')) return <BarChartOutlined />
  if (path.includes('/boveda')) return <BankOutlined />
  if (path.includes('/reportes/cobranza') || text.includes('cobranza')) {
    return <WalletOutlined />
  }
  if (path.includes('/informes/cobro') || text.includes('cobro diario')) {
    return <DollarOutlined />
  }
  if (path.includes('morosidad') || text.includes('morosidad') || text.includes('vencido')) {
    return <AuditOutlined />
  }
  if (path.includes('rentabilidad')) return <BarChartOutlined />
  if (path.includes('aprobacion') || text.includes('aprob')) return <CheckCircleOutlined />
  if (path.includes('central-riesgo') || text.includes('central')) return <SafetyOutlined />
  if (path.includes('clientes') || text.includes('cliente')) return <TeamOutlined />
  if (path.includes('saldo-cartera') || text.includes('cartera')) return <BankOutlined />
  if (path.includes('plan-pagos') || path.includes('estado-credito')) return <TableOutlined />
  if (path.includes('caja') || text.includes('caja')) return <DollarOutlined />
  if (path.includes('/informes')) return <FileTextOutlined />
  if (path.includes('/admin/usuario')) return <UserOutlined />
  if (path.includes('/admin/rol')) return <SafetyOutlined />
  if (path.includes('/admin/oficina')) return <BankOutlined />
  if (path.includes('/maestros/articulo')) return <ShopOutlined />
  if (path.includes('/maestros')) return <TableOutlined />
  if (path.includes('/cobertura')) return <AuditOutlined />
  if (path.includes('/codigo-barras')) return <QrcodeOutlined />
  if (path.includes('/nuevo')) return <PlusOutlined />
  return <FileTextOutlined />
}
