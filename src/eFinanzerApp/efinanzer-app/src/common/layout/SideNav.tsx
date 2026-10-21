import { Button, ConfigProvider, Divider, Menu, Select, Typography, theme } from 'antd';
import type { MenuProps } from 'antd';
import { EditOutlined, LogoutOutlined } from '@ant-design/icons';
import type { AccountingUnit, FiscalYear } from '../api/types';
import { useAuth } from '../auth/useAuth';
import { getRoleNames } from '../texts/roles';
import { useUserAccess } from '../stores/userAccess/useUserAccess';
import { colors } from '../../theme';
import Logo from './Logo';
import './AppLayout.css';

const { Text } = Typography;

const MAX_SHOWN_ROLES = 2;

const ADMINISTRATION_KEY = 'administration';
const administrationItems: MenuProps['items'] = [{ key: ADMINISTRATION_KEY, label: 'Administration' }];

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
  /** Undefined while a page outside of the menu is shown */
  selectedPageKey: string | undefined;
  onPageKeySelect: (key: string) => void;
  accountingUnits: AccountingUnit[];
  accountingUnit: AccountingUnit | undefined;
  onAccountingUnitChange: (key: string) => void;
  fiscalYear: FiscalYear | undefined;
  onFiscalYearChange: (key: string) => void;
  onEditAccountingUnit?: () => void;
  onEditFiscalYear?: () => void;
  onOpenProfile?: () => void;
  administrationSelected?: boolean;
  onOpenAdministration?: () => void;
}

// Content of the side navigation, shared by the desktop sider and the mobile drawer.
function SideNav({
  selectedPageKey,
  onPageKeySelect,
  accountingUnits,
  accountingUnit,
  onAccountingUnitChange,
  fiscalYear,
  onFiscalYearChange,
  onEditAccountingUnit,
  onEditFiscalYear,
  onOpenProfile,
  administrationSelected = false,
  onOpenAdministration,
}: SideNavProps) {
  const { displayName, logout } = useAuth();
  const { userAccess, loading } = useUserAccess();
  const canEditAccountingUnits = userAccess?.manageAccountingUnits ?? false;
  const canEditFiscalYears = accountingUnit?.manageFiscalYears ?? false;
  const canAdministrate = userAccess?.resolveRoleChangeRequests ?? false;
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

        {/* Grouped, so the menu item sits closer to its dividers than the 20px between the sections */}
        <div className="side-nav__administration">
          <Divider className="side-nav__divider" />

          {canAdministrate && (
            <>
              <Menu
                className="side-nav__menu"
                theme="dark"
                mode="inline"
                inlineIndent={12}
                items={administrationItems}
                selectedKeys={administrationSelected ? [ADMINISTRATION_KEY] : []}
                onClick={() => onOpenAdministration?.()}
              />

              <Divider className="side-nav__divider" />
            </>
          )}
        </div>

        <div className="side-nav__context">
          <div className="side-nav__field">
            <div className="side-nav__label-row">
              <label htmlFor="side-nav-accounting-unit" className="side-nav__label">
                Haushalt
              </label>
              {canEditAccountingUnits && (
                <Button
                  type="text"
                  size="small"
                  className="side-nav__edit"
                  icon={<EditOutlined />}
                  aria-label="Haushalt bearbeiten"
                  onClick={onEditAccountingUnit}
                />
              )}
            </div>
            <Select
              id="side-nav-accounting-unit"
              className="side-nav__select"
              value={accountingUnit?.key}
              onChange={onAccountingUnitChange}
              loading={loading}
              disabled={accountingUnits.length === 0}
              placeholder={loading ? 'Wird geladen…' : 'Kein Haushalt verfügbar'}
              options={accountingUnits.map((unit) => ({ value: unit.key, label: unit.name ?? unit.key }))}
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
              disabled={!accountingUnit || accountingUnit.fiscalYears.length === 0}
              placeholder={loading ? 'Wird geladen…' : 'Kein Haushaltsjahr verfügbar'}
              options={accountingUnit?.fiscalYears.map((year) => ({ value: year.key, label: year.name ?? year.key }))}
            />
          </div>
        </div>

        <Menu
          className="side-nav__menu"
          theme="dark"
          mode="inline"
          inlineIndent={12}
          items={navItems}
          selectedKeys={selectedPageKey ? [selectedPageKey] : []}
          onClick={({ key }) => onPageKeySelect(key)}
        />

        <div className="side-nav__user">
          <button
            type="button"
            className="side-nav__user-details"
            aria-label={`Profil von ${displayName} öffnen`}
            title="Profil öffnen"
            onClick={onOpenProfile}
          >
            <Text className="side-nav__user-name" ellipsis>
              {displayName}
            </Text>
            <div className="side-nav__user-roles">
              {roleNames.map((line) => (
                <Text key={line} className="side-nav__user-role" ellipsis={{ tooltip: line }}>
                  {line}
                </Text>
              ))}
            </div>
          </button>
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
