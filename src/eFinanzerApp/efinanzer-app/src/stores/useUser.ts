import { createContext, useContext, useEffect, useSyncExternalStore } from 'react';
import { formatUserName } from './userCache';
import type { UserCache, UserEntry } from './userCache';

export const UserCacheContext = createContext<UserCache | undefined>(undefined);

/** The shared user cache, available inside the UserStoreProvider */
export function useUserCache(): UserCache {
  const cache = useContext(UserCacheContext);
  if (!cache) throw new Error('useUserCache must be used inside a UserStoreProvider');
  return cache;
}

/** Info of the user with the given user name, loaded on first use. Undefined until the request started. */
export function useUser(username: string): UserEntry | undefined {
  const cache = useUserCache();
  const entry = useSyncExternalStore(cache.subscribe, () => cache.get(username));

  useEffect(() => {
    void cache.load(username);
  }, [cache, username]);

  return entry;
}

/** Full name of the user, the user name while loading or if the full name is not known */
export function useUserDisplayName(username: string): string {
  const entry = useUser(username);
  return (entry?.status === 'loaded' ? formatUserName(entry.user) : undefined) ?? username;
}
