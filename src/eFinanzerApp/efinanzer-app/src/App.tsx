import { App as AntApp, ConfigProvider } from 'antd';
import deDE from 'antd/locale/de_DE';
import dayjs from 'dayjs';
import 'dayjs/locale/de';
import { BrowserRouter, Route, Routes } from 'react-router';
import theme from './theme';
import AppLayout from './common/layout/AppLayout';
import { AuthProvider } from './common/auth/AuthProvider';
import { UserAccessProvider } from './common/stores/UserAccessStore';
import { UserStoreProvider } from './common/stores/UserStore';
import RequireManageAccountingUnits from './pages/manage-accounting-units/RequireManageAccountingUnits';
import ManageAccountingUnitsPage from './pages/manage-accounting-units/ManageAccountingUnitsPage';
import AccountingUnitPage from './pages/manage-accounting-units/AccountingUnitPage';
import RequireManageFiscalYears from './pages/manage-fiscal-years/RequireManageFiscalYears';
import ManageFiscalYearsPage from './pages/manage-fiscal-years/ManageFiscalYearsPage';
import ProfilePage from "./pages/profile-page/ProfilePage";
import RequireResolveRoleChangeRequests from './pages/administration/RequireResolveRoleChangeRequests';
import AdministrationPage from './pages/administration/AdministrationPage';

// The date pickers start the week on Monday and show German month names, like the antd texts of deDE
dayjs.locale('de');

function App() {
  return (
    <ConfigProvider theme={theme} locale={deDE}>
      {/* Above AntApp, so names can also be shown in its confirm dialogs */}
      <UserStoreProvider>
        {/* Lets the pages open themed confirm dialogs and messages via App.useApp() */}
        <AntApp>
          <BrowserRouter>
            <AuthProvider>
              <UserAccessProvider>
                <AppLayout>
                  <Routes>
                    <Route path="haushalte-verwalten" element={<RequireManageAccountingUnits />}>
                      <Route index element={<ManageAccountingUnitsPage />} />
                      <Route path=":key" element={<AccountingUnitPage />} />
                    </Route>
                    <Route path="haushaltsjahre-verwalten/:accountingUnitKey" element={<RequireManageFiscalYears />}>
                      <Route index element={<ManageFiscalYearsPage />} />
                    </Route>
                    <Route path="administration" element={<RequireResolveRoleChangeRequests />}>
                      <Route index element={<AdministrationPage />} />
                    </Route>
                    <Route path="profil">
                      <Route index element={<ProfilePage />} />
                    </Route>
                    {/* The other pages do not exist yet */}
                    <Route path="*" element={null} />
                  </Routes>
                </AppLayout>
              </UserAccessProvider>
            </AuthProvider>
          </BrowserRouter>
        </AntApp>
      </UserStoreProvider>
    </ConfigProvider>
  );
}

export default App;
