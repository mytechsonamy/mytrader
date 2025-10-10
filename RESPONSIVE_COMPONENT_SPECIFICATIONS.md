# MyTrader Web Component Specifications & Responsive Design

## Responsive Breakpoint System

### Breakpoint Definitions
```css
/* Mobile First Approach */
--breakpoint-xs: 320px;   /* Small phones */
--breakpoint-sm: 576px;   /* Large phones */
--breakpoint-md: 768px;   /* Tablets */
--breakpoint-lg: 1024px;  /* Small desktops */
--breakpoint-xl: 1280px;  /* Large desktops */
--breakpoint-2xl: 1536px; /* Extra large screens */
```

### Container System
```css
.container {
  width: 100%;
  margin: 0 auto;
  padding: 0 var(--space-4);
}

@media (min-width: 576px) { .container { max-width: 540px; } }
@media (min-width: 768px) { .container { max-width: 720px; } }
@media (min-width: 1024px) { .container { max-width: 960px; } }
@media (min-width: 1280px) { .container { max-width: 1200px; } }
@media (min-width: 1536px) { .container { max-width: 1320px; } }
```

## Layout Hierarchy Specifications

### 1. PublicLayout (Unauthenticated)

**Purpose**: Marketing and authentication pages
**Responsive Behavior**:
- Mobile: Full-width hero sections
- Desktop: Split-screen layouts

```tsx
interface PublicLayoutProps {
  children: React.ReactNode;
  showHeader?: boolean;
  showFooter?: boolean;
}

const PublicLayout: React.FC<PublicLayoutProps> = {
  // Header: Logo + Login/Register CTAs
  // Main: Hero sections with responsive grid
  // Footer: Links + social proof
}
```

**Responsive Grid**:
```css
/* Mobile: Single column */
.public-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--space-6);
}

/* Tablet: Two columns */
@media (min-width: 768px) {
  .public-grid {
    grid-template-columns: 1fr 1fr;
    gap: var(--space-8);
  }
}

/* Desktop: Three columns */
@media (min-width: 1024px) {
  .public-grid {
    grid-template-columns: 1fr 1fr 1fr;
    gap: var(--space-10);
  }
}
```

### 2. AuthenticatedLayout (Post-Login)

**Purpose**: Main trading dashboard and authenticated pages
**Components**: TopNavbar + Sidebar + MainContent + Footer

```tsx
interface AuthenticatedLayoutProps {
  children: React.ReactNode;
  sidebarCollapsed?: boolean;
  onSidebarToggle?: () => void;
}
```

**Layout Structure**:
```css
.authenticated-layout {
  display: grid;
  min-height: 100vh;
  grid-template-areas:
    "header header"
    "sidebar main"
    "footer footer";
  grid-template-rows: auto 1fr auto;
}

/* Mobile: Stack layout */
@media (max-width: 767px) {
  .authenticated-layout {
    grid-template-areas:
      "header"
      "main"
      "footer";
    grid-template-columns: 1fr;
  }
}

/* Desktop: Sidebar layout */
@media (min-width: 768px) {
  .authenticated-layout {
    grid-template-columns: 280px 1fr;
  }
}

/* Large screens: Wider sidebar */
@media (min-width: 1280px) {
  .authenticated-layout {
    grid-template-columns: 320px 1fr;
  }
}
```

## Navigation Component Specifications

### 1. TopNavbar (Web Adaptation)

**Mobile Transformation**: Bottom tabs → Top navigation bar
**Responsive Behavior**:
- Mobile: Hamburger menu + brand
- Desktop: Full navigation + user menu

```tsx
interface TopNavbarProps {
  user?: User | null;
  onMenuToggle?: () => void;
  onProfileClick?: () => void;
  onLogout?: () => void;
}

const TopNavbar: React.FC<TopNavbarProps> = {
  // Brand logo (left)
  // Main navigation (center, desktop only)
  // User menu + notifications (right)
  // Mobile hamburger menu
}
```

**Responsive Navigation**:
```css
.top-navbar {
  height: 64px;
  background: linear-gradient(135deg, var(--primary-gradient-start), var(--primary-gradient-end));
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 var(--space-4);
  position: sticky;
  top: 0;
  z-index: 50;
}

/* Desktop navigation items */
.nav-items {
  display: none;
}

@media (min-width: 768px) {
  .nav-items {
    display: flex;
    gap: var(--space-6);
  }
}

.nav-item {
  color: rgba(255, 255, 255, 0.8);
  text-decoration: none;
  font-weight: 600;
  font-size: var(--text-sm);
  transition: color 0.2s ease;
}

.nav-item:hover,
.nav-item.active {
  color: white;
}
```

### 2. Sidebar Navigation (Desktop)

**Purpose**: Persistent navigation for authenticated users
**Features**: Collapsible, emoji icons, active states

