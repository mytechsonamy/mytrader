# MyTrader Design Tokens Specification

## Design Token Architecture

### Token Naming Convention
```
[category]-[property]-[variant]-[state]

Examples:
- color-brand-primary
- space-component-card-padding
- typography-heading-large-weight
- border-radius-card-default
```

### Token Categories
1. **Primitive Tokens**: Base values (colors, measurements)
2. **Semantic Tokens**: Purpose-based tokens (brand, feedback)
3. **Component Tokens**: Component-specific values
4. **Layout Tokens**: Spacing, sizing, breakpoints

## 1. Color Tokens

### Primitive Color Palette
```typescript
export const primitiveColors = {
  // Grays (Tailwind-inspired scale)
  gray: {
    50: '#f9fafb',
    100: '#f3f4f6',
    200: '#e5e7eb',
    300: '#d1d5db',
    400: '#9ca3af',
    500: '#6b7280',
    600: '#4b5563',
    700: '#374151',
    800: '#1f2937',
    900: '#111827',
  },

  // Brand Purple-Blue Scale
  brand: {
    50: '#ede9fe',
    100: '#ddd6fe',
    200: '#c4b5fd',
    300: '#a78bfa',
    400: '#8b5cf6',
    500: '#667eea', // Primary brand color
    600: '#5a67d8',
    700: '#4c51bf',
    800: '#434190',
    900: '#3c366b',
  },

  // Secondary Purple Scale
  purple: {
    50: '#faf5ff',
    100: '#f3e8ff',
    200: '#e9d5ff',
    300: '#d8b4fe',
    400: '#c084fc',
    500: '#764ba2', // Secondary brand color
    600: '#9333ea',
    700: '#7c3aed',
    800: '#6b21a8',
    900: '#581c87',
  },

  // Market Colors
  green: {
    50: '#ecfdf5',
    100: '#d1fae5',
    200: '#a7f3d0',
    300: '#6ee7b7',
    400: '#34d399',
    500: '#10b981', // Positive/Gains
    600: '#059669',
    700: '#047857',
    800: '#065f46',
    900: '#064e3b',
  },

  red: {
    50: '#fef2f2',
    100: '#fee2e2',
    200: '#fecaca',
    300: '#fca5a5',
    400: '#f87171',
    500: '#ef4444', // Negative/Losses
    600: '#dc2626',
    700: '#b91c1c',
    800: '#991b1b',
    900: '#7f1d1d',
  },

  // Warning/Alert Colors
  amber: {
    50: '#fffbeb',
    100: '#fef3c7',
    200: '#fde68a',
    300: '#fcd34d',
    400: '#fbbf24',
    500: '#f59e0b', // Warnings
    600: '#d97706',
    700: '#b45309',
    800: '#92400e',
    900: '#78350f',
  },

  // Information Colors
  blue: {
    50: '#eff6ff',
    100: '#dbeafe',
    200: '#bfdbfe',
    300: '#93c5fd',
    400: '#60a5fa',
    500: '#3b82f6', // Information
    600: '#2563eb',
    700: '#1d4ed8',
    800: '#1e40af',
    900: '#1e3a8a',
  },
} as const;
```

