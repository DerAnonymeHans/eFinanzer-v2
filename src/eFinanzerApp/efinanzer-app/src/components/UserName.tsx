import { Skeleton } from 'antd';
import { formatUserName } from '../stores/userCache';
import { useUser } from '../stores/useUser';
import './UserName.css';

interface UserNameProps {
  /** Keycloak user name, cryptic (e.g. the HTWK login), so only shown if the full name is unknown */
  username: string;
  className?: string;
}

/** Full name of a user, loaded through the user store. Falls back to the user name if there is none. */
function UserName({ username, className }: UserNameProps) {
  const entry = useUser(username);

  if (!entry || entry.status === 'loading') {
    return (
      <span className={className} role="status" aria-label="Name wird geladen">
        <Skeleton.Input className="user-name__skeleton" size="small" active />
      </span>
    );
  }

  const name = entry.status === 'loaded' ? formatUserName(entry.user) : undefined;
  return <span className={className}>{name ?? username}</span>;
}

export default UserName;