```tsx
interface SidebarProps {
  collapsed?: boolean;
  onToggle?: () => void;
  activeRoute?: string;
}

const navigationItems = [
  { icon: '🏠', label: 'Ana Sayfa', route: '/dashboard' },
  { icon: '💼', label: 'Portföy', route: '/portfolio' },
  { icon: '⚡', label: 'Stratejiler', route: '/strategies' },
  { icon: '🏆', label: 'Strategist', route: '/leaderboard' },
  { icon: '👤', label: 'Profil', route: '/profile' },
];
```

**Responsive Sidebar**:
```css
.sidebar {
  background: white;
  border-right: 1px solid var(--color-border);
  transition: width 0.3s ease;
  overflow: hidden;
}

/* Collapsed state */
.sidebar.collapsed {
  width: 64px;
}

/* Expanded state */
.sidebar.expanded {
  width: 280px;
}

/* Mobile: Overlay */
@media (max-width: 767px) {
  .sidebar {
    position: fixed;
    top: 64px;
    left: 0;
    height: calc(100vh - 64px);
    width: 280px;
    transform: translateX(-100%);
    z-index: 40;
  }

  .sidebar.open {
    transform: translateX(0);
  }
}

.sidebar-item {
  display: flex;
  align-items: center;
  padding: var(--space-3) var(--space-4);
  color: var(--text-secondary);
  text-decoration: none;
  transition: background-color 0.2s ease;
}

.sidebar-item:hover {
  background-color: var(--bg-hover);
}

.sidebar-item.active {
  background-color: rgba(102, 126, 234, 0.1);
  color: var(--primary-brand);
  border-right: 3px solid var(--primary-brand);
}

.sidebar-icon {
  font-size: 20px;
  margin-right: var(--space-3);
  min-width: 20px;
}

.sidebar-label {
  font-weight: 600;
  font-size: var(--text-sm);
}

.sidebar.collapsed .sidebar-label {
  display: none;
}
```

## Dashboard Component Adaptations

### 1. Smart Overview Header (Web Version)

**Mobile**: Full-width gradient header
**Desktop**: Contained header with better use of horizontal space

```tsx
interface WebOverviewHeaderProps extends SmartOverviewHeaderProps {
  layout?: 'mobile' | 'desktop';
  showBreadcrumbs?: boolean;
}
```

**Responsive Header**:
```css
.overview-header {
  background: linear-gradient(135deg, var(--primary-gradient-start), var(--primary-gradient-end));
  padding: var(--space-6) var(--space-4);
  border-radius: var(--radius-lg);
  margin-bottom: var(--space-6);
}

/* Desktop: Horizontal layout */
@media (min-width: 1024px) {
  .overview-header {
    padding: var(--space-8) var(--space-10);
  }

  .header-content {
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    gap: var(--space-8);
    align-items: center;
  }
}

/* Mobile: Vertical stacking */
@media (max-width: 1023px) {
  .header-content {
    display: flex;
    flex-direction: column;
    gap: var(--space-4);
  }
}
```

### 2. Asset Class Accordion (Responsive)

**Mobile**: Full-width accordions, single column cards
**Tablet**: Two-column card grid when expanded
**Desktop**: Three-column card grid, side-by-side accordions

```tsx
interface ResponsiveAccordionProps extends AssetClassAccordionProps {
  columnsDesktop?: number;
  columnsTablet?: number;
  showSideBySide?: boolean;
}
```

**Grid System**:
```css
/* Asset cards grid */
.asset-cards-grid {
  display: grid;
  gap: var(--space-3);
  grid-template-columns: 1fr; /* Mobile: Single column */
}

@media (min-width: 768px) {
  .asset-cards-grid {
    grid-template-columns: repeat(2, 1fr); /* Tablet: Two columns */
    gap: var(--space-4);
  }
}

@media (min-width: 1024px) {
  .asset-cards-grid {
    grid-template-columns: repeat(3, 1fr); /* Desktop: Three columns */
    gap: var(--space-5);
  }
}

/* Accordion container for desktop side-by-side */
@media (min-width: 1280px) {
  .accordions-container {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: var(--space-6);
  }
}
```

### 3. Asset Card Responsive Variants

**Three size variants for different breakpoints**:

```tsx
type AssetCardSize = 'compact' | 'medium' | 'large';

interface ResponsiveAssetCardProps extends AssetCardProps {
  size?: AssetCardSize;
  showFullDetails?: boolean;
  layout?: 'horizontal' | 'vertical';
}
```

**Size Specifications**:
```css
/* Compact (Mobile) */
.asset-card.compact {
  padding: var(--space-3);
  min-height: 80px;
}

/* Medium (Tablet) */
.asset-card.medium {
  padding: var(--space-4);
  min-height: 120px;
}

/* Large (Desktop) */
.asset-card.large {
  padding: var(--space-5);
  min-height: 160px;
}

/* Horizontal layout for wide screens */
@media (min-width: 1280px) {
  .asset-card.horizontal {
    display: grid;
    grid-template-columns: auto 1fr auto;
    align-items: center;
    gap: var(--space-4);
  }
}
```

### 4. Leaderboard Component (Web Adaptation)

**Mobile**: Vertical list with compact user cards
**Desktop**: Table-like layout with enhanced user details