### Semantic Color Tokens
```typescript
export const semanticColors = {
  // Brand Identity
  brand: {
    primary: primitiveColors.brand[500],
    secondary: primitiveColors.purple[500],
    accent: primitiveColors.blue[500],
    gradient: {
      start: primitiveColors.brand[500],
      end: primitiveColors.purple[500],
    },
  },

  // Text Hierarchy
  text: {
    primary: primitiveColors.gray[800],
    secondary: primitiveColors.gray[700],
    tertiary: primitiveColors.gray[500],
    quaternary: primitiveColors.gray[400],
    inverse: '#ffffff',
    brand: primitiveColors.brand[500],
  },

  // Background System
  background: {
    primary: '#f8fafc',
    secondary: primitiveColors.gray[50],
    tertiary: '#ffffff',
    inverse: primitiveColors.gray[900],
    card: 'rgba(255, 255, 255, 0.95)',
    overlay: 'rgba(0, 0, 0, 0.6)',
  },

  // Border Colors
  border: {
    default: primitiveColors.gray[200],
    subtle: primitiveColors.gray[100],
    strong: primitiveColors.gray[300],
    brand: primitiveColors.brand[500],
  },

  // Interactive States
  interactive: {
    default: primitiveColors.brand[500],
    hover: primitiveColors.brand[600],
    active: primitiveColors.brand[700],
    disabled: primitiveColors.gray[300],
    focus: primitiveColors.brand[500],
  },

  // Feedback Colors
  feedback: {
    positive: primitiveColors.green[500],
    negative: primitiveColors.red[500],
    warning: primitiveColors.amber[500],
    info: primitiveColors.blue[500],
    neutral: primitiveColors.gray[500],
  },

  // Market-Specific Colors
  market: {
    bullish: primitiveColors.green[500],
    bearish: primitiveColors.red[500],
    neutral: primitiveColors.gray[500],
    open: primitiveColors.green[500],
    closed: primitiveColors.red[500],
    preMarket: primitiveColors.amber[500],
    afterMarket: primitiveColors.amber[500],
  },
} as const;
```

### Component Color Tokens
```typescript
export const componentColors = {
  // Navigation
  navigation: {
    background: semanticColors.brand.primary,
    text: semanticColors.text.inverse,
    textInactive: 'rgba(255, 255, 255, 0.6)',
    hover: 'rgba(255, 255, 255, 0.1)',
    active: 'rgba(255, 255, 255, 0.2)',
  },

  // Cards
  card: {
    background: semanticColors.background.card,
    backgroundHover: semanticColors.background.tertiary,
    border: semanticColors.border.subtle,
    borderHover: semanticColors.border.default,
    shadow: 'rgba(0, 0, 0, 0.1)',
  },

  // Buttons
  button: {
    primary: {
      background: semanticColors.brand.primary,
      backgroundHover: semanticColors.interactive.hover,
      backgroundActive: semanticColors.interactive.active,
      text: semanticColors.text.inverse,
      shadow: 'rgba(102, 126, 234, 0.3)',
    },
    secondary: {
      background: semanticColors.background.secondary,
      backgroundHover: semanticColors.border.default,
      backgroundActive: semanticColors.border.strong,
      text: semanticColors.text.primary,
      border: semanticColors.border.default,
    },
  },

  // Form Elements
  form: {
    background: semanticColors.background.tertiary,
    border: semanticColors.border.default,
    borderFocus: semanticColors.brand.primary,
    borderError: semanticColors.feedback.negative,
    placeholder: semanticColors.text.quaternary,
  },
} as const;
```

## 2. Typography Tokens

### Font Families
```typescript
export const fontFamilies = {
  display: ['Inter', 'system-ui', 'sans-serif'],
  body: ['Inter', 'system-ui', 'sans-serif'],
  mono: ['Fira Code', 'Monaco', 'Consolas', 'monospace'],
} as const;
```

### Font Weights
```typescript
export const fontWeights = {
  light: 300,
  regular: 400,
  medium: 500,
  semibold: 600,
  bold: 700,
  extrabold: 800,
} as const;
```

### Font Sizes (Responsive Scale)
```typescript
export const fontSizes = {
  // Mobile-first scales
  xs: {
    mobile: '10px',
    tablet: '11px',
    desktop: '12px',
  },
  sm: {
    mobile: '12px',
    tablet: '13px',
    desktop: '14px',
  },
  base: {
    mobile: '14px',
    tablet: '15px',
    desktop: '16px',
  },
  lg: {
    mobile: '16px',
    tablet: '17px',
    desktop: '18px',
  },
  xl: {
    mobile: '18px',
    tablet: '19px',
    desktop: '20px',
  },
  '2xl': {
    mobile: '20px',
    tablet: '22px',
    desktop: '24px',
  },
  '3xl': {
    mobile: '24px',
    tablet: '28px',
    desktop: '32px',
  },
  '4xl': {
    mobile: '28px',
    tablet: '34px',
    desktop: '40px',
  },
} as const;
```

