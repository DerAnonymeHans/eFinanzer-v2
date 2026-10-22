export interface OffsetPagination {
	/** 1-based, like the page numbers shown */
	page: number;
	pageSize: number;
}

export interface Page<TItem> {
	items: TItem[];
	totalCount: number;
}
