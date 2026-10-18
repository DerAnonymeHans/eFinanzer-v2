import { useCallback, useEffect, useRef, useState } from 'react';
import { Alert, App, Button, Card, Empty, Form, Input, Select, Space, Spin, Typography } from 'antd';
import { ArrowLeftOutlined, DeleteOutlined, SaveOutlined, UserAddOutlined } from '@ant-design/icons';
import { Link, useParams } from 'react-router';
import apiClient from '../../api/client';
import type { AccountingUnitDetails, AccountingUnitRole, User, UserRoles } from '../../api/types';
import UserName from '../../components/UserName';
import { accountingUnitRoleNames } from '../../constants/roles';
import UserSearchSelect from '../../components/UserSearchSelect';
import { useUserDisplayName } from '../../stores/useUser';
import { useUserAccess } from '../../stores/useUserAccess';
import {
  DESCRIPTION_MAX_LENGTH,
  NAME_MAX_LENGTH,
  descriptionRules,
  emptyToNull,
  nameRules,
} from './accountingUnitValidation';
import './ManageAccountingUnits.css';

const { Title, Text } = Typography;

const roleOptions = (Object.keys(accountingUnitRoleNames) as AccountingUnitRole[]).map((role) => ({
  value: role,
  label: accountingUnitRoleNames[role],
}));

interface UserRowProps {
  user: UserRoles;
  onRolesChange: (roles: AccountingUnitRole[]) => void;
  onRemove: () => void;
}

// Shows the full name, the cryptic user name is only used as key and id
function UserRow({ user, onRolesChange, onRemove }: UserRowProps) {
  const displayName = useUserDisplayName(user.userName);
  const labelId = `roles-${user.userName}`;

  return (
    <div className="manage-units__user">
      <Text strong className="manage-units__user-name" id={labelId}>
        <UserName username={user.userName} />
      </Text>
      <Select
        mode="multiple"
        className="manage-units__user-roles"
        aria-labelledby={labelId}
        value={user.roles}
        onChange={onRolesChange}
        options={roleOptions}
        placeholder="Keine Rolle"
        allowClear
      />
      <Button
        type="text"
        danger
        icon={<DeleteOutlined />}
        aria-label={`${displayName} entfernen`}
        title="Entfernen"
        onClick={onRemove}
      />
    </div>
  );
}

interface GeneralInfoFormValues {
  name?: string;
  description?: string;
}

