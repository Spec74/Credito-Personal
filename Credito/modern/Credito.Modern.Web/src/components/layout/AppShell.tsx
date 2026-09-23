import { Suspense, useCallback, useEffect, useMemo, useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Drawer, Grid, Menu, Result, Spin, Tag, Typography, message } from 'antd'
import './app-shell.css'
import {
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  FundProjectionScreenOutlined,
  UserOutlined,
} from '@ant-design/icons'
import { fetchMenu } from '../../api/menu'
import { fetchOficinas } from '../../api/oficinas'
import { fetchCierreGerencialPermisos } from '../../api/cierreGerencial'
import { useAuth } from '../../auth/useAuth'
import { getLoginProfile } from '../../auth/sessionProfile'
import { BrandLogo } from '../brand/BrandLogo'
import { RouteFallback } from './RouteFallback'
import { branding } from '../../config/branding'
import { quickActions, type QuickAction } from '../../config/quickActions'
import {
  openLegacyClientesInactivosGestor,
  openLegacyCreditoObservado,
  openLegacyMorosidadGestor,
} from '../../config/legacyreporturls'
import {
  buildAntMenuItems,
  defaultOpenMenuKeys,
  findMenuItem,
  findSelectedMenuKeys,
} from '../../utils/menuTree'
import { resolveSpaPathFromModulo } from '../../utils/legacyRoutes'
import { resolveSpaPathFromMenuItem } from '../../utils/resolveSpaPathFromMenuItem'
import { hasMenuRouteAccess } from '../../utils/menuRouteAccess'

const { Text } = Typography

