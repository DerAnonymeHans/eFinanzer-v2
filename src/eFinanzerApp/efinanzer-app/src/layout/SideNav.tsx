import { Button, ConfigProvider, Divider, Menu, Select, Typography, theme } from 'antd';
import type { MenuProps } from 'antd';
import { EditOutlined, LogoutOutlined } from '@ant-design/icons';
import type { AccountingGroup, FiscalYear } from '../api/types';
import { useAuth } from '../auth/useAuth';
import { getRoleNames } from '../constants/roles';
import { useUserAccess } from '../stores/useUserAccess';
import { colors } from '../theme';
import Logo from './Logo';
import './AppLayout.css';

const { Text } = Typography;

const MAX_SHOWN_ROLES = 2;

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
  /** Haushalte the user has access to */
  budgets: AccountingGroup[];
  budget: AccountingGroup | undefined;
  onBudgetChange: (key: string) => void;
  fiscalYear: FiscalYear | undefined;
  onFiscalYearChange: (key: string) => void;
  onEditBudget?: () => void;
  onEditFiscalYear?: () => void;
}

// Content of the side navigation, shared by the desktop sider and the mobile drawer.
function SideNav({
  selectedKey,
  onSelect,
  budgets,
  budget,
  onBudgetChange,
  fiscalYear,
  onFiscalYearChange,
  onEditBudget,
  onEditFiscalYear,
}: SideNavProps) {
  const { username, displayName, logout } = useAuth();
  const { userAccess, loading } = useUserAccess();
  const canEditBudgets = userAccess?.manageAccountingUnits ?? false;
  const canEditFiscalYears = budget?.manageFiscalYears ?? false;
  const roleNames = userAccess ? getRoleNames(userAccess).slice(0, MAX_SHOWN_ROLES) : [];

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
              {canEditBudgets && (
                <Button
                  type="text"
                  size="small"
                  className="side-nav__edit"
                  icon={<EditOutlined />}
                  aria-label="Haushalt bearbeiten"
                  onClick={onEditBudget}
                />
              )}
            </div>
            <Select
              id="side-nav-budget"
              className="side-nav__select"
              value={budget?.key}
              onChange={onBudgetChange}
              loading={loading}
              disabled={budgets.length === 0}
              placeholder={loading ? 'Wird geladen…' : 'Kein Haushalt verfügbar'}
              options={budgets.map((group) => ({ value: group.key, label: group.name ?? group.key }))}
            />
          </div>
          <div className="side-nav__field">
            <div className="side-nav__label-row">
              <label htmlFor="side-nav-fiscal-year" className="side-nav__label">
                Haushaltsjahr
              </label>
              {canEditFiscalYears && (
                <Button
                  type="text"
                  size="small"
                  className="side-nav__edit"
                  icon={<EditOutlined />}
                  aria-label="Haushaltsjahr bearbeiten"
                  onClick={onEditFiscalYear}
                />
              )}
            </div>
            <Select
              id="side-nav-fiscal-year"
              className="side-nav__select"
              value={fiscalYear?.key}
              onChange={onFiscalYearChange}
              loading={loading}
              disabled={!budget || budget.fiscalYears.length === 0}
              placeholder={loading ? 'Wird geladen…' : 'Kein Haushaltsjahr verfügbar'}
              options={budget?.fiscalYears.map((year) => ({ value: year.key, label: year.name ?? year.key }))}
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
          <div className="side-nav__user-details">
            <Text className="side-nav__user-name" ellipsis>
              {displayName}
            </Text>
            <div className="side-nav__user-roles">
              {/* The user name stands in until the roles are loaded, or if the user has none */}
              {(roleNames.length > 0 ? roleNames : [username]).map((line) => (
                <Text key={line} className="side-nav__user-role" ellipsis={{ tooltip: line }}>
                  {line}
                </Text>
              ))}
            </div>
          </div>
          <Button
            type="text"
            className="side-nav__logout"
            icon={<LogoutOutlined />}
            aria-label="Abmelden"
            title="Abmelden"
            onClick={() => void logout()}
          />
        </div>
      </nav>
    </ConfigProvider>
  );
}

export default SideNav;
