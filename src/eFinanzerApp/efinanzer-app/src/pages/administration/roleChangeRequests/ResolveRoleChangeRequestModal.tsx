import { useState } from 'react';
import { App, Input, Modal, Typography } from 'antd';
import apiClient from '../../../common/api/client';
import type { RoleChangeRequest, RoleChangeRequestResolutionStatus } from '../../../common/api/types';
import type { components } from '../../../common/api/schema';
import RoleChangeList from '../../../common/components/roleChangeList/RoleChangeList';
import UserName from '../../../common/components/username/UserName';
import { formatDateTime } from '../../../common/texts/dateTime';
import './RoleChangeRequests.css';

const { Text, Paragraph } = Typography;

export type Resolution = NonNullable<RoleChangeRequestResolutionStatus>;
type NotApplicableError = components['schemas']['RoleChangeRequestNotApplicableError'];

/** Same limit as the API */
const COMMENT_MAX_LENGTH = 500;

// Both conflicts share the status code, only the one of a request that cannot be applied has a reason
function isNotApplicableError(error: unknown): error is NotApplicableError {
  return typeof error === 'object' && error !== null && 'reason' in error;
}

interface ResolveRoleChangeRequestModalProps {
  open: boolean;
  request: RoleChangeRequest;
  resolution: Resolution;
  getAccountingUnitName: (key: string) => string;
  onClose: () => void;
  /** Called after the request was resolved, or when the shown request turned out to be outdated */
  onChanged: () => void;
}

// Remounted for every opening (see AdministrationPage), so no comment of an earlier opening is left
function ResolveRoleChangeRequestModal({
  open,
  request,
  resolution,
  getAccountingUnitName,
  onClose,
  onChanged,
}: ResolveRoleChangeRequestModalProps) {
  const { message } = App.useApp();
  const [comment, setComment] = useState('');
  const [saving, setSaving] = useState(false);
  const isApproval = resolution === 'Approved';
  const failureText = isApproval ? 'Antrag konnte nicht angenommen werden' : 'Antrag konnte nicht abgelehnt werden';

  const handleSubmit = async () => {
    setSaving(true);
    try {
      const { response, error } = await apiClient.PUT('/api/manage-role-change-requests/resolve-role-change-request', {
        params: { query: { id: request.id } },
        body: { resolution, comment: comment.trim() || null },
      });

      if (response.ok) {
        message.success(isApproval ? 'Antrag wurde angenommen' : 'Antrag wurde abgelehnt');
        onChanged();
        onClose();
      } else if (response.status === 409 && isNotApplicableError(error)) {
        message.error('Der Antrag kann nicht angewendet werden, eventuell gibt es den Benutzer oder einen Haushalt nicht mehr');
      } else if (response.status === 409) {
        message.error('Der Antrag wurde bereits entschieden');
        onChanged();
        onClose();
      } else if (response.status === 404) {
        message.error('Den Antrag gibt es nicht mehr');
        onChanged();
        onClose();
      } else if (response.status === 400) {
        message.error('Die Eingaben sind ungültig');
      } else {
        // The API reverts the decision if not all roles could be changed, so the request stays open
        message.error(`${failureText}, der Antrag bleibt offen (HTTP ${response.status})`);
      }
    } catch {
      message.error(failureText);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title={isApproval ? 'Antrag annehmen?' : 'Antrag ablehnen?'}
      open={open}
      onCancel={onClose}
      onOk={() => void handleSubmit()}
      okText={isApproval ? 'Annehmen' : 'Ablehnen'}
      okButtonProps={{ danger: !isApproval }}
      cancelText="Abbrechen"
      confirmLoading={saving}
      destroyOnHidden
    >
      <div className="role-change-requests__modal">
        <Text>
          Antrag von <UserName username={request.requestedBy} />, gestellt am {formatDateTime(request.createdAt)}.
          {isApproval ? ' Die Rollen werden sofort geändert.' : ' Die Rollen bleiben unverändert.'}
        </Text>

        <RoleChangeList roleChanges={request.roleChanges} getAccountingUnitName={getAccountingUnitName} />

        {request.requestComment && (
          <div className="role-change-requests__modal-field">
            <Text type="secondary">Begründung</Text>
            <Paragraph className="role-change-requests__comment">{request.requestComment}</Paragraph>
          </div>
        )}

        <div className="role-change-requests__modal-field">
          <label htmlFor="resolve-role-change-request-comment" className="role-change-requests__comment-label">
            Kommentar (optional)
          </label>
          <Input.TextArea
            id="resolve-role-change-request-comment"
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            maxLength={COMMENT_MAX_LENGTH}
            showCount
            autoSize={{ minRows: 2, maxRows: 6 }}
            autoFocus
          />
        </div>
      </div>
    </Modal>
  );
}

export default ResolveRoleChangeRequestModal;
