import { Typography } from 'antd';
import PendingRoleChangeRequests from './roleChangeRequests/PendingRoleChangeRequests';
import './Administration.css';

const { Title } = Typography;

function AdministrationPage() {
  return (
    <div className="administration">
      <Title level={2} className="administration__title">
        Administration
      </Title>

      <PendingRoleChangeRequests />
    </div>
  );
}

export default AdministrationPage;
