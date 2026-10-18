import { ConfigProvider } from 'antd';
import deDE from 'antd/locale/de_DE';
import theme from './theme';
import AppLayout from './layout/AppLayout';

function App() {
  return (
    <ConfigProvider theme={theme} locale={deDE}>
      <AppLayout />
    </ConfigProvider>
  );
}

export default App;
