import { Spin, Tag, Typography } from 'antd';
import { useAuth } from '../../common/auth/useAuth';
import { getRoleNames } from '../../common/texts/roles';
import { useUserAccess } from '../../common/stores/userAccess/useUserAccess';
import OwnRoleChangeRequests from './roleChangeRequests/OwnRoleChangeRequests';
import './ProfilePage.css';

const { Title, Text } = Typography;

function ProfilePage() {
  const { displayName } = useAuth();
  const { userAccess, loading: userAccessLoading } = useUserAccess();

  const roleNames = userAccess ? getRoleNames(userAccess) : [];

  return (
    <div className="profile">
      <div>
        <Title level={2} className="profile__title">
          {displayName}
        </Title>
        <div className="profile__tags profile__roles" aria-label="Aktuelle Rollen">
          {userAccessLoading && <Spin size="small" />}
          {!userAccessLoading && roleNames.length === 0 && <Text type="secondary">Keine Rollen</Text>}
          {roleNames.map((name) => (
            <Tag key={name}>{name}</Tag>
          ))}
        </div>
      </div>

      <OwnRoleChangeRequests />
    </div>
  );
}

export default ProfilePage;
