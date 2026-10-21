import { combineReducers, configureStore } from "@reduxjs/toolkit";
import userCacheReducer from "./userCache/userCacheSlice";
import userAccessReducer from "./userAccess/userAccessSlice";
import {useDispatch, useSelector} from "react-redux";

const rootReducer = combineReducers({
    userCache: userCacheReducer,
    userAccess: userAccessReducer,
})

export type RootState = ReturnType<typeof rootReducer>;

// A factory, so tests can start every render with a fresh store, optionally with some state already loaded
export const makeStore = (preloadedState?: Partial<RootState>) => configureStore({
    reducer: rootReducer,
    preloadedState,
})

export const store = makeStore();

export type AppStore = ReturnType<typeof makeStore>;
export type AppDispatch = AppStore['dispatch'];


export const useAppDispatch = useDispatch.withTypes<AppDispatch>();
export const useAppSelector = useSelector.withTypes<RootState>();