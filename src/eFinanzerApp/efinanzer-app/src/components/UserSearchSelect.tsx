import { useEffect, useRef, useState } from 'react';
import { Select, Spin } from 'antd';
import apiClient from '../api/client';
import type { User } from '../api/types';
import { formatUserName } from '../stores/userCache';
import { useUserCache } from '../stores/useUser';

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
  const userCache = useUserCache();
  const [results, setResults] = useState<User[]>([]);
  const [searchText, setSearchText] = useState('');
  const [state, setState] = useState<SearchState>('idle');

  // Only the latest search may update the results, so a slow earlier response cannot overwrite a newer one
  const latestSearch = useRef(0);
  const timer = useRef<ReturnType<typeof setTimeout>>(undefined);
  useEffect(() => () => clearTimeout(timer.current), []);

  const search = async (text: string, request: number) => {
    let found: User[] = [];
    let failed = false;
    try {
      const { data, response } = await apiClient.GET('/api/get-users/search-user', {
        params: { query: { search: text } },
      });
      if (response.ok && data) found = data;
      else failed = true;
    } catch {
      failed = true;
    }

    if (request !== latestSearch.current) return;
    // The names of the results are cached, so the selected user is shown without loading it again
    found.forEach(userCache.remember);
    setResults(found);
    setState(failed ? 'failed' : 'done');
  };

  // Waits until the typing pauses, so not every key press starts a request
  const handleSearch = (text: string) => {
    setSearchText(text);
    clearTimeout(timer.current);
    const request = ++latestSearch.current;

    if (text.trim().length < MIN_SEARCH_LENGTH) {
      setResults([]);
      setState('idle');
      return;
    }

    setState('loading');
    timer.current = setTimeout(() => void search(text.trim(), request), SEARCH_DELAY_MS);
  };

  const excluded = new Set(excludeUsernames.map((username) => username.toLowerCase()));
  const options = results
    .filter((user) => !excluded.has(user.username.toLowerCase()))
    .map((user) => ({ value: user.username, label: formatUserName(user) ?? user.username }));

  const notFoundContent = {
    idle: `Mindestens ${MIN_SEARCH_LENGTH} Zeichen eingeben`,
    loading: <Spin size="small" />,
    done: 'Keine Benutzer gefunden',
    failed: 'Suche fehlgeschlagen',
  }[state];

  return (
    <Select
      className={className}
      aria-label={ariaLabel}
      placeholder={placeholder}
      showSearch={{ filterOption: false, onSearch: handleSearch, searchValue: searchText }}
      labelInValue
      value={value ? { value: value.username, label: formatUserName(value) ?? value.username } : undefined}
      onChange={(selected?: { value: string; label: string }) => {
        onChange(selected ? results.find((user) => user.username === selected.value) : undefined);
        setSearchText('');
      }}
      options={options}
      notFoundContent={notFoundContent}
      allowClear
    />
  );
}

export default UserSearchSelect;