### Line Heights
```typescript
export const lineHeights = {
  none: 1,
  tight: 1.25,
  normal: 1.5,
  relaxed: 1.75,
  loose: 2,
} as const;
```

### Typography Semantic Tokens
```typescript
export const typography = {
  // Headings
  h1: {
    fontFamily: fontFamilies.display,
    fontSize: fontSizes['4xl'],
    fontWeight: fontWeights.bold,
    lineHeight: lineHeights.tight,
    letterSpacing: '-0.025em',
  },
  h2: {
    fontFamily: fontFamilies.display,
    fontSize: fontSizes['3xl'],
    fontWeight: fontWeights.bold,
    lineHeight: lineHeights.tight,
    letterSpacing: '-0.025em',
  },
  h3: {
    fontFamily: fontFamilies.display,
    fontSize: fontSizes['2xl'],
    fontWeight: fontWeights.semibold,
    lineHeight: lineHeights.normal,
  },
  h4: {
    fontFamily: fontFamilies.display,
    fontSize: fontSizes.xl,
    fontWeight: fontWeights.semibold,
    lineHeight: lineHeights.normal,
  },
  h5: {
    fontFamily: fontFamilies.display,
    fontSize: fontSizes.lg,
    fontWeight: fontWeights.medium,
    lineHeight: lineHeights.normal,
  },

  // Body Text
  body: {
    fontFamily: fontFamilies.body,
    fontSize: fontSizes.base,
    fontWeight: fontWeights.regular,
    lineHeight: lineHeights.normal,
  },
  bodyLarge: {
    fontFamily: fontFamilies.body,
    fontSize: fontSizes.lg,
    fontWeight: fontWeights.regular,
    lineHeight: lineHeights.relaxed,
  },
  bodySmall: {
    fontFamily: fontFamilies.body,
    fontSize: fontSizes.sm,
    fontWeight: fontWeights.regular,
    lineHeight: lineHeights.normal,
  },

  // Financial Data Typography
  price: {
    fontFamily: fontFamilies.mono,
    fontWeight: fontWeights.bold,
    fontFeatureSettings: '"tnum"', // Tabular numbers
    fontVariantNumeric: 'tabular-nums',
  },
  priceLarge: {
    ...typography.price,
    fontSize: fontSizes['2xl'],
  },
  priceMedium: {
    ...typography.price,
    fontSize: fontSizes.lg,
  },
  priceSmall: {
    ...typography.price,
    fontSize: fontSizes.base,
  },

  // Interface Elements
  button: {
    fontFamily: fontFamilies.body,
    fontSize: fontSizes.sm,
    fontWeight: fontWeights.semibold,
    lineHeight: lineHeights.none,
  },
  caption: {
    fontFamily: fontFamilies.body,
    fontSize: fontSizes.xs,
    fontWeight: fontWeights.regular,
    lineHeight: lineHeights.normal,
  },
  label: {
    fontFamily: fontFamilies.body,
    fontSize: fontSizes.sm,
    fontWeight: fontWeights.medium,
    lineHeight: lineHeights.normal,
  },
} as const;
```

## 3. Spacing Tokens

### Base Spacing Scale
```typescript
export const spacing = {
  0: '0px',
  1: '4px',
  2: '8px',
  3: '12px',
  4: '16px',
  5: '20px',
  6: '24px',
  8: '32px',
  10: '40px',
  12: '48px',
  16: '64px',
  20: '80px',
  24: '96px',
  32: '128px',
  40: '160px',
  48: '192px',
  56: '224px',
  64: '256px',
} as const;
```

