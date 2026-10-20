import { App as AntApp, ConfigProvider } from 'antd';
import deDE from 'antd/locale/de_DE';
import dayjs from 'dayjs';
import 'dayjs/locale/de';
import { BrowserRouter, Route, Routes } from 'react-router';
import theme from './theme';
import AppLayout from './common/layout/AppLayout';
import { AuthProvider } from './common/auth/AuthProvider';
import RequireManageAccountingUnits from './pages/manageAccountingUnits/RequireManageAccountingUnits';
import ManageAccountingUnitsPage from './pages/manageAccountingUnits/ManageAccountingUnitsPage';
import AccountingUnitPage from './pages/manageAccountingUnits/manageAccountingUnit/AccountingUnitPage';
import RequireManageFiscalYears from './pages/manageFiscalYears/RequireManageFiscalYears';
import ManageFiscalYearsPage from './pages/manageFiscalYears/ManageFiscalYearsPage';
import ProfilePage from "./pages/profilePage/ProfilePage";
import RequireAdministrationRights from './pages/administration/RequireAdministrationRights';
import AdministrationPage from './pages/administration/AdministrationPage';

// The date pickers start the week on Monday and show German month names, like the antd texts of deDE
dayjs.locale('de');

function App() {
  return (
    <ConfigProvider theme={theme} locale={deDE}>
      {/* Lets the pages open themed confirm dialogs and messages via App.useApp() */}
      <AntApp>
        <BrowserRouter>
          <AuthProvider>
            <AppLayout>
              <Routes>
                <Route path="haushalte-verwalten" element={<RequireManageAccountingUnits />}>
                  <Route index element={<ManageAccountingUnitsPage />} />
                  <Route path=":key" element={<AccountingUnitPage />} />
                </Route>
                <Route path="haushaltsjahre-verwalten/:accountingUnitKey" element={<RequireManageFiscalYears />}>
                  <Route index element={<ManageFiscalYearsPage />} />
                </Route>
                <Route path="administration" element={<RequireAdministrationRights />}>
                  <Route index element={<AdministrationPage />} />
                </Route>
                <Route path="profil">
                  <Route index element={<ProfilePage />} />
                </Route>
                {/* The other pages do not exist yet */}
                <Route path="*" element={null} />
              </Routes>
            </AppLayout>
          </AuthProvider>
        </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  );
}

export default App;
