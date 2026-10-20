import {useCallback, useEffect, useRef, useState} from 'react';
import type {TableColumnsType} from 'antd';
import {Alert, Button, Card, Space, Table, Typography} from 'antd';
import {CheckOutlined, CloseOutlined} from '@ant-design/icons';
import apiClient from '../../../common/api/client';
import type {RoleChangeRequest, RoleChangeRequestAccountingUnit} from '../../../common/api/types';
import type {Resolution} from './ResolveRoleChangeRequestModal';
import ResolveRoleChangeRequestModal from './ResolveRoleChangeRequestModal';
import {useAuth} from '../../../common/auth/useAuth';
import RoleChangeList from '../../../common/components/roleChangeList/RoleChangeList';
import UserName from '../../../common/components/username/UserName';
import {formatDateTime} from '../../../common/texts/dateTime';
import {useUserAccess} from '../../../common/stores/userAccess/useUserAccess';
import './RoleChangeRequests.css';
import {OffsetPagination} from "../../../common/pagination/offsetPagination";
import {Page} from "../../../common/pagination/page";

const { Paragraph } = Typography;

/** The API allows at most 50 */
const PAGE_SIZES = [10, 20, 50];

interface LoadResult {
  /** The pagination the result was loaded for, a newer one means the page is still loading */
  requested: OffsetPagination;
  /** Differs from the requested one if the requested page does not exist (anymore) */
  shown: OffsetPagination;
  page?: Page<RoleChangeRequest>;
  error?: string;
}

async function fetchPage({ page, pageSize }: OffsetPagination): Promise<{ page?: Page<RoleChangeRequest>; error?: string }> {
  try {
    const { data, response } = await apiClient.GET('/api/manage-role-change-requests/list-pending-role-change-requests', {
      params: { query: { PageSize: pageSize, Offset: (page - 1) * pageSize } },
    });
    if (response.ok && data) return { page: { requests: data.items, totalCount: Number(data.totalCount) } };
    return { error: `Offene Rollenänderungsanträge konnten nicht geladen werden (HTTP ${response.status})` };
  } catch {
    return { error: 'Offene Rollenänderungsanträge konnten nicht geladen werden' };
  }
}

interface ModalState {
  /** Changes for every opening, so the dialog starts without the comment of an earlier opening */
  id: number;
  request: RoleChangeRequest;
  resolution: Resolution;
}

