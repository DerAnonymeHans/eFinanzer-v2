import { Skeleton } from 'antd';
import './UserName.css';
import {useUserCache} from "../../stores/userCache/useUserCache";
import {getUserDisplayName} from "../../texts/usernames";

interface UserNameProps {
  /** Keycloak user name, cryptic (e.g. the HTWK login), so only shown if the full name is unknown */
  username: string;
  className?: string;
}

/** Full name of a user, loaded through the user store. Falls back to the user name if there is none. */
function UserName({ username, className }: UserNameProps) {
  const userEntry = useUserCache(username)

  if (!userEntry || userEntry.status === 'loading') {
    return (
      <span className={className} role="status" aria-label="Name wird geladen">
        <Skeleton.Input className="user-name__skeleton" size="small" active />
      </span>
    );
  }

  const displayName = userEntry.status === 'loaded' ? getUserDisplayName(userEntry.user) : undefined;
  return <span className={className}>{displayName ?? username}</span>;
}

export default UserName;
