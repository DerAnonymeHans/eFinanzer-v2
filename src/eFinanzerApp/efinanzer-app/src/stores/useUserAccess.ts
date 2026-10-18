import { createContext, useContext } from 'react';
import type { UserAccess } from '../api/types';

export interface UserAccessState {
  /** Undefined until loaded, or when loading failed */
  userAccess: UserAccess | undefined;
  loading: boolean;
  error: Error | undefined;
  /** Loads the user access again, e.g. after the user's roles changed */
  reload: () => Promise<void>;
}

export const UserAccessContext = createContext<UserAccessState | undefined>(undefined);

/** What the logged-in user may access, available inside the UserAccessProvider */
export function useUserAccess(): UserAccessState {
  const state = useContext(UserAccessContext);
  if (!state) throw new Error('useUserAccess must be used inside a UserAccessProvider');
  return state;
}
