import { Drawer, type DrawerProps } from 'antd'

export function CajaDrawer({ rootClassName, ...props }: DrawerProps) {
  return (
    <Drawer
      destroyOnClose
      {...props}
      rootClassName={['caja-drawer-root', rootClassName].filter(Boolean).join(' ')}
    />
  )
}
