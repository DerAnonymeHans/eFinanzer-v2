import { useState } from 'react';
import type { ReactNode } from 'react';
import { Button, Drawer, Grid, Layout, Typography } from 'antd';
import { MenuOutlined } from '@ant-design/icons';
import { useLocation, useNavigate } from 'react-router';
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
  const [selectedKey, setSelectedKey] = useState('uebersicht');
  const navigate = useNavigate();
  // The menu entries are not routed yet, so they only count as selected outside of the routed pages
  const onRoutedPage = useLocation().pathname !== '/';
  const [budgetKey, setBudgetKey] = useState<string>();
  const [fiscalYearKey, setFiscalYearKey] = useState<string>();

  // Only Haushalte and fiscal years the user has access to can be selected. Without a valid choice the first one is
  // used, e.g. right after the user access was loaded.
  const { userAccess } = useUserAccess();
  const budgets = userAccess?.accountingGroups ?? [];
  const budget = budgets.find((group) => group.key === budgetKey) ?? budgets[0];
  const fiscalYear = budget?.fiscalYears.find((year) => year.key === fiscalYearKey) ?? budget?.fiscalYears[0];

  const handleBudgetChange = (key: string) => {
    setBudgetKey(key);
    // Every Haushalt has its own fiscal years
    setFiscalYearKey(undefined);
  };

  const handleSelect = (key: string) => {
    setSelectedKey(key);
    setDrawerOpen(false);
    navigate('/');
  };

  const handleEditBudget = () => {
    setDrawerOpen(false);
    navigate('/haushalte-verwalten');
  };

  const nav = (
    <SideNav
      selectedKey={onRoutedPage ? undefined : selectedKey}
      onSelect={handleSelect}
      budgets={budgets}
      budget={budget}
      onBudgetChange={handleBudgetChange}
      fiscalYear={fiscalYear}
      onFiscalYearChange={setFiscalYearKey}
      onEditBudget={handleEditBudget}
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
