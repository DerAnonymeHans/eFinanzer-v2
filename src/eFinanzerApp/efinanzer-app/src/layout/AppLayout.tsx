import { useState } from 'react';
import type { ReactNode } from 'react';
import { Button, Drawer, Grid, Layout, Typography } from 'antd';
import { MenuOutlined } from '@ant-design/icons';
import { useLocation, useMatch, useNavigate } from 'react-router';
import Logo from './Logo';
import { useUserAccess } from '../stores/useUserAccess';
import SideNav from './SideNav';
import './AppLayout.css';

const { Header, Sider, Content } = Layout;
const { useBreakpoint } = Grid;

const SIDER_WIDTH = 240;

interface AppLayoutProps {
  children?: ReactNode;
}

function AppLayout({ children }: AppLayoutProps) {
  const screens = useBreakpoint();
  // useBreakpoint() is empty on the first render, so only treat an explicit `false` as mobile.
  const isMobile = screens.md === false;

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [selectedPageKey, setSelectedPageKey] = useState('uebersicht');
  const navigate = useNavigate();
  // The menu entries are not routed yet, so they only count as selected outside of the routed pages
  const onRoutedPage = useLocation().pathname !== '/';
  // The fiscal year page belongs to one Haushalt, which is then also the one selected in the navigation
  const fiscalYearsPage = useMatch('/haushaltsjahre-verwalten/:accountingUnitKey');
  const [accountingUnitKey, setAccountingUnitKey] = useState<string>();
  const [fiscalYearKey, setFiscalYearKey] = useState<string>();

  // Only Haushalte and fiscal years the user has access to can be selected. Without a valid choice the first one is
  // used, e.g. right after the user access was loaded.
  const { userAccess } = useUserAccess();
  const accountingUnits = userAccess?.accountingUnits ?? [];
  const selectedAccountingUnitKey = fiscalYearsPage?.params.accountingUnitKey ?? accountingUnitKey;
  const accountingUnit = accountingUnits.find((unit) => unit.key === selectedAccountingUnitKey) ?? accountingUnits[0];
  const fiscalYear = accountingUnit?.fiscalYears.find((year) => year.key === fiscalYearKey) ?? accountingUnit?.fiscalYears[0];

  const handleAccountingUnitChange = (key: string) => {
    setAccountingUnitKey(key);
    // Every Haushalt has its own fiscal years
    setFiscalYearKey(undefined);
    // Switches to the fiscal years of the new Haushalt, if the user may manage them
    if (fiscalYearsPage) {
      const canManage = accountingUnits.find((unit) => unit.key === key)?.manageFiscalYears ?? false;
      navigate(canManage ? `/haushaltsjahre-verwalten/${encodeURIComponent(key)}` : '/');
    }
  };

  const handlePageKeySelect = (key: string) => {
    setSelectedPageKey(key);
    setDrawerOpen(false);
    navigate('/');
  };

  const handleEditAccountingUnit = () => {
    setDrawerOpen(false);
    navigate('/haushalte-verwalten');
  };

  const handleEditFiscalYear = () => {
    if (!accountingUnit) return;
    // Keeps the Haushalt selected after leaving the page
    setAccountingUnitKey(accountingUnit.key);
    setDrawerOpen(false);
    navigate(`/haushaltsjahre-verwalten/${encodeURIComponent(accountingUnit.key)}`);
  };

  const nav = (
    <SideNav
      selectedPageKey={onRoutedPage ? undefined : selectedPageKey}
      onPageKeySelect={handlePageKeySelect}
      accountingUnits={accountingUnits}
      accountingUnit={accountingUnit}
      onAccountingUnitChange={handleAccountingUnitChange}
      fiscalYear={fiscalYear}
      onFiscalYearChange={setFiscalYearKey}
      onEditAccountingUnit={handleEditAccountingUnit}
      onEditFiscalYear={handleEditFiscalYear}
    />
  );

  return (
    <Layout className="app-layout">
      {isMobile ? (
        <Drawer
          className="app-layout__drawer"
          placement="left"
          size={SIDER_WIDTH + 40}
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          closable={false}
          styles={{ body: { padding: 0 } }}
        >
          {nav}
        </Drawer>
      ) : (
        <Sider className="app-layout__sider" width={SIDER_WIDTH}>
          {nav}
        </Sider>
      )}

      <Layout>
        {isMobile && (
          <Header className="app-layout__header">
            <Button
              type="text"
              className="app-layout__menu-button"
              icon={<MenuOutlined />}
              aria-label="Navigation öffnen"
              onClick={() => setDrawerOpen(true)}
            />
            <Typography.Text className="app-layout__header-title">
              <Logo size={22} />
              eFinanzer
            </Typography.Text>
          </Header>
        )}

        <Content className="app-layout__content">{children}</Content>
      </Layout>
    </Layout>
  );
}

export default AppLayout;
