import {fetchUser, getUser} from "./userCacheSlice";
import {useAppDispatch, useAppSelector} from "../store";
import {useEffect} from "react";

export function useUserCache(username: string) {
  const dispatch = useAppDispatch();
  const userEntry = useAppSelector((s) => getUser(s, username));

  useEffect(() => {
    dispatch(fetchUser(username));   // no-op if already cached (condition)
  }, [dispatch, username]);

  return userEntry;
}