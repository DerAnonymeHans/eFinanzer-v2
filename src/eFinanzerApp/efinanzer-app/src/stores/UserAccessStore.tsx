import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import apiClient from '../api/client';
import type { UserAccess } from '../api/types';

interface UserAccessState {
  /** Undefined until loaded, or when loading failed */
  userAccess: UserAccess | undefined;
  loading: boolean;
  error: Error | undefined;
  /** Loads the user access again, e.g. after the user's roles changed */
  reload: () => Promise<void>;
}

const UserAccessContext = createContext<UserAccessState | undefined>(undefined);

interface UserAccessProviderProps {
  children?: ReactNode;
}

// Loads what the signed-in user may access once and shares it with the whole app
export function UserAccessProvider({ children }: UserAccessProviderProps) {
  const [userAccess, setUserAccess] = useState<UserAccess>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error>();

  // Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
  const latestRequest = useRef(0);

  const reload = useCallback(async () => {
    const request = ++latestRequest.current;
    setLoading(true);

    let result: UserAccess | undefined;
    let failure: Error | undefined;
    try {
      const { data, response } = await apiClient.GET('/api/get-user-access/my-user-access');
      if (response.ok && data) {
        result = data;
      } else {
        failure = new Error(`Benutzerrechte konnten nicht geladen werden (HTTP ${response.status})`);
      }
    } catch (e) {
      failure = e instanceof Error ? e : new Error(String(e));
    }

    if (request !== latestRequest.current) return;
    setUserAccess(result);
    setError(failure);
    setLoading(false);
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  const value = useMemo(
    () => ({ userAccess, loading, error, reload }),
    [userAccess, loading, error, reload],
  );

  return <UserAccessContext.Provider value={value}>{children}</UserAccessContext.Provider>;
}

export function useUserAccess(): UserAccessState {
  const state = useContext(UserAccessContext);
  if (!state) throw new Error('useUserAccess must be used inside a UserAccessProvider');
  return state;
}
