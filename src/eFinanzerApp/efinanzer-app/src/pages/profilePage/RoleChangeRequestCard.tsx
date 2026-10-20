import type { ReactNode } from 'react';
import { Card, Tag, Typography } from 'antd';
import type { RoleChangeRequest } from '../../common/api/types';
import RoleChangeList from '../../common/components/roleChangeList/RoleChangeList';
import UserName from '../../common/components/username/UserName';
import { formatDateTime } from '../../common/texts/dateTime';

const { Text, Paragraph } = Typography;

function ResolutionTag({ request }: { request: RoleChangeRequest }) {
  if (request.resolution === 'Approved') return <Tag color="success">Angenommen</Tag>;
  if (request.resolution === 'Dismissed') return <Tag color="error">Abgelehnt</Tag>;
  return <Tag color="warning">Offen</Tag>;
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
  return (
    <Card
      size="small"
      title={
        <span className="profile__request-title">
          <ResolutionTag request={request} />
          <Text type="secondary">Gestellt am {formatDateTime(request.createdAt)}</Text>
        </span>
      }
      extra={extra}
    >
      <div className="profile__request">
        <RoleChangeList roleChanges={request.roleChanges} getAccountingUnitName={getAccountingUnitName} />

        {request.requestComment && (
          <div>
            <Text type="secondary">Begründung</Text>
            <Paragraph className="profile__comment">{request.requestComment}</Paragraph>
          </div>
        )}

        {request.resolvedAt && (
          <div className="profile__resolution">
            <Text type="secondary">
              {request.resolution === 'Approved' ? 'Angenommen' : 'Abgelehnt'}
              {request.resolvedBy && (
                <>
                  {' von '}
                  <UserName username={request.resolvedBy} />
                </>
              )}{' '}
              am {formatDateTime(request.resolvedAt)}
            </Text>
            {request.resolutionComment && (
              <Paragraph className="profile__comment">{request.resolutionComment}</Paragraph>
            )}
          </div>
        )}
      </div>
    </Card>
  );
}

export default RoleChangeRequestCard;
