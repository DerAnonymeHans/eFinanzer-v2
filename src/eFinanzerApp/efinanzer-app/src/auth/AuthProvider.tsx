import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { Button, Result, Spin } from 'antd';
import keycloak, { initKeycloak } from './keycloak';
import './AuthProvider.css';

interface AuthState {
  /** Keycloak user name, e.g. the HTWK login */
  username: string;
  /** Full name if Keycloak knows it, otherwise the user name */
  displayName: string;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

interface AuthProviderProps {
  children?: ReactNode;
}

type Status = 'loading' | 'authenticated' | 'failed';

// Renders its children only for a logged-in user, everything else is redirected to the Keycloak login first
export function AuthProvider({ children }: AuthProviderProps) {
  const [status, setStatus] = useState<Status>('loading');

  useEffect(() => {
    initKeycloak()
      .then((authenticated) => setStatus(authenticated ? 'authenticated' : 'loading'))
      .catch(() => setStatus('failed'));
  }, []);

  const value = useMemo<AuthState | undefined>(() => {
    if (status !== 'authenticated') return undefined;
    const token = keycloak.tokenParsed ?? {};
    const username: string = token.preferred_username ?? token.sub ?? '';
    return {
      username,
      displayName: token.name ?? username,
      logout: () => keycloak.logout({ redirectUri: window.location.origin + '/' }),
    };
  }, [status]);

  if (status === 'failed') {
    return (
      <div className="auth-screen">
        <Result
          status="error"
          title="Anmeldung fehlgeschlagen"
          subTitle="Der Anmeldedienst ist gerade nicht erreichbar."
          extra={
            <Button type="primary" onClick={() => window.location.reload()}>
              Erneut versuchen
            </Button>
          }
        />
      </div>
    );
  }

  if (!value) {
    return (
      <div className="auth-screen">
        <Spin size="large" description="Anmeldung…" />
      </div>
    );
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const state = useContext(AuthContext);
  if (!state) throw new Error('useAuth must be used inside an AuthProvider');
  return state;
}
