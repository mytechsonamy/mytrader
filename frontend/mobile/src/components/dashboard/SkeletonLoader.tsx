import React, { useEffect, useRef } from 'react';
import { View, StyleSheet, Animated } from 'react-native';

interface SkeletonLoaderProps {
  count?: number;
  height?: number;
  style?: any;
}

export const SkeletonLoader: React.FC<SkeletonLoaderProps> = ({ 
  count = 5, 
  height = 60,
  style 
}) => {
  const animatedValue = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    const animation = Animated.loop(
      Animated.sequence([
        Animated.timing(animatedValue, {
          toValue: 1,
          duration: 1000,
          useNativeDriver: true,
        }),
        Animated.timing(animatedValue, {
          toValue: 0,
          duration: 1000,
          useNativeDriver: true,
        }),
      ])
    );

    animation.start();

    return () => animation.stop();
  }, [animatedValue]);

  const opacity = animatedValue.interpolate({
    inputRange: [0, 1],
    outputRange: [0.3, 0.7],
  });

  return (
    <View style={[styles.container, style]}>
      {Array.from({ length: count }).map((_, index) => (
        <Animated.View
          key={index}
          style={[
            styles.skeletonItem,
            { height, opacity },
          ]}
        />
      ))}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    width: '100%',
  },
  skeletonItem: {
    backgroundColor: '#E1E9EE',
    borderRadius: 8,
    marginBottom: 12,
  },
});
