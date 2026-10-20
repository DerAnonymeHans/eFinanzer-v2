import { useEffect, useRef, useState } from 'react';
import { Select, Spin } from 'antd';
import apiClient from '../../api/client';
import type { User } from '../../api/types';
import { useAppDispatch } from '../../stores/store';
import { cacheUsers } from '../../stores/userCache/userCacheSlice';
import { getUserDisplayName } from '../../texts/usernames';

// Mirrors GetUserTask.SearchStringMinLength in the backend
const MIN_SEARCH_LENGTH = 2;
const SEARCH_DELAY_MS = 300;

type SearchState = 'idle' | 'loading' | 'done' | 'failed';

interface UserSearchSelectProps {
  value: User | undefined;
  onChange: (user: User | undefined) => void;
  /** User names that are left out of the results, e.g. because they were already added */
  excludeUsernames?: string[];
  placeholder?: string;
  className?: string;
  'aria-label'?: string;
}

/** Select that finds users by their real name, user name or email through the API's user search */
function UserSearchSelect({
  value,
  onChange,
  excludeUsernames = [],
  placeholder = 'Name suchen…',
  className,
  'aria-label': ariaLabel,
}: UserSearchSelectProps) {
  const dispatch = useAppDispatch();
  const [userResults, setUserResults] = useState<User[]>([]);
  const [searchText, setSearchText] = useState('');
  const [searchState, setSearchState] = useState<SearchState>('idle');

  // Only the latest search may update the results, so a slow earlier response cannot overwrite a newer one
  const latestSearchNumber = useRef(0);
  const searchTimeout = useRef<ReturnType<typeof setTimeout>>(undefined);
  useEffect(() => () => clearTimeout(searchTimeout.current), []);

  const search = async (text: string, searchNumber: number) => {
    let foundUsers: User[] = [];
    let failed = false;
    try {
      const { data, response } = await apiClient.GET('/api/get-users/search-user', {
        params: { query: { search: text } },
      });
      if (response.ok && data) foundUsers = data;
      else failed = true;
    } catch {
      failed = true;
    }

    if (searchNumber !== latestSearchNumber.current) return;
    // The names of the results are cached, so the selected user is shown without loading it again
    dispatch(cacheUsers(foundUsers));
    setUserResults(foundUsers);
    setSearchState(failed ? 'failed' : 'done');
  };

  // Waits until the typing pauses, so not every key press starts a request
  const handleSearch = (text: string) => {
    setSearchText(text);
    clearTimeout(searchTimeout.current);
    const searchNumber = ++latestSearchNumber.current;

    if (text.trim().length < MIN_SEARCH_LENGTH) {
      setUserResults([]);
      setSearchState('idle');
      return;
    }

    setSearchState('loading');
    searchTimeout.current = setTimeout(() => void search(text.trim(), searchNumber), SEARCH_DELAY_MS);
  };

  const excluded = new Set(excludeUsernames.map((username) => username.toLowerCase()));
  const selectOptions = userResults
    .filter((user) => !excluded.has(user.username.toLowerCase()))
    .map((user) => ({ value: user.username, label: getUserDisplayName(user) }));

  const notFoundContent = {
    idle: `Mindestens ${MIN_SEARCH_LENGTH} Zeichen eingeben`,
    loading: <Spin size="small" />,
    done: 'Keine Benutzer gefunden',
    failed: 'Suche fehlgeschlagen',
  }[searchState];

  return (
    <Select
      className={className}
      aria-label={ariaLabel}
      placeholder={placeholder}
      showSearch={{ filterOption: false, onSearch: handleSearch, searchValue: searchText }}
      labelInValue
      value={value ? { value: value.username, label: getUserDisplayName(value) } : undefined}
      onChange={(selected?: { value: string; label: string }) => {
        onChange(selected ? userResults.find((user) => user.username === selected.value) : undefined);
        setSearchText('');
      }}
      options={selectOptions}
      notFoundContent={notFoundContent}
      allowClear
    />
  );
}

export default UserSearchSelect;
