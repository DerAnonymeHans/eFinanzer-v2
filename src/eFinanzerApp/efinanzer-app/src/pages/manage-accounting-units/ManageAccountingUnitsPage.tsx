import { useCallback, useEffect, useRef, useState } from 'react';
import { Alert, App, Button, Card, Empty, Spin, Tag, Typography } from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router';
import apiClient from '../../api/client';
import type { AccountingUnitListItem } from '../../api/types';
import { useUserAccess } from '../../stores/useUserAccess';
import CreateAccountingUnitModal from './CreateAccountingUnitModal';
import './ManageAccountingUnits.css';

const { Title, Paragraph } = Typography;

function ManageAccountingUnitsPage() {
  const { modal, message } = App.useApp();
  const navigate = useNavigate();
  const { reload: reloadUserAccess } = useUserAccess();

  const [accountingUnits, setAccountingUnits] = useState<AccountingUnitListItem[]>();
  const [error, setError] = useState<string>();
  const [createOpen, setCreateOpen] = useState(false);

  // Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
  const latestRequest = useRef(0);

  // Sets state only once the response is there, so it can run inside an effect
  const load = useCallback(async () => {
    const request = ++latestRequest.current;

    let result: AccountingUnitListItem[] | undefined;
    let failure: string | undefined;
    try {
      const { data, response } = await apiClient.GET('/api/manage-accounting-units/list-accounting-units');
      if (response.ok && data) result = data;
      else failure = `Haushalte konnten nicht geladen werden (HTTP ${response.status})`;
    } catch {
      failure = 'Haushalte konnten nicht geladen werden';
    }

    if (request !== latestRequest.current) return;
    setAccountingUnits(result);
    setError(failure);
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  // The side navigation lists the Haushalte too, so it is updated after every change
  const handleChanged = () => {
    void load();
    void reloadUserAccess();
  };

  const confirmDelete = (accountingUnit: AccountingUnitListItem) => {
    const name = accountingUnit.name ?? accountingUnit.key;
    modal.confirm({
      title: `Haushalt „${name}“ löschen?`,
      content: 'Alle Rollen der Benutzer in diesem Haushalt werden ebenfalls entfernt. Das kann nicht rückgängig gemacht werden.',
      okText: 'Löschen',
      okButtonProps: { danger: true },
      cancelText: 'Abbrechen',
      onOk: async () => {
        try {
          const { response } = await apiClient.DELETE('/api/manage-accounting-units/delete-accounting-unit', {
            params: { query: { key: accountingUnit.key } },
          });

          if (response.ok) {
            message.success(`Haushalt „${name}“ wurde gelöscht`);
            handleChanged();
          } else if (response.status === 409) {
            message.error('Der Haushalt hat noch Haushaltsjahre und kann daher nicht gelöscht werden');
          } else if (response.status === 404) {
            message.error('Der Haushalt existiert nicht mehr');
            handleChanged();
          } else {
            message.error(`Haushalt konnte nicht gelöscht werden (HTTP ${response.status})`);
          }
        } catch {
          message.error('Haushalt konnte nicht gelöscht werden');
        }
      },
    });
  };

  return (
    <div className="manage-units">
      <div className="manage-units__header">
        <Title level={2} className="manage-units__title">
          Haushalte verwalten
        </Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>
          Haushalt anlegen
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

      {!accountingUnits && !error && <Spin className="manage-units__spinner" />}

      {accountingUnits?.length === 0 && <Empty description="Noch keine Haushalte angelegt" />}

      {accountingUnits && accountingUnits.length > 0 && (
        <div className="manage-units__grid">
          {accountingUnits.map((accountingUnit) => {
            const name = accountingUnit.name ?? accountingUnit.key;
            return (
              <Card
                key={accountingUnit.key}
                className="manage-units__card"
                title={name}
                extra={<Tag>{accountingUnit.key}</Tag>}
                actions={[
                  <Button
                    key="edit"
                    type="text"
                    icon={<EditOutlined />}
                    aria-label={`Haushalt ${name} bearbeiten`}
                    onClick={() => navigate(`/haushalte-verwalten/${encodeURIComponent(accountingUnit.key)}`)}
                  >
                    Bearbeiten
                  </Button>,
                  <Button
                    key="delete"
                    type="text"
                    danger
                    icon={<DeleteOutlined />}
                    aria-label={`Haushalt ${name} löschen`}
                    onClick={() => confirmDelete(accountingUnit)}
                  >
                    Löschen
                  </Button>,
                ]}
              >
                <Paragraph
                  type={accountingUnit.description ? undefined : 'secondary'}
                  ellipsis={{ rows: 3, tooltip: accountingUnit.description }}
                  className="manage-units__description"
                >
                  {accountingUnit.description ?? 'Keine Beschreibung'}
                </Paragraph>
              </Card>
            );
          })}
        </div>
      )}

      <CreateAccountingUnitModal open={createOpen} onClose={() => setCreateOpen(false)} onCreated={handleChanged} />
    </div>
  );
}

export default ManageAccountingUnitsPage;
