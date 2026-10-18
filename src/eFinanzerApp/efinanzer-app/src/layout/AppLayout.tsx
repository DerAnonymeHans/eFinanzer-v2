import { useState } from 'react';
import type { ReactNode } from 'react';
import { Button, Drawer, Grid, Layout, Typography } from 'antd';
import { MenuOutlined } from '@ant-design/icons';
import Logo from './Logo';
import SideNav, { budgets, fiscalYears } from './SideNav';
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
  const [budget, setBudget] = useState(budgets[0]);
  const [fiscalYear, setFiscalYear] = useState(fiscalYears[0]);

  const handleSelect = (key: string) => {
    setSelectedKey(key);
    setDrawerOpen(false);
  };

  const nav = (
    <SideNav
      selectedKey={selectedKey}
      onSelect={handleSelect}
      budget={budget}
      onBudgetChange={setBudget}
      fiscalYear={fiscalYear}
      onFiscalYearChange={setFiscalYear}
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
