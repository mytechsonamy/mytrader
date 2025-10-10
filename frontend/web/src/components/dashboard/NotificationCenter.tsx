import { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { removeNotification } from '../../store/slices/uiSlice';

const NotificationCenter: React.FC = () => {
  const dispatch = useAppDispatch();
  const { notifications } = useAppSelector((state) => state.ui);

  useEffect(() => {
    // Auto-remove notifications after 5 seconds
    notifications.forEach((notification) => {
      if (notification.autoClose !== false) {
        const timer = setTimeout(() => {
          dispatch(removeNotification(notification.id));
        }, 5000);

        return () => clearTimeout(timer);
      }
    });
  }, [notifications, dispatch]);

  if (notifications.length === 0) {
    return null;
  }

  return (
    <div className="notification-center">
      {notifications.map((notification) => (
        <div
          key={notification.id}
          className={`notification notification-${notification.type}`}
        >
          <div className="notification-content">
            <span className="notification-icon">
              {notification.type === 'success' && '✅'}
              {notification.type === 'error' && '❌'}
              {notification.type === 'warning' && '⚠️'}
              {notification.type === 'info' && 'ℹ️'}
            </span>
            <span className="notification-message">{notification.message}</span>
          </div>
          <button
            className="notification-close"
            onClick={() => dispatch(removeNotification(notification.id))}
          >
            ×
          </button>
        </div>
      ))}
    </div>
  );
};

export default NotificationCenter;