import type { ReactNode } from 'react';
import { Card } from 'antd';
import { CheckCircleFilled, ClockCircleFilled, CloseCircleFilled } from '@ant-design/icons';
import type { RoleChangeRequest } from '../../../common/api/types';
import RoleChangeList from '../../../common/components/roleChangeList/RoleChangeList';
import UserName from '../../../common/components/username/UserName';
import { formatDateTime } from '../../../common/texts/dateTime';

const statuses = {
  pending: { label: 'Offen', icon: <ClockCircleFilled /> },
  approved: { label: 'Angenommen', icon: <CheckCircleFilled /> },
  dismissed: { label: 'Abgelehnt', icon: <CloseCircleFilled /> },
};

function getStatus(request: RoleChangeRequest): keyof typeof statuses {
  if (request.resolution === 'Approved') return 'approved';
  if (request.resolution === 'Dismissed') return 'dismissed';
  return 'pending';
}

/** A comment with its label, shown as a quote so it stands out from the generated texts */
function Comment({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="own-role-change-requests__field">
      <span className="own-role-change-requests__label">{label}</span>
      <p className="own-role-change-requests__comment">{children}</p>
    </div>
  );
}

interface RoleChangeRequestCardProps {
  request: RoleChangeRequest;
  /** Name of an accounting unit, the key if it is not known (anymore) */
  getAccountingUnitName: (key: string) => string;
  /** Shown in the header, e.g. to withdraw the request */
  extra?: ReactNode;
}

/** A role change request of the user with its changes and, once resolved, the decision */
function RoleChangeRequestCard({ request, getAccountingUnitName, extra }: RoleChangeRequestCardProps) {
  const status = getStatus(request);
  const { label, icon } = statuses[status];

  return (
    <Card
      size="small"
      type="inner"
      // The modifier sets the status color of the status tag and the decision comment
      className={`own-role-change-requests__card own-role-change-requests__card--${status}`}
      title={
        <span className="own-role-change-requests__request-title">
          <span className="own-role-change-requests__status">
            {icon}
            {label}
          </span>
          <span>Gestellt am {formatDateTime(request.createdAt)}</span>
        </span>
      }
      extra={extra}
    >
      <div className="own-role-change-requests__request">
        <RoleChangeList roleChanges={request.roleChanges} getAccountingUnitName={getAccountingUnitName} />

        {request.requestComment && <Comment label="Begründung">{request.requestComment}</Comment>}

        {request.resolvedAt && (
          <div className="own-role-change-requests__resolution">
            {/* The status tag in the header already shows the decision in color */}
            <div>
              <strong>{label}</strong>
              {request.resolvedBy && (
                <>
                  {' von '}
                  <UserName username={request.resolvedBy} />
                </>
              )}{' '}
              am {formatDateTime(request.resolvedAt)}
            </div>
            {request.resolutionComment && <Comment label="Kommentar">{request.resolutionComment}</Comment>}
          </div>
        )}
      </div>
    </Card>
  );
}

export default RoleChangeRequestCard;
