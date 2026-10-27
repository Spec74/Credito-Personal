import { theme, type ThemeConfig } from 'antd'

/**
 * Modo oscuro Credix — no es inversión de pantalla.
 *
 * Criterios (WCAG 2.2 AA + APCA / Material 3):
 * - Lienzo #12151a, no #000: el negro puro produce halo y fatiga.
 * - Superficies un paso más claras que el canvas (elevación por tono, no por sombra).
 * - Texto #e6e8ec, no #fff: baja el deslumbramiento.
 * - Texto secundario #b4bcc8 ≥ 4.5:1 sobre canvas y cards.
 * - Marca #114885 se mantiene en cabecera y botones primarios (blanco sobre navy ≥ 8:1).
 * - Enlaces y selección usan #8cb6e8: #114885 sobre fondo oscuro no pasa contraste.
 * - Fotos y videos no se invierten.
 */
const LINK = '#8cb6e8'
const CANVAS = '#12151a'
const SURFACE = '#1b2028'
const ELEVATED = '#232a34'
const BORDER = '#3a4452'
const TEXT = '#e6e8ec'
const MUTED = '#b4bcc8'
const TERTIARY = '#9aa3b2'

export const credixDarkTheme: ThemeConfig = {
  algorithm: theme.darkAlgorithm,
  cssVar: {
    prefix: 'credix',
  },
  token: {
    colorPrimary: '#2e69ae',
    colorInfo: LINK,
    colorSuccess: '#4ade80',
    colorWarning: '#fbbf24',
    colorError: '#f87171',
    colorLink: LINK,
    colorBgLayout: CANVAS,
    colorBgContainer: SURFACE,
    colorBgElevated: ELEVATED,
    colorBorder: BORDER,
    colorBorderSecondary: '#2c3440',
    colorText: TEXT,
    colorTextSecondary: MUTED,
    colorTextTertiary: TERTIARY,
    colorFillAlter: '#1f2630',
    colorFillSecondary: '#252c36',
    borderRadius: 8,
    borderRadiusLG: 10,
    borderRadiusSM: 6,
    borderRadiusXS: 4,
    fontFamily:
      "'Inter', 'Segoe UI', system-ui, -apple-system, BlinkMacSystemFont, 'Helvetica Neue', Arial, sans-serif",
    fontFamilyCode: "'Roboto Mono', ui-monospace, 'Cascadia Mono', Consolas, monospace",
    fontSize: 14,
    fontSizeHeading1: 28,
    fontSizeHeading2: 22,
    fontSizeHeading3: 18,
    fontSizeHeading4: 16,
    fontSizeHeading5: 14,
    controlHeight: 36,
    controlHeightLG: 40,
    controlHeightSM: 28,
    lineHeight: 1.5,
    lineWidth: 1,
    motionDurationMid: '0.2s',
    motionDurationSlow: '0.28s',
    boxShadow: 'none',
    boxShadowSecondary: 'none',
    wireframe: false,
  },
  components: {
    Layout: {
      siderBg: SURFACE,
      headerBg: '#114885',
      bodyBg: CANVAS,
      headerHeight: 52,
    },
    Menu: {
      itemBg: 'transparent',
      subMenuItemBg: 'transparent',
      itemColor: '#d5dae2',
      itemHoverColor: LINK,
      itemSelectedColor: LINK,
      itemSelectedBg: 'rgba(140, 182, 232, 0.14)',
      itemHeight: 40,
      iconSize: 18,
      fontSize: 14,
      itemBorderRadius: 10,
      itemMarginInline: 8,
    },
    Table: {
      headerBg: '#1a2838',
      headerColor: LINK,
      headerSplitColor: BORDER,
      borderColor: BORDER,
      rowHoverBg: '#243044',
      rowSelectedBg: 'rgba(140, 182, 232, 0.12)',
      rowSelectedHoverBg: 'rgba(140, 182, 232, 0.18)',
      fontSize: 13,
      cellPaddingBlock: 10,
      cellPaddingInline: 12,
      headerBorderRadius: 8,
    },
    Button: {
      borderRadius: 8,
      controlHeight: 36,
      controlHeightLG: 40,
      controlHeightSM: 28,
      fontWeight: 600,
      primaryShadow: 'none',
      defaultShadow: 'none',
      dangerShadow: 'none',
      paddingContentHorizontal: 16,
    },
    Card: {
      borderRadiusLG: 10,
      paddingLG: 16,
    },
    Modal: {
      borderRadiusLG: 12,
      titleFontSize: 16,
      titleLineHeight: 1.35,
      contentBg: ELEVATED,
      headerBg: ELEVATED,
      footerBg: ELEVATED,
      paddingContentHorizontalLG: 24,
      paddingMD: 16,
    },
    Drawer: {
      paddingLG: 20,
    },
    Form: {
      labelColor: MUTED,
      labelFontSize: 13,
      labelHeight: 22,
      itemMarginBottom: 16,
      verticalLabelPadding: '0 0 4px',
      verticalLabelMargin: 0,
    },
    Input: {
      borderRadius: 8,
      activeBorderColor: LINK,
      hoverBorderColor: '#5b9bd6',
      activeShadow: '0 0 0 2px rgba(140, 182, 232, 0.22)',
      paddingBlock: 6,
      paddingInline: 11,
    },
    InputNumber: {
      borderRadius: 8,
      activeBorderColor: LINK,
      hoverBorderColor: '#5b9bd6',
      activeShadow: '0 0 0 2px rgba(140, 182, 232, 0.22)',
    },
    Select: {
      borderRadius: 8,
      optionSelectedBg: 'rgba(140, 182, 232, 0.16)',
      optionActiveBg: '#243044',
      optionSelectedFontWeight: 600,
      controlHeight: 36,
    },
    DatePicker: {
      borderRadius: 8,
      cellActiveWithRangeBg: 'rgba(140, 182, 232, 0.16)',
      cellHoverWithRangeBg: '#243044',
      cellRangeBorderColor: LINK,
      controlHeight: 36,
    },
    Checkbox: {
      borderRadiusSM: 4,
    },
    Radio: {
      buttonCheckedBg: '#2e69ae',
      buttonSolidCheckedBg: '#2e69ae',
    },
    Switch: {
      colorPrimary: '#2e69ae',
    },
    Tabs: {
      itemColor: MUTED,
      itemSelectedColor: LINK,
      itemHoverColor: LINK,
      inkBarColor: LINK,
      titleFontSize: 13,
      horizontalItemPadding: '10px 14px',
      cardBg: SURFACE,
    },
    Tag: {
      borderRadiusSM: 6,
      defaultBg: '#252c36',
      defaultColor: TEXT,
    },
    Alert: {
      borderRadiusLG: 8,
      withDescriptionPadding: '12px 16px',
      defaultPadding: '10px 14px',
    },
    Message: {
      contentBg: ELEVATED,
      contentPadding: '10px 16px',
    },
    Notification: {
      width: 384,
      borderRadiusLG: 10,
    },
    Pagination: {
      itemActiveBg: '#2e69ae',
      borderRadius: 6,
    },
    Tooltip: {
      borderRadius: 6,
      colorBgSpotlight: '#0d1014',
    },
    Popconfirm: {
      borderRadiusLG: 10,
    },
    Empty: {
      colorTextDescription: MUTED,
    },
    Spin: {
      colorPrimary: LINK,
    },
    Segmented: {
      itemSelectedBg: ELEVATED,
      itemSelectedColor: LINK,
      trackBg: '#151a21',
      borderRadius: 8,
      borderRadiusSM: 6,
    },
    Breadcrumb: {
      fontSize: 13,
      itemColor: MUTED,
      lastItemColor: LINK,
      linkColor: MUTED,
      linkHoverColor: LINK,
      separatorColor: TERTIARY,
    },
    Dropdown: {
      borderRadiusLG: 10,
      paddingBlock: 6,
      controlItemBgHover: '#243044',
      controlItemBgActive: 'rgba(140, 182, 232, 0.16)',
    },
    Progress: {
      defaultColor: LINK,
      remainingColor: '#2c3440',
    },
    Badge: {
      textFontSize: 11,
    },
    Statistic: {
      titleFontSize: 13,
      contentFontSize: 22,
    },
    Skeleton: {
      borderRadius: 6,
    },
  },
}
