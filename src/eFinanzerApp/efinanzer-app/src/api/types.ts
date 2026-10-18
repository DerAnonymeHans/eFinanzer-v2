import type { components } from './schema';

// Short names for the generated API models
type Schemas = components['schemas'];

export type UserAccess = Schemas['UserAccess'];
export type AccountingGroup = Schemas['AccountingGroupDto'];
export type FiscalYear = Schemas['FiscalYearDto'];
export type AccountingUnitRole = Schemas['AccountingUnitRole'];
export type AdministrationRole = Schemas['AdministrationRole'];
