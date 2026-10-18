import { createContext, useContext } from 'react';

export interface AuthState {
  /** Keycloak user name, e.g. the HTWK login */
  username: string;
  /** Full name if Keycloak knows it, otherwise the user name */
  displayName: string;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthState | undefined>(undefined);

/** The logged-in user, available inside the AuthProvider */
export function useAuth(): AuthState {
  const state = useContext(AuthContext);
  if (!state) throw new Error('useAuth must be used inside an AuthProvider');
  return state;
}
