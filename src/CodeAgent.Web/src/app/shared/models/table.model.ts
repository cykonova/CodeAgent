/**
 * Column configuration for table display
 */
export interface TableColumn {
  /** Property name in data object */
  key: string;
  /** Display text for column header */
  label: string;
  /** Enable column sorting */
  sortable?: boolean;
  /** Column width specification */
  width?: string;
  /** Text alignment (left, center, right) */
  align?: 'left' | 'center' | 'right';
  /** Data type for formatting */
  type?: 'text' | 'number' | 'date' | 'status' | 'actions';
}

/**
 * Row action configuration
 */
export interface TableAction {
  /** Material icon name */
  icon: string;
  /** Tooltip text for action */
  label: string;
  /** Material theme color */
  color?: 'primary' | 'accent' | 'warn';
  /** Function to execute on click */
  callback: (row: any) => void;
}

/**
 * Overall table configuration
 */
export interface TableConfig {
  /** Array of column definitions */
  columns: TableColumn[];
  /** Array of row actions */
  actions?: TableAction[];
  /** Default items per page */
  pageSize?: number;
  /** Available page size options */
  pageSizeOptions?: number[];
  /** Toggle pagination display */
  showPagination?: boolean;
  /** Enable row striping */
  striped?: boolean;
  /** Enable row hover effects */
  hoverable?: boolean;
}