import { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import { AppState, AppStateStatus } from 'react-native';

interface PerformanceConfig {
  updateInterval: number;
  maxUpdateFrequency: number;
  pauseOnBackground: boolean;
  enableBatching: boolean;
  debounceDelay: number;
}

interface PerformanceMetrics {
  renderCount: number;
  lastRenderTime: number;
  averageRenderTime: number;
  updateCount: number;
  droppedUpdates: number;
}

const DEFAULT_CONFIG: PerformanceConfig = {
  updateInterval: 1000, // 1 second
  maxUpdateFrequency: 60, // 60 FPS max
  pauseOnBackground: true,
  enableBatching: true,
  debounceDelay: 100,
};

export function usePerformanceOptimization(config: Partial<PerformanceConfig> = {}) {
  const finalConfig = { ...DEFAULT_CONFIG, ...config };
  const [isActive, setIsActive] = useState(true);
  const [metrics, setMetrics] = useState<PerformanceMetrics>({
    renderCount: 0,
    lastRenderTime: 0,
    averageRenderTime: 0,
    updateCount: 0,
    droppedUpdates: 0,
  });

  const lastUpdateTime = useRef(0);
  const renderStartTime = useRef(0);
  const renderTimes = useRef<number[]>([]);
  const updateQueue = useRef<(() => void)[]>([]);
  const batchTimer = useRef<NodeJS.Timeout | null>(null);

  // Monitor app state for background/foreground optimization
  useEffect(() => {
    if (!finalConfig.pauseOnBackground) return;

    const handleAppStateChange = (nextAppState: AppStateStatus) => {
      setIsActive(nextAppState === 'active');
    };

    const subscription = AppState.addEventListener('change', handleAppStateChange);
    return () => subscription?.remove();
  }, [finalConfig.pauseOnBackground]);

  // Performance monitoring
  const startRender = useCallback(() => {
    renderStartTime.current = performance.now();
  }, []);

  const endRender = useCallback(() => {
    const renderTime = performance.now() - renderStartTime.current;
    renderTimes.current.push(renderTime);

    // Keep only last 100 render times for average calculation
    if (renderTimes.current.length > 100) {
      renderTimes.current = renderTimes.current.slice(-100);
    }

    const averageRenderTime = renderTimes.current.reduce((sum, time) => sum + time, 0) / renderTimes.current.length;

    setMetrics(prev => ({
      ...prev,
      renderCount: prev.renderCount + 1,
      lastRenderTime: renderTime,
      averageRenderTime,
    }));
  }, []);

  // Throttled update function
  const throttledUpdate = useCallback((updateFn: () => void) => {
    const now = performance.now();
    const timeSinceLastUpdate = now - lastUpdateTime.current;
    const minInterval = 1000 / finalConfig.maxUpdateFrequency;

    if (!isActive && finalConfig.pauseOnBackground) {
      setMetrics(prev => ({ ...prev, droppedUpdates: prev.droppedUpdates + 1 }));
      return;
    }

    if (timeSinceLastUpdate < minInterval) {
      // If updating too frequently, either drop or queue the update
      if (finalConfig.enableBatching) {
        updateQueue.current.push(updateFn);

        if (batchTimer.current) {
          clearTimeout(batchTimer.current);
        }

        batchTimer.current = setTimeout(() => {
          const updates = updateQueue.current.splice(0);
          if (updates.length > 0) {
            // Execute only the last update to avoid redundant operations
            updates[updates.length - 1]();
            lastUpdateTime.current = performance.now();
            setMetrics(prev => ({
              ...prev,
              updateCount: prev.updateCount + 1,
              droppedUpdates: prev.droppedUpdates + (updates.length - 1),
            }));
          }
          batchTimer.current = null;
        }, finalConfig.debounceDelay);
      } else {
        setMetrics(prev => ({ ...prev, droppedUpdates: prev.droppedUpdates + 1 }));
      }
      return;
    }

    updateFn();
    lastUpdateTime.current = now;
    setMetrics(prev => ({ ...prev, updateCount: prev.updateCount + 1 }));
  }, [isActive, finalConfig]);

  // Debounced function creator
  const createDebouncedFunction = useCallback(<T extends (...args: any[]) => any>(
    func: T,
    delay: number = finalConfig.debounceDelay
  ) => {
    const timeoutRef = useRef<NodeJS.Timeout | null>(null);

    return useCallback((...args: Parameters<T>) => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }

      timeoutRef.current = setTimeout(() => {
        func(...args);
      }, delay);
    }, [func, delay]);
  }, [finalConfig.debounceDelay]);

  // Optimized state updater that batches updates
  const batchedSetState = useCallback(<T>(
    setter: React.Dispatch<React.SetStateAction<T>>,
    value: T | ((prev: T) => T)
  ) => {
    throttledUpdate(() => setter(value));
  }, [throttledUpdate]);

  // Memory-efficient array operations
  const optimizedArrayOperations = useMemo(() => ({
    // Virtualized rendering for large lists
    getVisibleItems: <T>(items: T[], startIndex: number, endIndex: number): T[] => {
      return items.slice(startIndex, endIndex + 1);
    },

    // Efficient filtering with memoization
    filterItems: <T>(items: T[], predicate: (item: T) => boolean, maxResults = 100): T[] => {
      const result: T[] = [];
      for (let i = 0; i < items.length && result.length < maxResults; i++) {
        if (predicate(items[i])) {
          result.push(items[i]);
        }
      }
      return result;
    },

    // Chunked processing for large datasets
    processInChunks: async <T, R>(
      items: T[],
      processor: (chunk: T[]) => R[],
      chunkSize = 50
    ): Promise<R[]> => {
      const results: R[] = [];

      for (let i = 0; i < items.length; i += chunkSize) {
        const chunk = items.slice(i, i + chunkSize);
        const chunkResults = processor(chunk);
        results.push(...chunkResults);

        // Yield control back to the event loop
        if (i + chunkSize < items.length) {
          await new Promise(resolve => setTimeout(resolve, 0));
        }
      }

      return results;
    },
  }), []);

  // Performance monitoring utilities
  const performanceUtils = useMemo(() => ({
    // Measure function execution time
    measureTime: <T extends (...args: any[]) => any>(
      func: T,
      label?: string
    ): T => {
      return ((...args: Parameters<T>) => {
        const start = performance.now();
        const result = func(...args);
        const end = performance.now();

        if (label && __DEV__) {
          console.log(`⏱️ ${label}: ${(end - start).toFixed(2)}ms`);
        }

        return result;
      }) as T;
    },

    // Check if device is under memory pressure (simplified)
    isMemoryPressureHigh: (): boolean => {
      // In a real implementation, you might use native modules to check memory
      return metrics.renderCount > 1000 && metrics.averageRenderTime > 16.67; // 60fps threshold
    },

    // Adaptive quality based on performance
    getAdaptiveQuality: (): 'high' | 'medium' | 'low' => {
      if (metrics.averageRenderTime < 8) return 'high'; // 120fps capable
      if (metrics.averageRenderTime < 16.67) return 'medium'; // 60fps capable
      return 'low'; // Below 60fps
    },
  }), [metrics]);

  // Cleanup function
  useEffect(() => {
    return () => {
      if (batchTimer.current) {
        clearTimeout(batchTimer.current);
      }
    };
  }, []);

  return {
    isActive,
    metrics,
    startRender,
    endRender,
    throttledUpdate,
    createDebouncedFunction,
    batchedSetState,
    optimizedArrayOperations,
    performanceUtils,
    config: finalConfig,
  };
}

