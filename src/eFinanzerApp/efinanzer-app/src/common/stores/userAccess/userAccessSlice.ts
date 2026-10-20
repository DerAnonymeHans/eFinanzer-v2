import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import type { RootState } from "../store";
import type { UserAccess } from "../../api/types";
import apiClient from "../../api/client";

export interface UserAccessState {
  /** Undefined until loaded. A failed reload keeps the last loaded access. */
  userAccess?: UserAccess;
  status: 'idle' | 'loading' | 'loaded' | 'failed';
  error?: string;
  // Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
  latestRequestId?: string;
}

const initialState: UserAccessState = {
  status: 'idle',
}

export const fetchUserAccess = createAsyncThunk<UserAccess, { reload?: boolean } | void, { state: RootState, rejectValue: string }>(
  'userAccess/fetchUserAccess',
  async (_, { rejectWithValue }) => {
    try {
      const { data, response } = await apiClient.GET('/api/get-user-access/my-user-access');
      if (response.ok && data) {
        return data;
      }
      return rejectWithValue(`Benutzerrechte konnten nicht geladen werden (HTTP ${response.status})`);
    } catch (e) {
      return rejectWithValue(e instanceof Error ? e.message : String(e));
    }
  },
  {
    // Loads only once, unless a reload is requested, e.g. after the user's roles changed
    condition: (arg, { getState }) => (arg && arg.reload) || getState().userAccess.status === 'idle',
  }
)

const userAccessSlice = createSlice({
  name: "userAccess",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchUserAccess.pending, (state, action) => {
        state.status = 'loading';
        state.latestRequestId = action.meta.requestId;
      })
      .addCase(fetchUserAccess.fulfilled, (state, action) => {
        if (action.meta.requestId !== state.latestRequestId) return;
        state.status = 'loaded';
        state.userAccess = action.payload;
        state.error = undefined;
      })
      .addCase(fetchUserAccess.rejected, (state, action) => {
        if (action.meta.requestId !== state.latestRequestId) return;
        state.status = 'failed';
        state.error = action.payload ?? action.error.message;
      })
  }
})

export const getUserAccess = (state: RootState) => state.userAccess;

export default userAccessSlice.reducer;
