/**
 * Animation utilities for React Native with Expo compatibility
 * Handles fallbacks for when native driver is not available
 */
import { Animated, Platform } from 'react-native';

interface AnimationConfig {
  toValue: number;
  duration: number;
  useNativeDriver?: boolean;
}

/**
 * Check if native driver is available
 * In Expo development, native driver may not always be available
 */
export const isNativeDriverAvailable = (): boolean => {
  try {
    // Test if native driver works
    const testAnim = new Animated.Value(0);
    Animated.timing(testAnim, {
      toValue: 1,
      duration: 0,
      useNativeDriver: true,
    });
    return true;
  } catch {
    return false;
  }
};

/**
 * Create a timing animation with safe native driver fallback
 */
export const createTimingAnimation = (
  animatedValue: Animated.Value,
  config: AnimationConfig
): Animated.CompositeAnimation => {
  const shouldUseNativeDriver = config.useNativeDriver !== false && isNativeDriverAvailable();

  return Animated.timing(animatedValue, {
    ...config,
    useNativeDriver: shouldUseNativeDriver,
  });
};

/**
 * Create a spring animation with safe native driver fallback
 */
export const createSpringAnimation = (
  animatedValue: Animated.Value,
  config: Omit<Animated.SpringAnimationConfig, 'useNativeDriver'> & { useNativeDriver?: boolean }
): Animated.CompositeAnimation => {
  const shouldUseNativeDriver = config.useNativeDriver !== false && isNativeDriverAvailable();

  return Animated.spring(animatedValue, {
    ...config,
    useNativeDriver: shouldUseNativeDriver,
  });
};

/**
 * Safe transform style creator that handles native driver compatibility
 */
export const createTransformStyle = (
  animatedValue: Animated.Value,
  transform: string = 'scale',
  inputRange: number[] = [0, 1],
  outputRange: string[] | number[] = [0, 1]
) => {
  return {
    transform: [
      {
        [transform]: animatedValue.interpolate({
          inputRange,
          outputRange,
        }),
      },
    ],
  };
};

/**
 * Create a rotation transform style
 */
export const createRotationStyle = (
  animatedValue: Animated.Value,
  inputRange: number[] = [0, 1],
  outputRange: string[] = ['0deg', '360deg']
) => {
  return createTransformStyle(animatedValue, 'rotate', inputRange, outputRange);
};

/**
 * Create a scale transform style
 */
export const createScaleStyle = (
  animatedValue: Animated.Value,
  inputRange: number[] = [0, 1],
  outputRange: number[] = [1, 1.05]
) => {
  return createTransformStyle(animatedValue, 'scale', inputRange, outputRange);
};

/**
 * Create an opacity style
 */
export const createOpacityStyle = (
  animatedValue: Animated.Value,
  inputRange: number[] = [0, 1],
  outputRange: number[] = [0, 1]
) => {
  return {
    opacity: animatedValue.interpolate({
      inputRange,
      outputRange,
    }),
  };
};

/**
 * Animation presets for common use cases
 */
export const AnimationPresets = {
  /**
   * Fade in animation
   */
  fadeIn: (animatedValue: Animated.Value, duration: number = 200) =>
    createTimingAnimation(animatedValue, {
      toValue: 1,
      duration,
      useNativeDriver: true,
    }),

  /**
   * Fade out animation
   */
  fadeOut: (animatedValue: Animated.Value, duration: number = 200) =>
    createTimingAnimation(animatedValue, {
      toValue: 0,
      duration,
      useNativeDriver: true,
    }),

  /**
   * Scale up animation
   */
  scaleUp: (animatedValue: Animated.Value, duration: number = 200) =>
    createSpringAnimation(animatedValue, {
      toValue: 1,
      useNativeDriver: true,
    }),

  /**
   * Scale down animation
   */
  scaleDown: (animatedValue: Animated.Value, duration: number = 200) =>
    createSpringAnimation(animatedValue, {
      toValue: 0,
      useNativeDriver: true,
    }),

  /**
   * Rotate animation
   */
  rotate: (animatedValue: Animated.Value, duration: number = 200, toValue: number = 1) =>
    createTimingAnimation(animatedValue, {
      toValue,
      duration,
      useNativeDriver: true,
    }),
};

/**
 * Error-safe animation runner
 * Catches and logs animation errors without crashing the app
 */
export const runSafeAnimation = (
  animation: Animated.CompositeAnimation,
  callback?: (finished: boolean) => void
): void => {
  try {
    animation.start((result) => {
      try {
        const finished = result ? result.finished : false;
        callback?.(finished);
      } catch (error) {
        console.warn('Animation callback error:', error);
      }
    });
  } catch (error) {
    console.warn('Animation start error:', error);
    // Call callback immediately if animation fails
    callback?.(false);
  }
};

export default {
  isNativeDriverAvailable,
  createTimingAnimation,
  createSpringAnimation,
  createTransformStyle,
  createRotationStyle,
  createScaleStyle,
  createOpacityStyle,
  AnimationPresets,
  runSafeAnimation,
};