### Component-Specific Spacing
```typescript
export const componentSpacing = {
  // Layout Containers
  page: {
    padding: spacing[4],
    paddingDesktop: spacing[8],
  },
  section: {
    marginBottom: spacing[6],
    marginBottomDesktop: spacing[10],
  },

  // Cards
  card: {
    padding: spacing[4],
    paddingLarge: spacing[6],
    gap: spacing[3],
    marginBottom: spacing[3],
  },

  // Navigation
  navbar: {
    height: '64px',
    padding: spacing[4],
  },
  sidebar: {
    width: '280px',
    widthCollapsed: '64px',
    padding: spacing[4],
  },

  // Form Elements
  input: {
    padding: `${spacing[3]} ${spacing[4]}`,
    marginBottom: spacing[4],
  },
  button: {
    padding: `${spacing[3]} ${spacing[6]}`,
    gap: spacing[2],
  },

  // Lists and Grids
  list: {
    gap: spacing[2],
    padding: spacing[1],
  },
  grid: {
    gap: spacing[4],
    gapLarge: spacing[6],
  },
} as const;
```

## 4. Border Radius Tokens

```typescript
export const borderRadius = {
  none: '0px',
  sm: '8px',
  md: '12px',
  lg: '15px',
  xl: '20px',
  '2xl': '24px',
  full: '50px',
} as const;

export const componentBorderRadius = {
  card: borderRadius.lg,
  cardCompact: borderRadius.md,
  button: borderRadius.sm,
  buttonPrimary: borderRadius.md,
  input: borderRadius.sm,
  modal: borderRadius.xl,
  badge: borderRadius.full,
  avatar: borderRadius.full,
} as const;
```

## 5. Shadow Tokens

```typescript
export const shadows = {
  none: 'none',
  sm: '0 1px 2px rgba(0, 0, 0, 0.05)',
  md: '0 4px 6px rgba(0, 0, 0, 0.1)',
  lg: '0 10px 15px rgba(0, 0, 0, 0.1)',
  xl: '0 20px 25px rgba(0, 0, 0, 0.1)',
  '2xl': '0 25px 50px rgba(0, 0, 0, 0.25)',
  inner: 'inset 0 2px 4px rgba(0, 0, 0, 0.06)',
  focus: '0 0 0 3px rgba(102, 126, 234, 0.5)',
} as const;

export const componentShadows = {
  card: shadows.md,
  cardHover: shadows.lg,
  cardInteractive: shadows.xl,
  button: shadows.sm,
  buttonHover: '0 4px 12px rgba(102, 126, 234, 0.3)',
  modal: shadows['2xl'],
  dropdown: shadows.lg,
  tooltip: shadows.md,
} as const;
```

## 6. Breakpoint Tokens

```typescript
export const breakpoints = {
  xs: '320px',
  sm: '576px',
  md: '768px',
  lg: '1024px',
  xl: '1280px',
  '2xl': '1536px',
} as const;

export const mediaQueries = {
  xs: `(min-width: ${breakpoints.xs})`,
  sm: `(min-width: ${breakpoints.sm})`,
  md: `(min-width: ${breakpoints.md})`,
  lg: `(min-width: ${breakpoints.lg})`,
  xl: `(min-width: ${breakpoints.xl})`,
  '2xl': `(min-width: ${breakpoints['2xl']})`,
  mobile: `(max-width: ${breakpoints.md})`,
  tablet: `(min-width: ${breakpoints.md}) and (max-width: ${breakpoints.lg})`,
  desktop: `(min-width: ${breakpoints.lg})`,
} as const;
```

## 7. Animation Tokens

```typescript
export const animations = {
  // Duration
  duration: {
    fast: '0.15s',
    normal: '0.2s',
    slow: '0.3s',
    slower: '0.5s',
  },

  // Easing Functions
  easing: {
    ease: 'ease',
    easeIn: 'cubic-bezier(0.4, 0, 1, 1)',
    easeOut: 'cubic-bezier(0, 0, 0.2, 1)',
    easeInOut: 'cubic-bezier(0.4, 0, 0.2, 1)',
    bounce: 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
  },

  // Common Animations
  hover: {
    duration: animations.duration.normal,
    easing: animations.easing.easeOut,
    transform: 'translateY(-2px)',
  },
  focus: {
    duration: animations.duration.fast,
    easing: animations.easing.easeOut,
  },
  modal: {
    duration: animations.duration.slow,
    easing: animations.easing.easeInOut,
  },
} as const;
```

## 8. Z-Index Scale

