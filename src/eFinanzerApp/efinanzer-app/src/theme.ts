import type { ThemeConfig } from 'antd';

// Design tokens of the "Finanzverwaltung Studierendenschaft" design.
export const colors = {
  ink: '#17191C',
  inkSoft: '#5A5F66',
  paper: '#F4F3EE',
  border: '#DEDCD4',
  primary: '#0E5B52',
  primaryDark: '#08403A',
  navText: '#EDEBE4',
  navTextMuted: '#A9ADB3',
  navBorder: '#33373D',
};

export const fontFamily = "'Schibsted Grotesk', system-ui, sans-serif";

const theme: ThemeConfig = {
  token: {
    colorPrimary: colors.primary,
    colorLink: colors.primary,
    colorLinkHover: colors.primaryDark,
    colorText: colors.ink,
    colorTextSecondary: colors.inkSoft,
    colorBorder: '#C9C6BC',
    colorBorderSecondary: colors.border,
    colorBgLayout: colors.paper,
    fontFamily,
    fontSize: 15,
    borderRadius: 8,
    controlHeight: 44,
  },
  components: {
    Layout: {
      bodyBg: colors.paper,
      siderBg: colors.ink,
      headerBg: colors.ink,
      headerHeight: 56,
      headerPadding: '0 12px',
    },
    Menu: {
      darkItemBg: colors.ink,
      darkSubMenuItemBg: colors.ink,
      darkItemColor: colors.navText,
      darkItemHoverColor: '#FFFFFF',
      darkItemHoverBg: 'rgba(255, 255, 255, 0.06)',
      darkItemSelectedBg: colors.primary,
      darkItemSelectedColor: '#FFFFFF',
      itemHeight: 44,
      itemBorderRadius: 8,
      itemMarginInline: 0,
      itemMarginBlock: 2,
    },
    Drawer: {
      colorBgElevated: colors.ink,
    },
  },
};

export default theme;
