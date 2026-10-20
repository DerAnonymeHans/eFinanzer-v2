import { Result, Spin } from 'antd';
import { Outlet } from 'react-router';
import { useUserAccess } from '../../common/stores/userAccess/useUserAccess';
import './Administration.css';

// Only users who may resolve role change requests see these pages. The API checks it again for every request.
function RequireAdministrationRights() {
  const { userAccess, loading } = useUserAccess();

  if (loading) return <Spin className="administration__spinner" />;

  if (!userAccess?.resolveRoleChangeRequests) {
    return (
      <Result
        status="403"
        title="Kein Zugriff"
        subTitle="Für die Administration fehlt die Berechtigung."
      />
    );
  }

  return <Outlet />;
}

export default RequireAdministrationRights;