export function AppShell() {
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  const { session, logout } = useAuth()
  const screens = Grid.useBreakpoint()
  const isMobile = !screens.lg
  const [desktopCollapsed, setDesktopCollapsed] = useState(false)
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [openKeysUser, setOpenKeysUser] = useState<string[] | null>(null)
  const [menuStamp, setMenuStamp] = useState('')
  const profile = getLoginProfile()

  const collapsed = isMobile ? false : desktopCollapsed

  const [prevIsMobile, setPrevIsMobile] = useState(isMobile)
  if (isMobile !== prevIsMobile) {
    setPrevIsMobile(isMobile)
    if (isMobile) {
      setMobileNavOpen(false)
    }
  }

  useEffect(() => {
    document.body.classList.add('credix-app-body')
    return () => document.body.classList.remove('credix-app-body')
  }, [])

  const oficinasQuery = useQuery({
    queryKey: ['oficinas'],
    queryFn: fetchOficinas,
    staleTime: 10 * 60_000,
  })

  const menuQuery = useQuery({
    queryKey: ['menu', session?.oficinaId, session?.usuarioId],
    queryFn: fetchMenu,
    enabled: (session?.oficinaId ?? 0) > 0 && (session?.usuarioId ?? 0) > 0,
    staleTime: 5 * 60_000,
  })

  const cierrePermisosQuery = useQuery({
    queryKey: ['cierre-gerencial-permisos', session?.usuarioId],
    queryFn: fetchCierreGerencialPermisos,
    enabled: (session?.usuarioId ?? 0) > 0,
    staleTime: 5 * 60_000,
  })
  const puedeVerCierreGerencial = cierrePermisosQuery.data?.puedeConsultar === true

  const navigationMenuData = useMemo(() => menuQuery.data ?? [], [menuQuery.data])
  /** Paridad `_Layout.cshtml`: los 5 accesos rápidos siempre visibles con sesión. */
  const quickActionsVisible = quickActions
  const extraAllowedPaths = useMemo(
    () => (puedeVerCierreGerencial ? ['/informes/cierre-gerencial'] : []),
    [puedeVerCierreGerencial],
  )
  const hasCurrentRouteAccess = useMemo(
    () => hasMenuRouteAccess(location.pathname, navigationMenuData, extraAllowedPaths),
    [location.pathname, navigationMenuData, extraAllowedPaths],
  )

  const defaultOpenKeys = useMemo(
    () => (navigationMenuData.length > 0 ? defaultOpenMenuKeys(navigationMenuData) : []),
    [navigationMenuData],
  )
  const nextMenuStamp = `${session?.oficinaId ?? 0}-${session?.usuarioId ?? 0}-${menuQuery.dataUpdatedAt ?? 0}`
  if (nextMenuStamp !== menuStamp && defaultOpenKeys.length > 0) {
    setMenuStamp(nextMenuStamp)
    setOpenKeysUser(null)
  }
  const openKeys = openKeysUser ?? defaultOpenKeys

  useEffect(() => {
    if (!session) {
      queryClient.removeQueries({ queryKey: ['menu'] })
    }
  }, [session, queryClient])

  const oficinaNombre = useMemo(() => {
    if (profile.oficinaLabel) {
      return profile.oficinaLabel
    }
    const id = session?.oficinaId
    if (!id) {
      return null
    }
    return (
      oficinasQuery.data?.find((o) => o.oficinaId === id)?.denominacion ??
      `Oficina ${id}`
    )
  }, [profile.oficinaLabel, session?.oficinaId, oficinasQuery.data])

  const usuarioNombre =
    profile.nombreUsuario ?? (session ? `Usuario ${session.usuarioId}` : '')

  const menuItems = useMemo(() => {
    const base = buildAntMenuItems(navigationMenuData)
    if (!puedeVerCierreGerencial) {
      return base
    }
    return [
      ...base,
      {
        key: 'cierre-gerencial',
        icon: <FundProjectionScreenOutlined />,
        label: <span className="credix-menu-label">Cierre gerencial</span>,
      },
    ]
  }, [navigationMenuData, puedeVerCierreGerencial])
  const selectedKeys = useMemo(() => {
    const keys = findSelectedMenuKeys(location.pathname, navigationMenuData)
    if (location.pathname.startsWith('/informes/cierre-gerencial')) {
      return [...keys, 'cierre-gerencial']
    }
    return keys
  }, [location.pathname, navigationMenuData])

  const closeMobileNav = useCallback(() => {
    if (isMobile) {
      setMobileNavOpen(false)
    }
  }, [isMobile])

  const navigateMenuItem = (item: (typeof navigationMenuData)[number]) => {
    const spaFromMenu = resolveSpaPathFromMenuItem(
      item.url,
      item.denominacion,
      item.modulo,
    )
    if (spaFromMenu) {
      navigate(spaFromMenu)
      closeMobileNav()
      return
    }

    if (item.url?.trim()) {
      navigate(`/modulo/${item.menuId}`, {
        state: {
          titulo: item.denominacion,
          modulo: item.modulo,
          legacyUrl: item.url,
        },
      })
      closeMobileNav()
      return
    }

    const hubSinUrl = resolveSpaPathFromModulo(item.modulo)
    if (hubSinUrl) {
      navigate(hubSinUrl)
      closeMobileNav()
      return
    }

    navigate(`/modulo/${item.menuId}`, {
      state: {
        titulo: item.denominacion,
        modulo: item.modulo,
        legacyUrl: item.url,
      },
    })
    closeMobileNav()
  }

  const onMenuClick = ({ key }: { key: string }) => {
    if (key === 'dashboard') {
      navigate('/inicio')
      closeMobileNav()
      return
    }
    if (key === 'cierre-gerencial') {
      navigate('/informes/cierre-gerencial')
      closeMobileNav()
      return
    }
    if (key.startsWith('parent-') || key.startsWith('mod-')) {
      return
    }

    const menuId = Number(key)
    if (Number.isNaN(menuId)) {
      return
    }

    const item = findMenuItem(navigationMenuData, menuId)
    if (item) {
      navigateMenuItem(item)
    }
  }

  const toggleNav = () => {
    if (isMobile) {
      setMobileNavOpen((o) => !o)
    } else {
      setDesktopCollapsed((c) => !c)
    }
  }

  const roleTags = session?.roles ?? []

  const onQuickAction = useCallback(
    (qa: QuickAction) => {
      const oficinaId = session?.oficinaId
      const usuarioId = session?.usuarioId
      closeMobileNav()

      if (qa.kind === 'navigate') {
        navigate(qa.spaPath ?? '/inicio')
        return
      }

      if (!oficinaId || !usuarioId) {
        message.warning('Sesión incompleta: no se puede generar el reporte.')
        return
      }

      try {
        if (qa.kind === 'pdf-observados') {
          // Legacy: pOficinaId=null (todas) + pUsuarioId.
          openLegacyCreditoObservado(undefined, usuarioId, 'PDF')
        } else if (qa.kind === 'pdf-vencidos') {
          // Legacy: pOficinaId=null + pUsuarioId → PDF morosidad gestor.
          openLegacyMorosidadGestor(undefined, usuarioId, 'PDF')
        } else if (qa.kind === 'pdf-inactivos') {
          openLegacyClientesInactivosGestor(oficinaId, usuarioId)
        }
      } catch {
        message.error('No se pudo abrir el reporte. Permita ventanas emergentes.')
      }
    },
    [closeMobileNav, navigate, session?.oficinaId, session?.usuarioId],
  )

  const navPanel = (
    <NavPanel
      menuExpanded={isMobile || !collapsed}
      menuQuery={menuQuery}
      menuItems={menuItems}
      selectedKeys={selectedKeys}
      openKeys={openKeys}
      setOpenKeys={setOpenKeysUser}
      onMenuClick={onMenuClick}
      onRetryMenu={() => void menuQuery.refetch()}
      quickActionsVisible={quickActionsVisible}
      onQuickAction={onQuickAction}
    />
  )

  return (
    <div className="credix-app">
      <a href="#main-content" className="credix-skip-link">
        Saltar al contenido
      </a>
      <header className="credix-header">
        <div className="credix-header-start">
          <Button
            type="text"
            icon={mobileNavOpen && isMobile ? <MenuFoldOutlined /> : <MenuUnfoldOutlined />}
            onClick={toggleNav}
            aria-label={isMobile && mobileNavOpen ? 'Cerrar menú' : 'Abrir menú'}
            aria-expanded={isMobile ? mobileNavOpen : !collapsed}
            aria-controls={isMobile ? undefined : 'credix-sidebar-nav'}
          />
          <span className="credix-header-brand">
            <strong>CREDICONFIABLE</strong>
          </span>
          <span className="credix-header-user">
            <UserOutlined />
            <Text strong style={{ color: '#fff' }}>
              {usuarioNombre}
            </Text>
            {oficinaNombre ? (
              <Text style={{ color: 'rgba(255,255,255,0.85)' }}> · {oficinaNombre}</Text>
            ) : null}
          </span>
        </div>
        <div className="credix-header-end">
          {screens.md &&
            roleTags.slice(0, 3).map((rol) => (
              <Tag key={rol}>{rol}</Tag>
            ))}
          {screens.md && roleTags.length > 3 ? (
            <Tag>+{roleTags.length - 3}</Tag>
          ) : null}
          {!screens.md && roleTags.length > 0 ? (
            <Tag>{roleTags.length} roles</Tag>
          ) : null}
          <Button
            type="text"
            icon={<LogoutOutlined />}
            aria-label="Cerrar sesión"
            onClick={() => {
              logout()
              navigate('/login')
            }}
          >
            {screens.sm ? 'Salir' : null}
          </Button>
        </div>
      </header>

      <div className="credix-layout">
        {!isMobile && (
          <aside
            id="credix-sidebar-nav"
            className={
              collapsed ? 'credix-sidebar credix-sidebar--collapsed' : 'credix-sidebar'
            }
          >
            {navPanel}
          </aside>
        )}

        <Drawer
          title={branding.companyName}
          placement="left"
          open={isMobile && mobileNavOpen}
          onClose={() => setMobileNavOpen(false)}
          width={Math.min(300, typeof window !== 'undefined' ? window.innerWidth * 0.88 : 300)}
          className="app-shell-drawer"
          styles={{ body: { padding: 0 } }}
          destroyOnClose={false}
        >
          {navPanel}
        </Drawer>

        <div className="credix-main">
          <main id="main-content" className="credix-content app-shell-content" tabIndex={-1}>
            <Suspense fallback={<RouteFallback />}>
              {menuQuery.isLoading || hasCurrentRouteAccess ? (
                <Outlet />
              ) : (
                <Result
                  status="403"
                  title="Sin permiso"
                  subTitle="Esta ruta no está habilitada en el menú asignado a su rol/oficina."
                  extra={
                    <Button type="primary" onClick={() => navigate('/inicio')}>
                      Ir al inicio
                    </Button>
                  }
                />
              )}
            </Suspense>
          </main>
          <footer className="credix-footer app-shell-footer">
            Soporte: 966900599 · © {new Date().getFullYear()} {branding.companyName}
          </footer>
        </div>
      </div>
    </div>
  )
}

