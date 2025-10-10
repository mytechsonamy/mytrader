import React, { useState, useEffect } from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Animated, Dimensions } from 'react-native';
import { createTimingAnimation, runSafeAnimation } from '../utils/animationUtils';

interface ErrorNotificationProps {
  message: string;
  type?: 'error' | 'warning' | 'info';
  duration?: number;
  onDismiss?: () => void;
  visible: boolean;
}

const ErrorNotification: React.FC<ErrorNotificationProps> = ({
  message,
  type = 'error',
  duration = 4000,
  onDismiss,
  visible
}) => {
  const [fadeAnim] = useState(new Animated.Value(0));
  const [slideAnim] = useState(new Animated.Value(50));

  useEffect(() => {
    if (visible) {
      // Animate in
      const animateIn = Animated.parallel([
        createTimingAnimation(fadeAnim, {
          toValue: 1,
          duration: 300,
          useNativeDriver: true,
        }),
        createTimingAnimation(slideAnim, {
          toValue: 0,
          duration: 300,
          useNativeDriver: true,
        }),
      ]);
      runSafeAnimation(animateIn);

      // Auto-dismiss after duration
      const timer = setTimeout(() => {
        handleDismiss();
      }, duration);

      return () => clearTimeout(timer);
    } else {
      // Animate out
      const animateOut = Animated.parallel([
        createTimingAnimation(fadeAnim, {
          toValue: 0,
          duration: 200,
          useNativeDriver: true,
        }),
        createTimingAnimation(slideAnim, {
          toValue: 50,
          duration: 200,
          useNativeDriver: true,
        }),
      ]);
      runSafeAnimation(animateOut);
    }
  }, [visible, duration, fadeAnim, slideAnim]);

  const handleDismiss = () => {
    const dismissAnimation = Animated.parallel([
      createTimingAnimation(fadeAnim, {
        toValue: 0,
        duration: 200,
        useNativeDriver: true,
      }),
      createTimingAnimation(slideAnim, {
        toValue: 50,
        duration: 200,
        useNativeDriver: true,
      }),
    ]);
    runSafeAnimation(dismissAnimation, () => {
      onDismiss?.();
    });
  };

  const getTypeStyles = () => {
    switch (type) {
      case 'warning':
        return {
          backgroundColor: '#FEF3C7',
          borderColor: '#F59E0B',
          textColor: '#92400E',
          iconColor: '#F59E0B',
          icon: '⚠️'
        };
      case 'info':
        return {
          backgroundColor: '#DBEAFE',
          borderColor: '#3B82F6',
          textColor: '#1E40AF',
          iconColor: '#3B82F6',
          icon: 'ℹ️'
        };
      default: // error
        return {
          backgroundColor: '#FEE2E2',
          borderColor: '#EF4444',
          textColor: '#DC2626',
          iconColor: '#EF4444',
          icon: '❌'
        };
    }
  };

  const typeStyles = getTypeStyles();

  if (!visible) {
    return null;
  }

  return (
    <Animated.View
      style={[
        styles.container,
        {
          opacity: fadeAnim,
          transform: [{ translateY: slideAnim }],
          backgroundColor: typeStyles.backgroundColor,
          borderColor: typeStyles.borderColor,
        }
      ]}
    >
      <View style={styles.content}>
        <Text style={[styles.icon, { color: typeStyles.iconColor }]}>
          {typeStyles.icon}
        </Text>
        <Text style={[styles.message, { color: typeStyles.textColor }]} numberOfLines={2}>
          {message}
        </Text>
        <TouchableOpacity
          style={styles.dismissButton}
          onPress={handleDismiss}
          activeOpacity={0.7}
        >
          <Text style={[styles.dismissText, { color: typeStyles.textColor }]}>✕</Text>
        </TouchableOpacity>
      </View>
    </Animated.View>
  );
};

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    bottom: 100,
    left: 16,
    right: 16,
    borderWidth: 1,
    borderRadius: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 5,
    zIndex: 1000,
  },
  content: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  icon: {
    fontSize: 18,
    marginRight: 12,
  },
  message: {
    flex: 1,
    fontSize: 14,
    fontWeight: '500',
    lineHeight: 18,
  },
  dismissButton: {
    marginLeft: 8,
    padding: 4,
  },
  dismissText: {
    fontSize: 16,
    fontWeight: '600',
  },
});

// Hook for managing error notifications
export const useErrorNotification = () => {
  const [notification, setNotification] = useState<{
    message: string;
    type: 'error' | 'warning' | 'info';
    visible: boolean;
  } | null>(null);

  const showError = (message: string, type: 'error' | 'warning' | 'info' = 'error') => {
    setNotification({ message, type, visible: true });
  };

  const hideError = () => {
    setNotification(prev => prev ? { ...prev, visible: false } : null);
  };

  const NotificationComponent = () => {
    if (!notification) return null;

    return (
      <ErrorNotification
        message={notification.message}
        type={notification.type}
        visible={notification.visible}
        onDismiss={() => {
          setNotification(null);
        }}
      />
    );
  };

  return {
    showError,
    hideError,
    NotificationComponent,
  };
};

export default ErrorNotification;