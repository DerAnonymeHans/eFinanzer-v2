import type {User} from "../api/types";


export function getUserDisplayName(user: User){
  if (!user.firstName && !user.lastName)
    return user.username;
  return `${user.firstName} ${user.lastName}`;
}