import { Result, Spin } from 'antd';
import { Outlet } from 'react-router';
import { useUserAccess } from '../../stores/useUserAccess';
import './ManageAccountingUnits.css';

// Only users who may manage accounting units see these pages. The API checks it again for every request.
function RequireManageAccountingUnits() {
  const { userAccess, loading } = useUserAccess();

  if (loading) return <Spin className="manage-units__spinner" />;

  if (!userAccess?.manageAccountingUnits) {
    return (
      <Result
        status="403"
        title="Kein Zugriff"
        subTitle="Für die Verwaltung der Haushalte fehlt die Berechtigung."
      />
    );
  }

  return <Outlet />;
}

export default RequireManageAccountingUnits;
