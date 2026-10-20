import { useCallback, useEffect } from "react";
import { useAppDispatch, useAppSelector } from "../store";
import { fetchUserAccess, getUserAccess } from "./userAccessSlice";

/** What the logged-in user may access. Loads it on first use and shares it through the store. */
export function useUserAccess() {
  const dispatch = useAppDispatch();
  const { userAccess, status, error } = useAppSelector(getUserAccess);

  useEffect(() => {
    void dispatch(fetchUserAccess());   // no-op once loaded (condition)
  }, [dispatch]);

  /** Loads the user access again, e.g. after the user's roles changed */
  const reload = useCallback(async () => {
    await dispatch(fetchUserAccess({ reload: true }));
  }, [dispatch]);

  return {
    userAccess,
    loading: status === 'idle' || status === 'loading',
    error,
    reload,
  };
}
