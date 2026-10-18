import { ConfigProvider } from 'antd';
import deDE from 'antd/locale/de_DE';
import theme from './theme';
import AppLayout from './layout/AppLayout';
import { AuthProvider } from './auth/AuthProvider';
import { UserAccessProvider } from './stores/UserAccessStore';

function App() {
  return (
    <ConfigProvider theme={theme} locale={deDE}>
      <AuthProvider>
        <UserAccessProvider>
          <AppLayout />
        </UserAccessProvider>
      </AuthProvider>
    </ConfigProvider>
  );
}

export default App;
