import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Alert, App, Button, Card, Empty, Grid, Spin, Typography } from 'antd';
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import apiClient from '../../../common/api/client';
import type { AccountingUnitRole, RoleChangeRequest, RoleChangeRequestAccountingUnit } from '../../../common/api/types';
import { useUserAccess } from '../../../common/stores/userAccess/useUserAccess';
import RoleChangeRequestCard from './RoleChangeRequestCard';
import RoleChangeRequestModal from './RoleChangeRequestModal';
import './OwnRoleChangeRequests.css';

const { Title } = Typography;
const { useBreakpoint } = Grid;

interface LoadedData {
  /** Newest first */
  requests: RoleChangeRequest[];
  accountingUnits: RoleChangeRequestAccountingUnit[];
}

/** The role change requests of the logged-in user: the pending one, which can be withdrawn, and the resolved ones */
function OwnRoleChangeRequests() {
  const { modal, message } = App.useApp();
  const { userAccess } = useUserAccess();
  // useBreakpoint() is empty on the first render, so only treat an explicit `false` as mobile.
  // The button texts do not fit into the card header there, so only the icons are shown.
  const isMobile = useBreakpoint().md === false;

  const [data, setData] = useState<LoadedData>();
  const [error, setError] = useState<string>();
  /** Changes for every opening, so the dialog starts without the selection of an earlier opening */
  const [modalId, setModalId] = useState(0);
  const [modalOpen, setModalOpen] = useState(false);

  // Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
  const latestRequest = useRef(0);

  // Sets state only once the responses are there, so it can run inside an effect
  const load = useCallback(async () => {
    const request = ++latestRequest.current;

    let result: LoadedData | undefined;
    let failure: string | undefined;
    try {
      const [requests, accountingUnits] = await Promise.all([
        apiClient.GET('/api/manage-role-change-requests/list-own-role-change-requests'),
        apiClient.GET('/api/manage-role-change-requests/list-accounting-units'),
      ]);
      if (requests.response.ok && requests.data && accountingUnits.response.ok && accountingUnits.data) {
        result = { requests: requests.data, accountingUnits: accountingUnits.data };
      } else {
        const status = requests.response.ok ? accountingUnits.response.status : requests.response.status;
        failure = `Rollenänderungsanträge konnten nicht geladen werden (HTTP ${status})`;
      }
    } catch {
      failure = 'Rollenänderungsanträge konnten nicht geladen werden';
    }

    if (request !== latestRequest.current) return;
    setData(result);
    setError(failure);
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const currentRoles = useMemo(
    () => new Map<string, AccountingUnitRole[]>(
      userAccess?.accountingUnits.map((unit) => [unit.key, unit.accountingUnitRoles]) ?? [],
    ),
    [userAccess],
  );

  // Requests can name accounting units the user has no role in, so the names come from the list of all of them
  const getAccountingUnitName = (key: string) =>
    data?.accountingUnits.find((unit) => unit.key === key)?.name ?? key;

  const pendingRequest = data?.requests.find((request) => !request.resolution);
  const resolvedRequests = data?.requests.filter((request) => request.resolution) ?? [];

  const openModal = () => {
    setModalId((current) => current + 1);
    setModalOpen(true);
  };

  const confirmWithdraw = (request: RoleChangeRequest) => {
    modal.confirm({
      title: 'Antrag zurückziehen?',
      content: 'Der offene Rollenänderungsantrag wird gelöscht.',
      okText: 'Zurückziehen',
      okButtonProps: { danger: true },
      cancelText: 'Abbrechen',
      onOk: async () => {
        try {
          const { response } = await apiClient.DELETE('/api/manage-role-change-requests/delete-role-change-request', {
            params: { query: { id: request.id } },
          });

          if (response.ok) {
            message.success('Rollenänderungsantrag wurde zurückgezogen');
          } else if (response.status === 409) {
            message.error('Der Antrag wurde bereits entschieden');
          } else if (response.status === 404) {
            message.error('Den Antrag gibt es nicht mehr');
          } else {
            message.error(`Antrag konnte nicht zurückgezogen werden (HTTP ${response.status})`);
            return;
          }
          void load();
        } catch {
          message.error('Antrag konnte nicht zurückgezogen werden');
        }
      },
    });
  };

  return (
    <>
      <Card
        title="Rollenänderungsanträge"
        extra={
          data && !pendingRequest && (
            <Button
              type="primary"
              icon={<PlusOutlined />}
              aria-label={isMobile ? 'Neuer Rollenänderungsantrag' : undefined}
              title={isMobile ? 'Neuer Rollenänderungsantrag' : undefined}
              onClick={openModal}
            >
              {!isMobile && 'Neuer Rollenänderungsantrag'}
            </Button>
          )
        }
      >
        <div className="own-role-change-requests__requests">
          {error && (
            <Alert
              type="error"
              showIcon
              title={error}
              action={
                <Button size="small" onClick={() => void load()}>
                  Erneut versuchen
                </Button>
              }
            />
          )}

          {!data && !error && <Spin className="own-role-change-requests__spinner" />}

          {pendingRequest && (
            <RoleChangeRequestCard
              request={pendingRequest}
              getAccountingUnitName={getAccountingUnitName}
              extra={
                <Button
                  danger
                  type="text"
                  icon={<DeleteOutlined />}
                  aria-label={isMobile ? 'Zurückziehen' : undefined}
                  title={isMobile ? 'Zurückziehen' : undefined}
                  onClick={() => confirmWithdraw(pendingRequest)}
                >
                  {!isMobile && 'Zurückziehen'}
                </Button>
              }
            />
          )}

          {data && data.requests.length === 0 && <Empty description="Noch keine Rollenänderungsanträge gestellt" />}

          {resolvedRequests.length > 0 && (
            <>
              <Title level={5} className="own-role-change-requests__history-title">
                Frühere Anträge
              </Title>
              {resolvedRequests.map((request) => (
                <RoleChangeRequestCard key={request.id} request={request} getAccountingUnitName={getAccountingUnitName} />
              ))}
            </>
          )}
        </div>
      </Card>

      {data && modalId > 0 && (
        <RoleChangeRequestModal
          key={modalId}
          open={modalOpen}
          accountingUnits={data.accountingUnits}
          currentRoles={currentRoles}
          onClose={() => setModalOpen(false)}
          onChanged={() => void load()}
        />
      )}
    </>
  );
}

export default OwnRoleChangeRequests;
