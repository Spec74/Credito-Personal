import type { ThemeConfig } from 'antd'

/** Tema Ant Design — moderno, marca CREDIX (#114885). */
export const credixLegacyTheme: ThemeConfig = {
  token: {
    colorPrimary: '#114885',
    colorInfo: '#2e69ae',
    colorSuccess: '#15803d',
    colorWarning: '#c47a00',
    colorError: '#b91c1c',
    colorBgLayout: '#f0f2f5',
    colorBgContainer: '#ffffff',
    colorBorder: '#e5e7eb',
    colorText: '#1f2937',
    colorTextSecondary: '#6b7280',
    borderRadius: 8,
    borderRadiusLG: 8,
    fontFamily:
      "'Segoe UI', system-ui, -apple-system, BlinkMacSystemFont, sans-serif",
    fontSize: 14,
    controlHeight: 36,
    lineHeight: 1.5,
  },
  components: {
    Layout: {
      siderBg: '#ffffff',
      headerBg: '#114885',
      bodyBg: '#f0f2f5',
    },
    Menu: {
      itemBg: 'transparent',
      subMenuItemBg: 'transparent',
      itemColor: '#374151',
      itemHoverColor: '#114885',
      itemSelectedColor: '#114885',
      itemSelectedBg: '#e8f2fc',
      itemHeight: 40,
      iconSize: 18,
      fontSize: 14,
    },
    Table: {
      headerBg: '#f9fafb',
      headerColor: '#1f2937',
      borderColor: '#e5e7eb',
      rowHoverBg: '#f0f7ff',
      fontSize: 13,
    },
    Button: {
      borderRadius: 8,
      controlHeight: 36,
    },
    Card: {
      borderRadiusLG: 8,
    },
    Drawer: {
      paddingLG: 0,
    },
  },
}