```typescript
export const zIndex = {
  auto: 'auto',
  base: 0,
  docked: 10,
  dropdown: 20,
  sticky: 30,
  banner: 40,
  overlay: 50,
  modal: 60,
  popover: 70,
  skipLink: 80,
  toast: 90,
  tooltip: 100,
} as const;
```

## 9. CSS Custom Properties Export

```css
:root {
  /* Colors */
  --color-brand-primary: #{semanticColors.brand.primary};
  --color-brand-secondary: #{semanticColors.brand.secondary};
  --color-text-primary: #{semanticColors.text.primary};
  --color-text-secondary: #{semanticColors.text.secondary};
  --color-bg-primary: #{semanticColors.background.primary};
  --color-bg-card: #{semanticColors.background.card};

  /* Market Colors */
  --color-positive: #{semanticColors.market.bullish};
  --color-negative: #{semanticColors.market.bearish};
  --color-neutral: #{semanticColors.market.neutral};

  /* Spacing */
  --space-1: #{spacing[1]};
  --space-2: #{spacing[2]};
  --space-3: #{spacing[3]};
  --space-4: #{spacing[4]};
  --space-6: #{spacing[6]};
  --space-8: #{spacing[8]};

  /* Typography */
  --font-size-sm: #{fontSizes.sm.desktop};
  --font-size-base: #{fontSizes.base.desktop};
  --font-size-lg: #{fontSizes.lg.desktop};
  --font-weight-medium: #{fontWeights.medium};
  --font-weight-semibold: #{fontWeights.semibold};
  --font-weight-bold: #{fontWeights.bold};

  /* Border Radius */
  --radius-sm: #{borderRadius.sm};
  --radius-md: #{borderRadius.md};
  --radius-lg: #{borderRadius.lg};

  /* Shadows */
  --shadow-md: #{shadows.md};
  --shadow-lg: #{shadows.lg};

  /* Layout */
  --navbar-height: #{componentSpacing.navbar.height};
  --sidebar-width: #{componentSpacing.sidebar.width};
  --sidebar-width-collapsed: #{componentSpacing.sidebar.widthCollapsed};

  /* Breakpoints */
  --breakpoint-md: #{breakpoints.md};
  --breakpoint-lg: #{breakpoints.lg};
  --breakpoint-xl: #{breakpoints.xl};
}

/* Responsive font sizes */
@media (max-width: 767px) {
  :root {
    --font-size-sm: #{fontSizes.sm.mobile};
    --font-size-base: #{fontSizes.base.mobile};
    --font-size-lg: #{fontSizes.lg.mobile};
  }
}

@media (min-width: 768px) and (max-width: 1023px) {
  :root {
    --font-size-sm: #{fontSizes.sm.tablet};
    --font-size-base: #{fontSizes.base.tablet};
    --font-size-lg: #{fontSizes.lg.tablet};
  }
}
```

## 10. TypeScript Type Definitions

```typescript
// Utility type for accessing nested token values
type TokenPath<T, K extends keyof T = keyof T> = K extends string
  ? T[K] extends Record<string, any>
    ? `${K}.${TokenPath<T[K]>}`
    : K
  : never;

// Theme interface
export interface Theme {
  colors: typeof semanticColors;
  typography: typeof typography;
  spacing: typeof spacing;
  borderRadius: typeof borderRadius;
  shadows: typeof shadows;
  breakpoints: typeof breakpoints;
  animations: typeof animations;
  zIndex: typeof zIndex;
}

// Design token utility functions
export const getTokenValue = (path: string, theme: Theme): any => {
  return path.split('.').reduce((obj, key) => obj?.[key], theme);
};

export const createResponsiveValue = (
  values: { mobile?: any; tablet?: any; desktop?: any }
) => {
  return {
    mobile: values.mobile,
    tablet: values.tablet || values.mobile,
    desktop: values.desktop || values.tablet || values.mobile,
  };
};
```

This comprehensive design token specification provides a scalable foundation for implementing MyTrader's web interface while maintaining consistency with the mobile app's sophisticated design language.