function AccountingUnitPage() {
  const { key = '' } = useParams<{ key: string }>();
  const { modal, message } = App.useApp();
  const { reload: reloadUserAccess } = useUserAccess();
  const [form] = Form.useForm<GeneralInfoFormValues>();

  const [accountingUnit, setAccountingUnit] = useState<AccountingUnitDetails>();
  const [error, setError] = useState<string>();
  const [users, setUsers] = useState<UserRoles[]>([]);
  const [newUser, setNewUser] = useState<User>();

  // Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
  const latestRequest = useRef(0);

  // Sets state only once the response is there, so it can run inside an effect
  const load = useCallback(async () => {
    const request = ++latestRequest.current;

    let result: AccountingUnitDetails | undefined;
    let failure: string | undefined;
    try {
      const { data, response } = await apiClient.GET('/api/manage-accounting-units/get-accounting-unit', {
        params: { query: { key } },
      });
      if (response.ok && data) result = data;
      else if (response.status === 404) failure = `Den Haushalt „${key}“ gibt es nicht`;
      else failure = `Haushalt konnte nicht geladen werden (HTTP ${response.status})`;
    } catch {
      failure = 'Haushalt konnte nicht geladen werden';
    }

    if (request !== latestRequest.current) return;
    setAccountingUnit(result);
    setError(failure);
    setUsers(result?.usersWithRoles ?? []);
  }, [key]);

  useEffect(() => {
    void load();
  }, [load]);

  // The form is only rendered once the accounting unit is loaded, so its values are set after that render
  useEffect(() => {
    if (accountingUnit) {
      form.setFieldsValue({ name: accountingUnit.name ?? undefined, description: accountingUnit.description ?? undefined });
    }
  }, [accountingUnit, form]);

  const setUserRoles = (userName: string, roles: AccountingUnitRole[]) =>
    setUsers((current) => current.map((user) => (user.userName === userName ? { ...user, roles } : user)));

  const removeUser = (userName: string) =>
    setUsers((current) => current.filter((user) => user.userName !== userName));

  // The search leaves out users that are already listed, so the selected one can be added as it is
  const addUser = () => {
    if (!newUser) return;
    setUsers((current) => [...current, { userName: newUser.username, roles: [] }]);
    setNewUser(undefined);
  };

  const save = async (values: GeneralInfoFormValues) => {
    try {
      const { response } = await apiClient.PUT('/api/manage-accounting-units/update-accounting-unit', {
        params: { query: { key } },
        body: {
          name: emptyToNull(values.name),
          description: emptyToNull(values.description),
          usersWithRoles: users,
        },
      });

      if (response.ok) {
        message.success('Änderungen wurden gespeichert');
        void load();
        // The side navigation shows the name of the Haushalt and the roles of the logged-in user
        void reloadUserAccess();
      } else if (response.status === 400) {
        message.error('Die Eingaben sind ungültig, eventuell gibt es einen Benutzer nicht');
      } else if (response.status === 404) {
        message.error('Der Haushalt existiert nicht mehr');
      } else {
        // Role changes are applied one after another, so some of them may have been saved
        message.error(`Änderungen konnten nicht vollständig gespeichert werden (HTTP ${response.status})`);
        void load();
      }
    } catch {
      message.error('Änderungen konnten nicht gespeichert werden');
    }
  };

  const confirmSave = async () => {
    let values: GeneralInfoFormValues;
    try {
      values = await form.validateFields();
    } catch {
      return;
    }

    const usersWithoutRoles = users.filter((user) => user.roles.length === 0);
    modal.confirm({
      title: 'Änderungen speichern?',
      content: (
        <>
          <p>Die allgemeinen Angaben und die Rollen der Benutzer werden übernommen.</p>
          {usersWithoutRoles.length > 0 && (
            <p>
              Ohne Rolle und damit aus dem Haushalt entfernt werden:{' '}
              {usersWithoutRoles.map((user, index) => (
                <span key={user.userName}>
                  {index > 0 && ', '}
                  <UserName username={user.userName} />
                </span>
              ))}
            </p>
          )}
        </>
      ),
      okText: 'Speichern',
      cancelText: 'Abbrechen',
      onOk: () => save(values),
    });
  };

  const backLink = (
    <Link to="/haushalte-verwalten" className="manage-units__back">
      <ArrowLeftOutlined /> Alle Haushalte
    </Link>
  );

  if (error) {
    return (
      <div className="manage-units">
        {backLink}
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
      </div>
    );
  }

  if (!accountingUnit) return <Spin className="manage-units__spinner" />;

  return (
    <div className="manage-units">
      {backLink}
      <div className="manage-units__header">
        <Title level={2} className="manage-units__title">
          {accountingUnit.name ?? accountingUnit.key}
        </Title>
        <Button type="primary" icon={<SaveOutlined />} onClick={() => void confirmSave()}>
          Speichern
        </Button>
      </div>

      <Card title="Allgemein">
        <Form form={form} layout="vertical" className="manage-units__form">
          <Form.Item label="Schlüssel">
            <Input value={accountingUnit.key} disabled />
          </Form.Item>
          <Form.Item name="name" label="Name" rules={nameRules}>
            <Input maxLength={NAME_MAX_LENGTH} />
          </Form.Item>
          <Form.Item name="description" label="Beschreibung" rules={descriptionRules}>
            <Input.TextArea maxLength={DESCRIPTION_MAX_LENGTH} showCount autoSize={{ minRows: 2, maxRows: 6 }} />
          </Form.Item>
        </Form>
      </Card>

      <Card title="Benutzer und Rollen">
        <div className="manage-units__users">
          {users.length === 0 && <Empty description="Noch keine Benutzer in diesem Haushalt" />}

          {users.map((user) => (
            <UserRow
              key={user.userName}
              user={user}
              onRolesChange={(roles) => setUserRoles(user.userName, roles)}
              onRemove={() => removeUser(user.userName)}
            />
          ))}

          <Space.Compact className="manage-units__add-user">
            <UserSearchSelect
              className="manage-units__user-search"
              aria-label="Benutzer suchen"
              value={newUser}
              onChange={setNewUser}
              excludeUsernames={users.map((user) => user.userName)}
            />
            <Button icon={<UserAddOutlined />} disabled={!newUser} onClick={addUser}>
              Hinzufügen
            </Button>
          </Space.Compact>
        </div>
      </Card>
    </div>
  );
}

export default AccountingUnitPage;
