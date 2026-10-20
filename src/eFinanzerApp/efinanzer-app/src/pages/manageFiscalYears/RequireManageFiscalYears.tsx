import { Result, Spin } from 'antd';
import { Outlet, useParams } from 'react-router';
import { useUserAccess } from '../../common/stores/userAccess/useUserAccess';
import './ManageFiscalYears.css';

// Only users who may manage the fiscal years of this Haushalt see the page. The API checks it again for every request.
function RequireManageFiscalYears() {
  const { accountingUnitKey } = useParams<{ accountingUnitKey: string }>();
  const { userAccess, loading } = useUserAccess();

  // Keeps the page while the user access is reloaded after a change, so its state survives
  if (loading && !userAccess) return <Spin className="manage-fiscal-years__spinner" />;

  const accountingUnit = userAccess?.accountingUnits.find((unit) => unit.key === accountingUnitKey);
  if (!accountingUnit?.manageFiscalYears) {
    return (
      <Result
        status="403"
        title="Kein Zugriff"
        subTitle="Für die Verwaltung der Haushaltsjahre dieses Haushalts fehlt die Berechtigung."
      />
    );
  }

  return <Outlet />;
}

export default RequireManageFiscalYears;
