import type { components } from './schema';

// Short names for the generated API models
type Schemas = components['schemas'];

export type UserAccess = Schemas['UserAccess'];
export type AccountingUnit = Schemas['AccountingUnitDto'];
export type FiscalYear = Schemas['FiscalYearDto'];
export type AccountingUnitRole = Schemas['AccountingUnitRole'];
export type AdministrationRole = Schemas['AdministrationRole'];
export type AccountingUnitListItem = Schemas['GetAccountingUnitManagementListDto'];
export type AccountingUnitDetails = Schemas['GetAccountingUnitManagementDto'];
export type UserRoles = Schemas['UserRoles'];
export type User = Schemas['GetUserDto'];
export type FiscalYearListItem = Schemas['GetFiscalYearManagementListDto'];