// Hook for managing expensive computations
export function useExpensiveComputation<T>(
  computeFn: () => T,
  dependencies: React.DependencyList,
  options: {
    timeout?: number;
    fallback?: T;
    enableAsync?: boolean;
  } = {}
) {
  const { timeout = 100, fallback, enableAsync = false } = options;
  const [result, setResult] = useState<T | undefined>(fallback);
  const [isComputing, setIsComputing] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const abortController = useRef<AbortController | null>(null);

  const compute = useCallback(async () => {
    if (abortController.current) {
      abortController.current.abort();
    }

    abortController.current = new AbortController();
    setIsComputing(true);
    setError(null);

    try {
      if (enableAsync) {
        // Break up computation into chunks
        const timeoutPromise = new Promise<never>((_, reject) =>
          setTimeout(() => reject(new Error('Computation timeout')), timeout)
        );

        const computePromise = new Promise<T>((resolve) => {
          // Use setTimeout to prevent blocking
          setTimeout(() => {
            if (!abortController.current?.signal.aborted) {
              resolve(computeFn());
            }
          }, 0);
        });

        const computationResult = await Promise.race([computePromise, timeoutPromise]);

        if (!abortController.current?.signal.aborted) {
          setResult(computationResult);
        }
      } else {
        const computationResult = computeFn();
        setResult(computationResult);
      }
    } catch (err) {
      if (!abortController.current?.signal.aborted) {
        setError(err instanceof Error ? err : new Error('Computation failed'));
        if (fallback !== undefined) {
          setResult(fallback);
        }
      }
    } finally {
      setIsComputing(false);
    }
  }, [computeFn, timeout, fallback, enableAsync]);

  useEffect(() => {
    compute();

    return () => {
      if (abortController.current) {
        abortController.current.abort();
      }
    };
  }, dependencies);

  return { result, isComputing, error, recompute: compute };
}

// Hook for intersection observer (viewport optimization)
export function useIntersectionObserver(
  elementRef: React.RefObject<any>,
  options: {
    threshold?: number;
    rootMargin?: string;
    enabled?: boolean;
  } = {}
) {
  const { threshold = 0, rootMargin = '0px', enabled = true } = options;
  const [isIntersecting, setIsIntersecting] = useState(false);
  const [hasBeenSeen, setHasBeenSeen] = useState(false);

  useEffect(() => {
    if (!enabled || !elementRef.current) return;

    // For React Native, we'll simulate intersection observer behavior
    // In a real implementation, you might use a library like react-native-intersection-observer
    setIsIntersecting(true);
    setHasBeenSeen(true);

    return () => {
      setIsIntersecting(false);
    };
  }, [enabled, threshold, rootMargin]);

  return { isIntersecting, hasBeenSeen };
}