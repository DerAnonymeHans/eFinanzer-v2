import { Tag, Typography } from 'antd';
import { MinusOutlined, PlusOutlined } from '@ant-design/icons';
import type { RoleChange } from '../../api/types';
import { formatAccountingUnitRole } from '../../constants/roles';
import './RoleChangeList.css';

const { Text } = Typography;

interface RoleChangeListProps {
  roleChanges: RoleChange[];
  /** Name of an accounting unit, the key if it is not known (anymore) */
  getAccountingUnitName: (key: string) => string;
}

/** The roles a role change request adds and removes, each group only if it has roles */
function RoleChangeList({ roleChanges, getAccountingUnitName }: RoleChangeListProps) {
  const groups = [
    { label: 'Beantragte Rollen', icon: <PlusOutlined />, changes: roleChanges.filter((change) => change.isAdded) },
    { label: 'Abzugebende Rollen', icon: <MinusOutlined />, changes: roleChanges.filter((change) => !change.isAdded) },
  ];

  return (
    <div className="role-change-list">
      {groups
        .filter((group) => group.changes.length > 0)
        .map((group) => (
          <div key={group.label} className="role-change-list__group">
            <Text type="secondary">{group.label}</Text>
            <div className="role-change-list__tags">
              {group.changes.map((change) => (
                <Tag key={`${change.accountingUnitKey}/${change.accountingUnitRole}`} icon={group.icon}>
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
