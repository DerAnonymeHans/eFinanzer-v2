import { Button, ConfigProvider, Divider, Menu, Select, Typography, theme } from 'antd';
import type { MenuProps } from 'antd';
import { EditOutlined } from '@ant-design/icons';
import { colors } from '../theme';
import Logo from './Logo';
import './AppLayout.css';

const { Text } = Typography;

// Fixed until the Haushalte and fiscal years are loaded from the API
export const budgets = ['FSR DIT', 'FSR IM', 'StuRa'];
export const fiscalYears = [2026, 2025, 2024];

const navItems: MenuProps['items'] = [
  { key: 'uebersicht', label: 'Übersicht' },
  { key: 'abrechnungen', label: 'Abrechnungen' },
  { key: 'haushaltsplan', label: 'Haushaltsplan' },
];

// Lets the selects blend into the dark navigation instead of rendering as white boxes
const navTheme = {
  algorithm: theme.darkAlgorithm,
  token: {
    colorBgContainer: '#22252A',
    colorBgElevated: '#22252A',
    colorBorder: colors.navBorder,
    colorText: colors.navText,
    colorTextPlaceholder: colors.navTextMuted,
  },
  components: {
    // Slimmer than the app-wide 44px, the selects are secondary to the menu
    Select: { controlHeight: 36, fontSize: 14 },
    Button: { controlHeightSM: 24 },
  },
};

interface SideNavProps {
  selectedKey: string;
  onSelect: (key: string) => void;
  budget: string;
  onBudgetChange: (budget: string) => void;
  fiscalYear: number;
  onFiscalYearChange: (fiscalYear: number) => void;
  onEditBudget?: () => void;
  onEditFiscalYear?: () => void;
}

// Content of the side navigation, shared by the desktop sider and the mobile drawer.
function SideNav({
  selectedKey,
  onSelect,
  budget,
  onBudgetChange,
  fiscalYear,
  onFiscalYearChange,
  onEditBudget,
  onEditFiscalYear,
}: SideNavProps) {
  return (
    <ConfigProvider theme={navTheme}>
      <nav className="side-nav" aria-label="Hauptnavigation">
        <div className="side-nav__brand">
          <Text className="side-nav__title">
            <Logo size={24} />
            eFinanzer
          </Text>
          <Text className="side-nav__subtitle">Finanzverwaltung der Studierendenschaft</Text>
        </div>

        <Divider className="side-nav__divider" />

        <div className="side-nav__context">
          <div className="side-nav__field">
            <div className="side-nav__label-row">
              <label htmlFor="side-nav-budget" className="side-nav__label">
                Haushalt
              </label>
              <Button
                type="text"
                size="small"
                className="side-nav__edit"
                icon={<EditOutlined />}
                aria-label="Haushalt bearbeiten"
                onClick={onEditBudget}
              />
            </div>
            <Select
              id="side-nav-budget"
              className="side-nav__select"
              value={budget}
              onChange={onBudgetChange}
              options={budgets.map((value) => ({ value, label: value }))}
            />
          </div>
          <div className="side-nav__field">
            <div className="side-nav__label-row">
              <label htmlFor="side-nav-fiscal-year" className="side-nav__label">
                Haushaltsjahr
              </label>
              <Button
                type="text"
                size="small"
                className="side-nav__edit"
                icon={<EditOutlined />}
                aria-label="Haushaltsjahr bearbeiten"
                onClick={onEditFiscalYear}
              />
            </div>
            <Select
              id="side-nav-fiscal-year"
              className="side-nav__select"
              value={fiscalYear}
              onChange={onFiscalYearChange}
              options={fiscalYears.map((value) => ({ value, label: value }))}
            />
          </div>
        </div>

        <Divider className="side-nav__divider" />

        <Menu
          className="side-nav__menu"
          theme="dark"
          mode="inline"
          inlineIndent={12}
          items={navItems}
          selectedKeys={[selectedKey]}
          onClick={({ key }) => onSelect(key)}
        />

        <div className="side-nav__user">
          <Text className="side-nav__user-name">[Name Nutzer:in]</Text>
          <Text className="side-nav__user-role">[Rolle]</Text>
        </div>
      </nav>
    </ConfigProvider>
  );
}

export default SideNav;