/** The pending role change requests of all users, which can be approved or dismissed here */
function PendingRoleChangeRequests() {
  const { username } = useAuth();
  const { reload: reloadUserAccess } = useUserAccess();

  const [pagination, setPagination] = useState<OffsetPagination>({ page: 1, pageSize: PAGE_SIZES[1] });
  const [loadResult, setLoadResult] = useState<LoadResult>();
  const [accountingUnits, setAccountingUnits] = useState<RoleChangeRequestAccountingUnit[]>([]);
  const [modalState, setModalState] = useState<ModalState>();
  const [modalOpen, setModalOpen] = useState(false);

  // Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
  const latestRequest = useRef(0);

  // Sets state only once the response is there, so it can run inside an effect
  const load = useCallback(async (requested: OffsetPagination) => {
    const request = ++latestRequest.current;

    let shown = requested;
    let result = await fetchPage(shown);

    // After the last request of a page was resolved, the page before is shown instead of an empty one
    const lastPage = Math.max(1, Math.ceil((result.page?.totalCount ?? 0) / requested.pageSize));
    if (result.page && requested.page > lastPage) {
      shown = { ...requested, page: lastPage };
      result = await fetchPage(shown);
    }

    if (request !== latestRequest.current) return;
    setLoadResult({ requested, shown, ...result });
  }, []);

  useEffect(() => {
    void load(pagination);
  }, [load, pagination]);

  // Only needed for the names of the accounting units, so the requests are still shown if it fails
  useEffect(() => {
    let cancelled = false;
    apiClient
      .GET('/api/manage-role-change-requests/list-accounting-units')
      .then(({ data, response }) => {
        if (!cancelled && response.ok && data) setAccountingUnits(data);
      })
      .catch(() => undefined);
    return () => {
      cancelled = true;
    };
  }, []);

  const loadedPage = loadResult?.page;
  const error = loadResult?.error;
  const loading = loadResult?.requested !== pagination;
  const shownPagination = loadResult?.shown ?? pagination;

  const getAccountingUnitName = (key: string) => accountingUnits.find((unit) => unit.key === key)?.name ?? key;

  const openModal = (request: RoleChangeRequest, resolution: Resolution) => {
    setModalState((current) => ({ id: (current?.id ?? 0) + 1, request, resolution }));
    setModalOpen(true);
  };

  const handleChanged = () => {
    // A new object, so the effect loads the shown page again
    setPagination({ ...shownPagination });
    // Resolving an own request changes the roles of the logged-in user
    if (modalState?.request.requestedBy === username) void reloadUserAccess();
  };

  const columns: TableColumnsType<RoleChangeRequest> = [
    {
      title: 'Antragsteller',
      key: 'requestedBy',
      render: (_, request) => <UserName username={request.requestedBy} />,
    },
    {
      title: 'Gestellt am',
      key: 'createdAt',
      render: (_, request) => <span className="role-change-requests__requested-at">{formatDateTime(request.createdAt)}</span>,
    },
    {
      title: 'Rollenänderungen',
      key: 'roleChanges',
      render: (_, request) => (
        <RoleChangeList roleChanges={request.roleChanges} getAccountingUnitName={getAccountingUnitName} />
      ),
    },
    {
      title: 'Begründung',
      key: 'requestComment',
      render: (_, request) =>
        request.requestComment && (
          <Paragraph className="role-change-requests__comment" ellipsis={{ rows: 3, tooltip: request.requestComment }}>
            {request.requestComment}
          </Paragraph>
        ),
    },
    {
      title: <span className="role-change-requests__visually-hidden">Aktionen</span>,
      key: 'actions',
      align: 'right',
      render: (_, request) => (
        <Space size={8}>
          <Button danger icon={<CloseOutlined />} onClick={() => openModal(request, 'Dismissed')}>
            Ablehnen
          </Button>
          <Button type="primary" icon={<CheckOutlined />} onClick={() => openModal(request, 'Approved')}>
            Annehmen
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <>
      <Card title="Offene Rollenänderungsanträge">
        {error ? (
          <Alert
            type="error"
            showIcon
            title={error}
            action={
              <Button size="small" onClick={() => void load(pagination)}>
                Erneut versuchen
              </Button>
            }
          />
        ) : (
          <Table
            rowKey="id"
            columns={columns}
            dataSource={loadedPage?.requests}
            loading={loading}
            locale={{ emptyText: 'Keine offenen Rollenänderungsanträge' }}
            scroll={{ x: 'max-content' }}
            pagination={{
              current: shownPagination.page,
              pageSize: shownPagination.pageSize,
              total: loadedPage?.totalCount ?? 0,
              pageSizeOptions: PAGE_SIZES,
              showSizeChanger: true,
              hideOnSinglePage: false,
              onChange: (page, pageSize) =>
                // A new page size starts on the first page, so the shown requests do not jump
                setPagination({ page: pageSize === shownPagination.pageSize ? page : 1, pageSize }),
            }}
          />
        )}
      </Card>

      {modalState && (
        <ResolveRoleChangeRequestModal
          key={modalState.id}
          open={modalOpen}
          request={modalState.request}
          resolution={modalState.resolution}
          getAccountingUnitName={getAccountingUnitName}
          onClose={() => setModalOpen(false)}
          onChanged={handleChanged}
        />
      )}
    </>
  );
}

export default PendingRoleChangeRequests;
