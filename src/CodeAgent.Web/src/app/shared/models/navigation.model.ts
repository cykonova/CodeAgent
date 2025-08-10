export interface MenuItem {
  label: string;
  icon: string;
  route?: string;
  badge?: string | number;
  badgeColor?: 'primary' | 'accent' | 'warn';
  children?: MenuItem[];
  expanded?: boolean;
  disabled?: boolean;
  permissions?: string[];
}

export interface MenuSection {
  title: string;
  items: MenuItem[];
  collapsible?: boolean;
  defaultExpanded?: boolean;
  expanded?: boolean;
}

export interface NavigationConfig {
  menuItems: MenuSection[];
  collapsed: boolean;
  showIcons?: boolean;
  showBadges?: boolean;
  allowNesting?: boolean;
  autoCollapse?: boolean;
}