import { useState } from 'react';
import type { ReactNode } from 'react';
import { UserCache } from './userCache';
import { UserCacheContext } from './useUser';

interface UserStoreProviderProps {
  children?: ReactNode;
}

// Shares one user cache with the whole app, so every user is loaded only once per session
export function UserStoreProvider({ children }: UserStoreProviderProps) {
  const [cache] = useState(() => new UserCache());
  return <UserCacheContext.Provider value={cache}>{children}</UserCacheContext.Provider>;
}
