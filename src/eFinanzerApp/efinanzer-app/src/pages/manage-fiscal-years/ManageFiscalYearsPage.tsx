import { useCallback, useEffect, useRef, useState } from 'react';
import { Alert, App, Button, Empty, Space, Spin, Table, Tag, Tooltip, Typography } from 'antd';
import type { TableColumnsType } from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { useParams } from 'react-router';
import apiClient from '../../common/api/client';
import type { FiscalYearListItem } from '../../common/api/types';
import { useUserAccess } from '../../common/stores/useUserAccess';
import FiscalYearModal from './FiscalYearModal';
import { formatDate, suggestNextPeriod } from './fiscalYearValidation';
import './ManageFiscalYears.css';

const { Title, Text } = Typography;

function hasBookings(fiscalYear: FiscalYearListItem) {
  return Number(fiscalYear.receiptsCount) > 0 || Number(fiscalYear.budgetsCount) > 0;
}

interface ModalState {
  /** Changes for every opening, so the dialog starts with fresh values */
  id: number;
  /** Undefined to create a new fiscal year */
  fiscalYear?: FiscalYearListItem;
}

function ManageFiscalYearsPage() {
  const { accountingUnitKey = '' } = useParams<{ accountingUnitKey: string }>();
  const { modal, message } = App.useApp();
  const { userAccess, reload: reloadUserAccess } = useUserAccess();
  const accountingUnit = userAccess?.accountingUnits.find((unit) => unit.key === accountingUnitKey);
  const accountingUnitName = accountingUnit?.name ?? accountingUnitKey;

  const [fiscalYears, setFiscalYears] = useState<FiscalYearListItem[]>();
  const [error, setError] = useState<string>();
  const [modalState, setModalState] = useState<ModalState>();
  const [modalOpen, setModalOpen] = useState(false);

  // Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
  const latestRequest = useRef(0);

  // Sets state only once the response is there, so it can run inside an effect
  const load = useCallback(async () => {
    const request = ++latestRequest.current;

    let result: FiscalYearListItem[] | undefined;
    let failure: string | undefined;
    try {
      const { data, response } = await apiClient.GET('/api/manage-fiscal-years/list-fiscal-years', {
        params: { query: { accountingUnitKey } },
      });
      if (response.ok && data) result = data;
      else if (response.status === 404) failure = `Den Haushalt „${accountingUnitKey}“ gibt es nicht`;
      else failure = `Haushaltsjahre konnten nicht geladen werden (HTTP ${response.status})`;
    } catch {
      failure = 'Haushaltsjahre konnten nicht geladen werden';
    }

    if (request !== latestRequest.current) return;
    setFiscalYears(result);
    setError(failure);
  }, [accountingUnitKey]);

  useEffect(() => {
    void load();
  }, [load]);

  // The side navigation lists the fiscal years too, so it is updated after every change
  const handleChanged = () => {
    void load();
    void reloadUserAccess();
  };

  const openModal = (fiscalYear?: FiscalYearListItem) => {
    setModalState((current) => ({ id: (current?.id ?? 0) + 1, fiscalYear }));
    setModalOpen(true);
  };

  const confirmDelete = (fiscalYear: FiscalYearListItem) => {
    const name = fiscalYear.name ?? fiscalYear.key;
    modal.confirm({
      title: `Haushaltsjahr „${name}“ löschen?`,
      content: 'Das kann nicht rückgängig gemacht werden.',
      okText: 'Löschen',
      okButtonProps: { danger: true },
      cancelText: 'Abbrechen',
      onOk: async () => {
        try {
          const { response } = await apiClient.DELETE('/api/manage-fiscal-years/delete-fiscal-year', {
            params: { query: { accountingUnitKey, key: fiscalYear.key } },
          });

          if (response.ok) {
            message.success(`Haushaltsjahr „${name}“ wurde gelöscht`);
            handleChanged();
          } else if (response.status === 409) {
            message.error('Das Haushaltsjahr hat noch Budgets oder Belege und kann daher nicht gelöscht werden');
            void load();
          } else if (response.status === 404) {
            message.error('Das Haushaltsjahr existiert nicht mehr');
            handleChanged();
          } else {
            message.error(`Haushaltsjahr konnte nicht gelöscht werden (HTTP ${response.status})`);
          }
        } catch {
          message.error('Haushaltsjahr konnte nicht gelöscht werden');
        }
      },
    });
  };

  const columns: TableColumnsType<FiscalYearListItem> = [
    {
      title: 'Name',
      key: 'name',
      render: (_, fiscalYear) => (
        <Space size={8} wrap>
          <Text strong>{fiscalYear.name ?? fiscalYear.key}</Text>
          {fiscalYear.name && <Tag>{fiscalYear.key}</Tag>}
        </Space>
      ),
    },
    {
      title: 'Zeitraum',
      key: 'period',
      render: (_, fiscalYear) => (
        <span className="manage-fiscal-years__period">
          {formatDate(fiscalYear.startDate)} – {formatDate(fiscalYear.endDate)}
        </span>
      ),
    },
    { title: 'Budgets', dataIndex: 'budgetsCount', align: 'right' },
    { title: 'Belege', dataIndex: 'receiptsCount', align: 'right' },
    {
      title: <span className="manage-fiscal-years__visually-hidden">Aktionen</span>,
      key: 'actions',
      align: 'right',
      render: (_, fiscalYear) => {
        const name = fiscalYear.name ?? fiscalYear.key;
        const deletable = !hasBookings(fiscalYear);
        return (
          <Space size={4}>
            <Button
              type="text"
              icon={<EditOutlined />}
              aria-label={`Haushaltsjahr ${name} bearbeiten`}
              title="Bearbeiten"
              onClick={() => openModal(fiscalYear)}
            />
            <Tooltip title={deletable ? 'Löschen' : 'Hat noch Budgets oder Belege und kann nicht gelöscht werden'}>
              <Button
                type="text"
                danger
                icon={<DeleteOutlined />}
                aria-label={`Haushaltsjahr ${name} löschen`}
                disabled={!deletable}
                onClick={() => confirmDelete(fiscalYear)}
              />
            </Tooltip>
          </Space>
        );
      },
    },
  ];

  return (
    <div className="manage-fiscal-years">
      <div className="manage-fiscal-years__header">
        <div>
          <Title level={2} className="manage-fiscal-years__title">
            Haushaltsjahre verwalten
          </Title>
          <Text type="secondary" className="manage-fiscal-years__subtitle">
            Haushalt {accountingUnitName}
          </Text>
        </div>
        <Button type="primary" icon={<PlusOutlined />} disabled={!fiscalYears} onClick={() => openModal()}>
          Haushaltsjahr anlegen
        </Button>
      </div>

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

      {!fiscalYears && !error && <Spin className="manage-fiscal-years__spinner" />}

      {fiscalYears?.length === 0 && <Empty description="Noch keine Haushaltsjahre angelegt" />}

      {fiscalYears && fiscalYears.length > 0 && (
        <Table
          rowKey="key"
          columns={columns}
          dataSource={fiscalYears}
          pagination={false}
          scroll={{ x: 'max-content' }}
        />
      )}

      {modalState && (
        <FiscalYearModal
          key={modalState.id}
          open={modalOpen}
          accountingUnitKey={accountingUnitKey}
          fiscalYear={modalState.fiscalYear}
          // The list is sorted by start date, newest first
          suggestedPeriod={suggestNextPeriod(fiscalYears?.[0]?.endDate)}
          onClose={() => setModalOpen(false)}
          onSaved={handleChanged}
        />
      )}
    </div>
  );
}

export default ManageFiscalYearsPage;
