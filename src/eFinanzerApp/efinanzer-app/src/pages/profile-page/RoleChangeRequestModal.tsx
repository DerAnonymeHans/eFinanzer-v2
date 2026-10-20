import { useState } from 'react';
import { Alert, App, Checkbox, Input, Modal, Table, Typography } from 'antd';
import type { TableColumnsType } from 'antd';
import apiClient from '../../common/api/client';
import type { AccountingUnitRole, RoleChange, RoleChangeRequestAccountingUnit } from '../../common/api/types';
import { accountingUnitRoleNames } from '../../common/constants/roles';

const { Title, Text } = Typography;

/** Same limit as the API */
const COMMENT_MAX_LENGTH = 500;

const roles = Object.keys(accountingUnitRoleNames) as AccountingUnitRole[];

const roleKey = (accountingUnitKey: string, role: AccountingUnitRole) => `${accountingUnitKey}/${role}`;

interface RoleTableProps {
  caption: string;
  accountingUnits: RoleChangeRequestAccountingUnit[];
  selected: Set<string>;
  onToggle: (key: string) => void;
  /** Whether the role can be chosen in this table */
  isSelectable: (accountingUnitKey: string, role: AccountingUnitRole) => boolean;
  /** Reason shown on the roles that cannot be chosen */
  notSelectableReason: string;
  /** Whether the roles that cannot be chosen are shown as checked */
  notSelectableChecked: boolean;
}

/** One row per accounting unit, one checkbox per role */
function RoleTable({
  caption,
  accountingUnits,
  selected,
  onToggle,
  isSelectable,
  notSelectableReason,
  notSelectableChecked,
}: RoleTableProps) {
  const columns: TableColumnsType<RoleChangeRequestAccountingUnit> = [
    {
      title: 'Haushalt',
      key: 'name',
      render: (_, unit) => unit.name ?? unit.key,
    },
    ...roles.map((role) => ({
      title: accountingUnitRoleNames[role],
      key: role,
      align: 'center' as const,
      render: (_: unknown, unit: RoleChangeRequestAccountingUnit) => {
        const key = roleKey(unit.key, role);
        const selectable = isSelectable(unit.key, role);
        return (
          <Checkbox
            aria-label={`${caption}: ${unit.name ?? unit.key} · ${accountingUnitRoleNames[role]}`}
            title={selectable ? undefined : notSelectableReason}
            disabled={!selectable}
            checked={selectable ? selected.has(key) : notSelectableChecked}
            onChange={() => onToggle(key)}
          />
        );
      },
    })),
  ];

  return (
    <Table
      size="small"
      rowKey="key"
      columns={columns}
      dataSource={accountingUnits}
      pagination={false}
      scroll={{ x: 'max-content', y: 260 }}
    />
  );
}

interface RoleChangeRequestModalProps {
  open: boolean;
  accountingUnits: RoleChangeRequestAccountingUnit[];
  /** Roles the user currently has, per accounting unit key */
  currentRoles: Map<string, AccountingUnitRole[]>;
  onClose: () => void;
  /** Called after the request was created, or when the shown data turned out to be outdated */
  onChanged: () => void;
}

// Remounted for every opening (see ProfilePage), so no selection of an earlier opening is left
function RoleChangeRequestModal({ open, accountingUnits, currentRoles, onClose, onChanged }: RoleChangeRequestModalProps) {
  const { message } = App.useApp();
  const [added, setAdded] = useState(() => new Set<string>());
  const [removed, setRemoved] = useState(() => new Set<string>());
  const [comment, setComment] = useState('');
  const [showEmptyError, setShowEmptyError] = useState(false);
  const [saving, setSaving] = useState(false);

  const hasRole = (accountingUnitKey: string, role: AccountingUnitRole) =>
    currentRoles.get(accountingUnitKey)?.includes(role) ?? false;

  const toggle = (setSelection: typeof setAdded) => (key: string) => {
    setShowEmptyError(false);
    setSelection((current) => {
      const next = new Set(current);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  };

  // Ordered like the tables, so the request reads the same way as it was entered
  const getRoleChanges = (): RoleChange[] =>
    accountingUnits.flatMap((unit) =>
      roles.flatMap((role): RoleChange[] => {
        const key = roleKey(unit.key, role);
        if (added.has(key)) return [{ accountingUnitKey: unit.key, accountingUnitRole: role, isAdded: true }];
        if (removed.has(key)) return [{ accountingUnitKey: unit.key, accountingUnitRole: role, isAdded: false }];
        return [];
      }),
    );

  const handleSubmit = async () => {
    const roleChanges = getRoleChanges();
    if (roleChanges.length === 0) {
      setShowEmptyError(true);
      return;
    }

    const failureText = 'Rollenänderungsantrag konnte nicht gestellt werden';
    setSaving(true);
    try {
      const { response } = await apiClient.POST('/api/manage-role-change-requests/create-role-change-request', {
        body: { comment: comment.trim() || null, roleChanges },
      });

      if (response.ok) {
        message.success('Rollenänderungsantrag wurde gestellt');
        onChanged();
        onClose();
      } else if (response.status === 409) {
        message.error('Es gibt bereits einen offenen Rollenänderungsantrag');
        onChanged();
        onClose();
      } else if (response.status === 404) {
        message.error('Einen der Haushalte gibt es nicht mehr');
        onChanged();
        onClose();
      } else if (response.status === 400) {
        message.error('Die Eingaben sind ungültig');
      } else {
        message.error(`${failureText} (HTTP ${response.status})`);
      }
    } catch {
      message.error(failureText);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title="Neuer Rollenänderungsantrag"
      open={open}
      onCancel={onClose}
      onOk={() => void handleSubmit()}
      okText="Antrag stellen"
      confirmLoading={saving}
      width={760}
      destroyOnHidden
    >
      <div className="profile__modal">
        <Text type="secondary">
          Rollen auswählen, die hinzukommen oder wegfallen sollen. Der Antrag wird anschließend von der Finanzverwaltung geprüft.
        </Text>

        <section>
          <Title level={5}>Rollen beantragen</Title>
          <RoleTable
            caption="Beantragen"
            accountingUnits={accountingUnits}
            selected={added}
            onToggle={toggle(setAdded)}
            isSelectable={(unitKey, role) => !hasRole(unitKey, role)}
            notSelectableReason="Rolle bereits vorhanden"
            notSelectableChecked
          />
        </section>

        <section>
          <Title level={5}>Rollen abgeben</Title>
          <RoleTable
            caption="Abgeben"
            accountingUnits={accountingUnits}
            selected={removed}
            onToggle={toggle(setRemoved)}
            isSelectable={hasRole}
            notSelectableReason="Rolle nicht vorhanden"
            notSelectableChecked={false}
          />
        </section>

        <section>
          <Title level={5}>
            <label htmlFor="role-change-request-comment">Begründung (optional)</label>
          </Title>
          <Input.TextArea
            id="role-change-request-comment"
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            maxLength={COMMENT_MAX_LENGTH}
            showCount
            autoSize={{ minRows: 2, maxRows: 6 }}
          />
        </section>

        {showEmptyError && <Alert type="error" showIcon title="Mindestens eine Rolle auswählen" />}
      </div>
    </Modal>
  );
}

export default RoleChangeRequestModal;