```tsx
interface WebLeaderboardProps extends CompactLeaderboardProps {
  variant?: 'compact' | 'table' | 'cards';
  showFilters?: boolean;
  showSearch?: boolean;
  itemsPerPage?: number;
}
```

**Table Layout for Desktop**:
```css
.leaderboard-table {
  display: none;
}

@media (min-width: 1024px) {
  .leaderboard-table {
    display: table;
    width: 100%;
    border-collapse: collapse;
  }

  .leaderboard-row {
    display: table-row;
  }

  .leaderboard-cell {
    display: table-cell;
    padding: var(--space-3) var(--space-4);
    border-bottom: 1px solid var(--color-border);
    vertical-align: middle;
  }
}

/* Mobile: Keep card layout */
@media (max-width: 1023px) {
  .leaderboard-cards {
    display: block;
  }

  .leaderboard-table {
    display: none;
  }
}
```

## Web-Specific Enhancements

### 1. Hover States and Interactions

```css
/* Card hover effects */
.card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
}

/* Button hover states */
.button {
  transition: all 0.2s ease;
}

.button:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

/* Asset card hover with price highlight */
.asset-card:hover .asset-price {
  color: var(--primary-brand);
  font-weight: 700;
}

/* Navigation item hover effects */
.nav-item:hover {
  background-color: rgba(255, 255, 255, 0.1);
  border-radius: var(--radius-sm);
}
```

### 2. Desktop-Optimized Click Targets

```css
/* Larger click targets for desktop */
@media (min-width: 1024px) {
  .clickable {
    min-height: 48px;
    min-width: 48px;
  }

  .button {
    padding: var(--space-3) var(--space-6);
    font-size: var(--text-base);
  }

  .icon-button {
    width: 48px;
    height: 48px;
  }
}
```

### 3. Modal vs Full-Screen Patterns

```tsx
interface ResponsiveModalProps {
  isOpen: boolean;
  onClose: () => void;
  fullScreenOnMobile?: boolean;
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'full';
}
```

**Modal Responsive Behavior**:
```css
.modal {
  position: fixed;
  inset: 0;
  z-index: 50;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-4);
}

.modal-content {
  background: white;
  border-radius: var(--radius-lg);
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
}

/* Mobile: Full screen */
@media (max-width: 767px) {
  .modal-content {
    height: 100vh;
    max-height: 100vh;
    border-radius: 0;
  }
}

/* Desktop: Centered with max width */
@media (min-width: 768px) {
  .modal-content.size-sm { max-width: 400px; }
  .modal-content.size-md { max-width: 600px; }
  .modal-content.size-lg { max-width: 800px; }
  .modal-content.size-xl { max-width: 1200px; }
}
```

## Multi-Column Layout Specifications

### 1. Dashboard Grid System

```css
.dashboard-grid {
  display: grid;
  gap: var(--space-6);
}

/* Mobile: Single column */
.dashboard-grid {
  grid-template-columns: 1fr;
}

/* Tablet: Two columns */
@media (min-width: 768px) {
  .dashboard-grid {
    grid-template-columns: 2fr 1fr;
  }
}

/* Desktop: Three columns */
@media (min-width: 1280px) {
  .dashboard-grid {
    grid-template-columns: 2fr 1fr 1fr;
    grid-template-areas:
      "main-charts sidebar-stats sidebar-news"
      "main-charts sidebar-leaderboard sidebar-news"
      "asset-grid asset-grid asset-grid";
  }
}
```

### 2. Content Area Specifications

```tsx
interface ContentAreaProps {
  children: React.ReactNode;
  layout?: 'single' | 'sidebar' | 'three-column';
  stickyHeader?: boolean;
}
```

**Layout Variants**:
```css
/* Single column (mobile) */
.content-single {
  max-width: 100%;
  margin: 0 auto;
}

/* Sidebar layout (tablet) */
.content-sidebar {
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: var(--space-8);
}

/* Three column (desktop) */
.content-three-column {
  display: grid;
  grid-template-columns: 1fr 2fr 1fr;
  gap: var(--space-8);
}

@media (max-width: 1023px) {
  .content-sidebar,
  .content-three-column {
    grid-template-columns: 1fr;
  }
}
```

## Performance Considerations

### 1. Responsive Images
```css
.responsive-image {
  width: 100%;
  height: auto;
  object-fit: cover;
}

/* Different sizes for different screens */
@media (max-width: 767px) {
  .responsive-image {
    max-width: 300px;
  }
}

@media (min-width: 768px) {
  .responsive-image {
    max-width: 600px;
  }
}
```

### 2. Lazy Loading Strategy
```tsx
interface LazyComponentProps {
  threshold?: number;
  rootMargin?: string;
  fallback?: React.ReactNode;
}

// Implement intersection observer for accordion content
// Load heavy components only when visible
```

### 3. Virtualization for Large Lists
```tsx
// For leaderboard and large asset lists
interface VirtualizedListProps {
  items: any[];
  itemHeight: number;
  containerHeight: number;
  overscan?: number;
}
```

This comprehensive specification ensures the web version maintains the mobile app's sophisticated design language while providing optimal user experiences across all device types and screen sizes.