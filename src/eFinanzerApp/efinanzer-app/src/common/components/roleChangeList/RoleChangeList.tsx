import { Tag } from 'antd';
import { MinusOutlined, PlusOutlined } from '@ant-design/icons';
import type { RoleChange } from '../../api/types';
import { formatAccountingUnitRole } from '../../texts/roles';
import './RoleChangeList.css';

interface RoleChangeListProps {
  roleChanges: RoleChange[];
  /** Name of an accounting unit, the key if it is not known (anymore) */
  getAccountingUnitName: (key: string) => string;
}

/** The roles a role change request adds and removes, each group only if it has roles */
function RoleChangeList({ roleChanges, getAccountingUnitName }: RoleChangeListProps) {
  const groups = [
    {
      label: 'Beantragte Rollen',
      icon: <PlusOutlined />,
      modifier: 'added',
      changes: roleChanges.filter((change) => change.isAdded),
    },
    {
      label: 'Abzugebende Rollen',
      icon: <MinusOutlined />,
      modifier: 'removed',
      changes: roleChanges.filter((change) => !change.isAdded),
    },
  ];

  return (
    <div className="role-change-list">
      {groups
        .filter((group) => group.changes.length > 0)
        .map((group) => (
          <div key={group.label} className="role-change-list__group">
            <span className="role-change-list__label">{group.label}</span>
            <div className="role-change-list__tags">
              {group.changes.map((change) => (
                <Tag
                  key={`${change.accountingUnitKey}/${change.accountingUnitRole}`}
                  icon={group.icon}
                  className={`role-change-list__tag role-change-list__tag--${group.modifier}`}
                >
                  {formatAccountingUnitRole(getAccountingUnitName(change.accountingUnitKey), change.accountingUnitRole)}
                </Tag>
              ))}
            </div>
          </div>
        ))}
    </div>
  );
}

export default RoleChangeList;
