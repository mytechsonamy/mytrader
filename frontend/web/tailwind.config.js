/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      // Color system based on design tokens
      colors: {
        // Brand colors
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
        // Secondary purple
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
        // Market colors
        positive: {
          50: '#ecfdf5',
          100: '#d1fae5',
          200: '#a7f3d0',
          300: '#6ee7b7',
          400: '#34d399',
          500: '#10b981', // Gains/bullish
          600: '#059669',
          700: '#047857',
          800: '#065f46',
          900: '#064e3b',
        },
        negative: {
          50: '#fef2f2',
          100: '#fee2e2',
          200: '#fecaca',
          300: '#fca5a5',
          400: '#f87171',
          500: '#ef4444', // Losses/bearish
          600: '#dc2626',
          700: '#b91c1c',
          800: '#991b1b',
          900: '#7f1d1d',
        },
        // Warning/Alert
        warning: {
          50: '#fffbeb',
          100: '#fef3c7',
          200: '#fde68a',
          300: '#fcd34d',
          400: '#fbbf24',
          500: '#f59e0b',
          600: '#d97706',
          700: '#b45309',
          800: '#92400e',
          900: '#78350f',
        },
        // Text hierarchy
        text: {
          primary: '#1f2937',
          secondary: '#374151',
          tertiary: '#6b7280',
          quaternary: '#9ca3af',
          inverse: '#ffffff',
        },
        // Background system
        background: {
          primary: '#f8fafc',
          secondary: '#f9fafb',
          tertiary: '#ffffff',
          inverse: '#111827',
          card: 'rgba(255, 255, 255, 0.95)',
          overlay: 'rgba(0, 0, 0, 0.6)',
        },
        // Border colors
        border: {
          DEFAULT: '#e5e7eb',
          subtle: '#f3f4f6',
          strong: '#d1d5db',
          brand: '#667eea',
        },
      },

      // Typography system
      fontFamily: {
        display: ['Inter', 'system-ui', 'sans-serif'],
        body: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['Fira Code', 'Monaco', 'Consolas', 'monospace'],
      },

      fontSize: {
        'xs': ['10px', { lineHeight: '1.5', letterSpacing: '0.025em' }],
        'sm': ['12px', { lineHeight: '1.5', letterSpacing: '0.025em' }],
        'base': ['14px', { lineHeight: '1.5' }],
        'lg': ['16px', { lineHeight: '1.5' }],
        'xl': ['18px', { lineHeight: '1.5' }],
        '2xl': ['20px', { lineHeight: '1.25', letterSpacing: '-0.025em' }],
        '3xl': ['24px', { lineHeight: '1.25', letterSpacing: '-0.025em' }],
        '4xl': ['28px', { lineHeight: '1.25', letterSpacing: '-0.025em' }],
        '5xl': ['32px', { lineHeight: '1.25', letterSpacing: '-0.025em' }],
        // Financial data sizes
        'price-sm': ['14px', { lineHeight: '1.25', fontFeatureSettings: '"tnum"' }],
        'price-md': ['16px', { lineHeight: '1.25', fontFeatureSettings: '"tnum"' }],
        'price-lg': ['20px', { lineHeight: '1.25', fontFeatureSettings: '"tnum"' }],
        'price-xl': ['24px', { lineHeight: '1.25', fontFeatureSettings: '"tnum"' }],
      },

      // Spacing system
      spacing: {
        '18': '4.5rem',   // 72px
        '88': '22rem',    // 352px
        '128': '32rem',   // 512px
        '144': '36rem',   // 576px
      },

      // Border radius
      borderRadius: {
        'sm': '8px',
        'md': '12px',
        'lg': '15px',
        'xl': '20px',
        '2xl': '24px',
      },

      // Box shadows
      boxShadow: {
        'card': '0 4px 6px rgba(0, 0, 0, 0.1)',
        'card-hover': '0 10px 15px rgba(0, 0, 0, 0.1)',
        'card-interactive': '0 20px 25px rgba(0, 0, 0, 0.1)',
        'button': '0 1px 2px rgba(0, 0, 0, 0.05)',
        'button-hover': '0 4px 12px rgba(102, 126, 234, 0.3)',
        'modal': '0 25px 50px rgba(0, 0, 0, 0.25)',
        'focus': '0 0 0 3px rgba(102, 126, 234, 0.5)',
      },

      // Breakpoints (mobile-first)
      screens: {
        'xs': '320px',
        'sm': '576px',
        'md': '768px',
        'lg': '1024px',
        'xl': '1280px',
        '2xl': '1536px',
      },

      // Animation & transitions
      animation: {
        'bounce-subtle': 'bounce-subtle 0.6s ease-in-out',
        'fade-in': 'fade-in 0.3s ease-in-out',
        'slide-up': 'slide-up 0.3s ease-out',
        'slide-down': 'slide-down 0.3s ease-out',
        'scale-in': 'scale-in 0.2s ease-out',
        'shimmer': 'shimmer 2s linear infinite',
      },

      keyframes: {
        'bounce-subtle': {
          '0%, 100%': { transform: 'scale(1)' },
          '50%': { transform: 'scale(1.05)' },
        },
        'fade-in': {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        'slide-up': {
          '0%': { transform: 'translateY(10px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' },
        },
        'slide-down': {
          '0%': { transform: 'translateY(-10px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' },
        },
        'scale-in': {
          '0%': { transform: 'scale(0.95)', opacity: '0' },
          '100%': { transform: 'scale(1)', opacity: '1' },
        },
        'shimmer': {
          '0%': { transform: 'translateX(-100%)' },
          '100%': { transform: 'translateX(100%)' },
        },
      },

      // Layout utilities
      container: {
        center: true,
        padding: {
          DEFAULT: '1rem',
          sm: '1.5rem',
          lg: '2rem',
          xl: '2.5rem',
          '2xl': '3rem',
        },
        screens: {
          sm: '640px',
          md: '768px',
          lg: '1024px',
          xl: '1280px',
          '2xl': '1400px',
        },
      },

      // Z-index scale
      zIndex: {
        'dropdown': '20',
        'sticky': '30',
        'banner': '40',
        'overlay': '50',
        'modal': '60',
        'popover': '70',
        'tooltip': '100',
      },

      // Aspect ratios for responsive components
      aspectRatio: {
        'card': '16 / 9',
        'chart': '4 / 3',
        'square': '1 / 1',
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
    // Custom plugin for component utilities
    function({ addUtilities, addComponents, theme }) {
      // Custom gradient utilities
      addUtilities({
        '.bg-gradient-brand': {
          'background-image': `linear-gradient(135deg, ${theme('colors.brand.500')} 0%, ${theme('colors.purple.500')} 100%)`,
        },
        '.bg-gradient-market-positive': {
          'background-image': `linear-gradient(135deg, ${theme('colors.positive.500')} 0%, ${theme('colors.positive.400')} 100%)`,
        },
        '.bg-gradient-market-negative': {
          'background-image': `linear-gradient(135deg, ${theme('colors.negative.500')} 0%, ${theme('colors.negative.400')} 100%)`,
        },
      });

      // Card component utilities
      addComponents({
        '.card': {
          'background-color': theme('colors.background.card'),
          'border-radius': theme('borderRadius.lg'),
          'box-shadow': theme('boxShadow.card'),
          'padding': theme('spacing.4'),
          'transition': 'all 0.2s ease-in-out',
        },
        '.card-hover': {
          '&:hover': {
            'box-shadow': theme('boxShadow.card-hover'),
            'transform': 'translateY(-2px)',
          },
        },
        '.card-interactive': {
          '&:hover': {
            'box-shadow': theme('boxShadow.card-interactive'),
            'transform': 'translateY(-4px)',
            'background-color': theme('colors.background.tertiary'),
          },
        },
        '.btn-primary': {
          'background-color': theme('colors.brand.500'),
          'color': theme('colors.text.inverse'),
          'padding': `${theme('spacing.3')} ${theme('spacing.6')}`,
          'border-radius': theme('borderRadius.md'),
          'font-weight': theme('fontWeight.semibold'),
          'font-size': theme('fontSize.sm[0]'),
          'line-height': theme('fontSize.sm[1].lineHeight'),
          'transition': 'all 0.2s ease-in-out',
          'box-shadow': theme('boxShadow.button'),
          '&:hover': {
            'background-color': theme('colors.brand.600'),
            'box-shadow': theme('boxShadow.button-hover'),
            'transform': 'translateY(-1px)',
          },
          '&:active': {
            'background-color': theme('colors.brand.700'),
            'transform': 'translateY(0)',
          },
          '&:focus': {
            'outline': 'none',
            'box-shadow': `${theme('boxShadow.button-hover')}, ${theme('boxShadow.focus')}`,
          },
          '&:disabled': {
            'background-color': theme('colors.gray.300'),
            'cursor': 'not-allowed',
            'transform': 'none',
            'box-shadow': 'none',
          },
        },
        '.btn-secondary': {
          'background-color': theme('colors.background.secondary'),
          'color': theme('colors.text.primary'),
          'border': `1px solid ${theme('colors.border.DEFAULT')}`,
          'padding': `${theme('spacing.3')} ${theme('spacing.6')}`,
          'border-radius': theme('borderRadius.md'),
          'font-weight': theme('fontWeight.semibold'),
          'font-size': theme('fontSize.sm[0]'),
          'line-height': theme('fontSize.sm[1].lineHeight'),
          'transition': 'all 0.2s ease-in-out',
          '&:hover': {
            'background-color': theme('colors.border.DEFAULT'),
            'border-color': theme('colors.border.strong'),
          },
          '&:active': {
            'background-color': theme('colors.border.strong'),
          },
          '&:focus': {
            'outline': 'none',
            'box-shadow': theme('boxShadow.focus'),
          },
        },
        '.price-positive': {
          'color': theme('colors.positive.500'),
          'font-weight': theme('fontWeight.bold'),
          'font-feature-settings': '"tnum"',
          'font-variant-numeric': 'tabular-nums',
        },
        '.price-negative': {
          'color': theme('colors.negative.500'),
          'font-weight': theme('fontWeight.bold'),
          'font-feature-settings': '"tnum"',
          'font-variant-numeric': 'tabular-nums',
        },
        '.price-neutral': {
          'color': theme('colors.text.tertiary'),
          'font-weight': theme('fontWeight.bold'),
          'font-feature-settings': '"tnum"',
          'font-variant-numeric': 'tabular-nums',
        },
      });
    },
  ],
}