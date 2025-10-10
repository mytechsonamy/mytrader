// Safe navigation utilities to prevent navigation crashes

export const safeNavigate = (url: string, fallbackMessage?: string) => {
  try {
    // Primary navigation method
    window.location.href = url;
  } catch (error) {
    console.error('Primary navigation failed:', error);

    try {
      // Fallback 1: Use History API
      window.history.pushState({}, '', url);
      window.location.reload();
    } catch (fallbackError) {
      console.error('History API navigation failed:', fallbackError);

      try {
        // Fallback 2: Use window.location.assign
        window.location.assign(url);
      } catch (assignError) {
        console.error('window.location.assign failed:', assignError);

        // Last resort: User notification
        const message = fallbackMessage || `Please navigate manually to: ${url}`;
        alert(message);
      }
    }
  }
};

export const safeReload = () => {
  try {
    window.location.reload();
  } catch (error) {
    console.error('Reload failed:', error);

    try {
      // Fallback: Navigate to current URL
      window.location.href = window.location.href;
    } catch (fallbackError) {
      console.error('Fallback reload failed:', fallbackError);
      alert('Please refresh the page manually');
    }
  }
};

export const safeBack = () => {
  try {
    if (window.history.length > 1) {
      window.history.back();
    } else {
      // If no history, go to home
      safeNavigate('/');
    }
  } catch (error) {
    console.error('Back navigation failed:', error);
    safeNavigate('/');
  }
};

export const safeForward = () => {
  try {
    window.history.forward();
  } catch (error) {
    console.error('Forward navigation failed:', error);
    // Silently fail for forward navigation
  }
};

export const safeReplaceState = (url: string) => {
  try {
    window.history.replaceState({}, '', url);
  } catch (error) {
    console.error('Replace state failed:', error);
    // Fallback to normal navigation
    safeNavigate(url);
  }
};

export const safePushState = (url: string) => {
  try {
    window.history.pushState({}, '', url);
  } catch (error) {
    console.error('Push state failed:', error);
    // Fallback to normal navigation
    safeNavigate(url);
  }
};

// Check if navigation is available and working
export const isNavigationAvailable = (): boolean => {
  try {
    return typeof window !== 'undefined' &&
           typeof window.location !== 'undefined' &&
           typeof window.history !== 'undefined';
  } catch (error) {
    console.error('Navigation availability check failed:', error);
    return false;
  }
};

// Get current URL safely
export const getCurrentUrl = (): string => {
  try {
    return window.location.href;
  } catch (error) {
    console.error('Failed to get current URL:', error);
    return '';
  }
};

// Get current pathname safely
export const getCurrentPathname = (): string => {
  try {
    return window.location.pathname;
  } catch (error) {
    console.error('Failed to get current pathname:', error);
    return '/';
  }
};

// Safe external link opening
export const safeOpenExternal = (url: string) => {
  try {
    window.open(url, '_blank', 'noopener,noreferrer');
  } catch (error) {
    console.error('Failed to open external link:', error);
    // Fallback: Copy to clipboard if possible
    try {
      navigator.clipboard.writeText(url);
      alert(`Unable to open link. URL copied to clipboard: ${url}`);
    } catch (clipboardError) {
      alert(`Unable to open link: ${url}`);
    }
  }
};