import {createAsyncThunk, createSlice, type PayloadAction} from "@reduxjs/toolkit";
import type {RootState} from "../store";
import type {User} from "../../api/types";
import apiClient from "../../api/client";

type UserEntry =
  | { status: 'loading' }
  | { status: 'loaded'; user: User }
  | { status: 'notFound' }
  | { status: 'error' };

interface UserCacheState {
  // map of username -> user
  users: Record<string, UserEntry>
}

const initialState: UserCacheState = {
  users: {}
}

// User names are case-insensitive, so "mmuster" and "MMuster" share one entry
const toKey = (username: string) => username.toLowerCase();

export const fetchUser = createAsyncThunk<User | null, string, { state: RootState, rejectValue: string }>(
  'userCacheSlice/fetchUser',
  async (username: string, { rejectWithValue }) => {
    try {
      const { data, response } = await apiClient.GET('/api/get-users/get-user', { params: { query: { username } } });
      if (response.status === 404) {
        return null;
      }
      if (!response.ok) {
        return rejectWithValue(response.statusText);
      }
      return data ?? null;
    } catch {
      return rejectWithValue(username);
    }
  },
  {
    condition: (username, { getState }) => {
      const entry = getState().userCache.users[toKey(username)];
      return entry === undefined || entry.status === 'error';
    }
  }
)

const userCacheSlice = createSlice({
  name: "userCache",
  initialState,
  reducers: {
    // Caches users that were already loaded elsewhere, e.g. by the user search
    cacheUsers: (state, action: PayloadAction<User[]>) => {
      for (const user of action.payload) {
        state.users[toKey(user.username)] = { status: 'loaded', user };
      }
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchUser.pending, (state, action) => {
        state.users[toKey(action.meta.arg)] = { status: 'loading' };
      })
      .addCase(fetchUser.fulfilled, (state, action) => {
        const user = action.payload;
        if (user === null) {
          state.users[toKey(action.meta.arg)] = { status: 'notFound' };
        } else {
          state.users[toKey(action.meta.arg)] = { status: 'loaded', user };
        }
      })
      .addCase(fetchUser.rejected, (state, action) => {
        state.users[toKey(action.meta.arg)] = { status: 'error' };
      })
  }
})

export const getUser = (state: RootState, username: string) => state.userCache.users[toKey(username)];

export const { cacheUsers } = userCacheSlice.actions;

export default userCacheSlice.reducer;