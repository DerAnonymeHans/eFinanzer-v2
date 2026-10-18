import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import apiClient from '../api/client';
import type { UserAccess } from '../api/types';
import { UserAccessContext } from './useUserAccess';

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

  // Sets state only once the response is there, so it can run inside an effect
  const load = useCallback(async () => {
    const request = ++latestRequest.current;

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
    void load();
  }, [load]);

  const reload = useCallback(() => {
    setLoading(true);
    return load();
  }, [load]);

  const value = useMemo(
    () => ({ userAccess, loading, error, reload }),
    [userAccess, loading, error, reload],
  );

  return <UserAccessContext.Provider value={value}>{children}</UserAccessContext.Provider>;
}