function NavPanel({
  menuExpanded,
  menuQuery,
  menuItems,
  selectedKeys,
  openKeys,
  setOpenKeys,
  onMenuClick,
  onRetryMenu,
  quickActionsVisible,
  onQuickAction,
}: {
  menuExpanded: boolean
  menuQuery: { isLoading: boolean; isError: boolean }
  menuItems: ReturnType<typeof buildAntMenuItems>
  selectedKeys: string[]
  openKeys: string[]
  setOpenKeys: (keys: string[]) => void
  onMenuClick: (info: { key: string }) => void
  onRetryMenu: () => void
  quickActionsVisible: typeof quickActions
  onQuickAction: (qa: QuickAction) => void
}) {
  return (
    <div className="app-shell-sider-inner">
      <div className="app-shell-sider-brand">
        {menuExpanded ? (
          <BrandLogo compact showTagline={false} />
        ) : (
          <Text strong style={{ display: 'block', textAlign: 'center', fontSize: 11 }}>
            {branding.appShortName}
          </Text>
        )}
      </div>

      <div className="app-shell-sider-menu">
        {menuQuery.isLoading ? (
          <div style={{ padding: 24, textAlign: 'center' }}>
            <Spin />
          </div>
        ) : menuQuery.isError ? (
          <div style={{ padding: 16 }}>
            <Text type="danger" style={{ display: 'block', marginBottom: 8 }}>
              No se pudo cargar el menú.
            </Text>
            <Button size="small" onClick={onRetryMenu}>
              Reintentar
            </Button>
          </div>
        ) : (
          <Menu
            className="credix-menu"
            mode="inline"
            inlineCollapsed={!menuExpanded}
            items={menuItems}
            selectedKeys={selectedKeys}
            openKeys={menuExpanded ? openKeys : []}
            onOpenChange={(keys) => setOpenKeys(keys as string[])}
            onClick={onMenuClick}
            style={{ borderInlineEnd: 0 }}
          />
        )}
      </div>

      {menuExpanded && quickActionsVisible.length > 0 && (
        <div className="app-shell-sider-quick">
          <Text type="secondary" style={{ fontSize: 11, display: 'block', marginBottom: 6 }}>
            Accesos rápidos
          </Text>
          <div className="credix-quick-actions app-shell-quick-actions">
            {quickActionsVisible.map((qa) => (
              <Button key={qa.label} type="text" size="small" onClick={() => onQuickAction(qa)}>
                {qa.label}
              </Button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
