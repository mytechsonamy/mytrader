import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  TextInput,
  Modal,
  ActivityIndicator,
  Alert,
  RefreshControl,
  Switch,
} from 'react-native';
import { Picker } from '@react-native-picker/picker';
import { useAuth } from '../context/AuthContext';
import { API_BASE_URL } from '../config';

interface PriceAlert {
  id: string;
  symbol: string;
  alertType: 'PRICE_ABOVE' | 'PRICE_BELOW' | 'PRICE_CHANGE';
  targetPrice: number;
  percentageChange?: number;
  currentPrice: number;
  isActive: boolean;
  message?: string;
  createdAt: string;
  triggeredAt?: string;
}

interface NotificationHistory {
  id: string;
  title: string;
  message: string;
  type: 'PRICE_ALERT' | 'STRATEGY_SIGNAL' | 'ACHIEVEMENT' | 'NEWS';
  isRead: boolean;
  createdAt: string;
  data?: any;
}

const AlarmsScreen = () => {
  const { user, getAuthHeaders } = useAuth();
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [activeTab, setActiveTab] = useState<'alerts' | 'notifications'>('alerts');
  const [priceAlerts, setPriceAlerts] = useState<PriceAlert[]>([]);
  const [notifications, setNotifications] = useState<NotificationHistory[]>([]);
  const [showCreateModal, setShowCreateModal] = useState(false);
  
  // Create Alert Form State
  const [newAlert, setNewAlert] = useState<{
    symbol: string;
    alertType: 'PRICE_ABOVE' | 'PRICE_BELOW' | 'PRICE_CHANGE';
    targetPrice: string;
    percentageChange: string;
    message: string;
  }>({
    symbol: 'BTCUSDT',
    alertType: 'PRICE_ABOVE',
    targetPrice: '',
    percentageChange: '',
    message: '',
  });

  const popularSymbols = ['BTCUSDT', 'ETHUSDT', 'BNBUSDT', 'ADAUSDT', 'XRPUSDT', 'SOLUSDT'];

  const fetchPriceAlerts = async () => {
    if (!user) return;

    try {
      const headers = await getAuthHeaders();
      const response = await fetch(`${API_BASE_URL}/prices/alerts`, {
        method: 'GET',
        headers,
      });

      if (response.ok) {
        const data = await response.json();
        setPriceAlerts(data.alerts || []);
      } else {
        // Mock data for development
        setPriceAlerts([
          {
            id: '1',
            symbol: 'BTCUSDT',
            alertType: 'PRICE_ABOVE',
            targetPrice: 70000,
            currentPrice: 65000,
            isActive: true,
            message: 'BTC $70k\'ya ulaştı!',
            createdAt: '2024-01-15T10:00:00Z',
          },
          {
            id: '2',
            symbol: 'ETHUSDT',
            alertType: 'PRICE_BELOW',
            targetPrice: 3000,
            currentPrice: 3200,
            isActive: true,
            message: 'ETH düşüş alarmı',
            createdAt: '2024-01-16T14:30:00Z',
          },
          {
            id: '3',
            symbol: 'BTCUSDT',
            alertType: 'PRICE_ABOVE',
            targetPrice: 60000,
            currentPrice: 65000,
            isActive: false,
            message: 'BTC hedefi vurdu!',
            createdAt: '2024-01-10T09:15:00Z',
            triggeredAt: '2024-01-12T11:45:00Z',
          },
        ]);
      }
    } catch (error) {
      console.error('Error fetching price alerts:', error);
    }
  };

  const fetchNotifications = async () => {
    if (!user) return;

    try {
      const headers = await getAuthHeaders();
      const response = await fetch(`${API_BASE_URL}/notifications/history?limit=20`, {
        method: 'GET',
        headers,
      });

      if (response.ok) {
        const data = await response.json();
        setNotifications(data.notifications || []);
      } else {
        // Mock data for development
        setNotifications([
          {
            id: '1',
            title: 'Fiyat Alarmı',
            message: 'BTC $65,000 seviyesini aştı!',
            type: 'PRICE_ALERT',
            isRead: false,
            createdAt: '2024-01-16T15:30:00Z',
          },
          {
            id: '2',
            title: 'Yeni Başarı!',
            message: 'İlk Kar başarısını kazandınız!',
            type: 'ACHIEVEMENT',
            isRead: true,
            createdAt: '2024-01-16T12:00:00Z',
          },
          {
            id: '3',
            title: 'Strateji Sinyali',
            message: 'RSI Stratejisi ETHUSDT için SATIŞ sinyali verdi',
            type: 'STRATEGY_SIGNAL',
            isRead: true,
            createdAt: '2024-01-16T10:15:00Z',
          },
        ]);
      }
    } catch (error) {
      console.error('Error fetching notifications:', error);
    }
  };

  const createPriceAlert = async () => {
    if (!user || !newAlert.symbol || !newAlert.targetPrice) {
      Alert.alert('Hata', 'Lütfen tüm alanları doldurun');
      return;
    }

    try {
      const headers = await getAuthHeaders();
      const response = await fetch(`${API_BASE_URL}/prices/alerts`, {
        method: 'POST',
        headers: {
          ...headers,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          symbol: newAlert.symbol,
          alertType: newAlert.alertType,
          targetPrice: parseFloat(newAlert.targetPrice),
          percentageChange: newAlert.percentageChange ? parseFloat(newAlert.percentageChange) : null,
          message: newAlert.message,
        }),
      });

      if (response.ok) {
        Alert.alert('Başarılı', 'Fiyat alarmı oluşturuldu');
        setShowCreateModal(false);
        setNewAlert({
          symbol: 'BTCUSDT',
          alertType: 'PRICE_ABOVE',
          targetPrice: '',
          percentageChange: '',
          message: '',
        });
        fetchPriceAlerts();
      } else {
        Alert.alert('Hata', 'Alarm oluşturulamadı');
      }
    } catch (error) {
      console.error('Error creating price alert:', error);
      Alert.alert('Hata', 'Bağlantı hatası');
    }
  };

  const toggleAlert = async (alertId: string, isActive: boolean) => {
    try {
      const headers = await getAuthHeaders();
      const response = await fetch(`${API_BASE_URL}/prices/alerts/${alertId}/toggle`, {
        method: 'PATCH',
        headers: {
          ...headers,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ isActive }),
      });

      if (response.ok) {
        setPriceAlerts(alerts =>
          alerts.map(alert =>
            alert.id === alertId ? { ...alert, isActive } : alert
          )
        );
      }
    } catch (error) {
      console.error('Error toggling alert:', error);
    }
  };

  const deleteAlert = async (alertId: string) => {
    Alert.alert(
      'Alarmı Sil',
      'Bu alarmı silmek istediğinizden emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Sil',
          style: 'destructive',
          onPress: async () => {
            try {
              const headers = await getAuthHeaders();
              const response = await fetch(`${API_BASE_URL}/prices/alerts/${alertId}`, {
                method: 'DELETE',
                headers,
              });

              if (response.ok) {
                setPriceAlerts(alerts => alerts.filter(alert => alert.id !== alertId));
              }
            } catch (error) {
              console.error('Error deleting alert:', error);
            }
          },
        },
      ]
    );
  };

  const markNotificationAsRead = async (notificationId: string) => {
    try {
      const headers = await getAuthHeaders();
      await fetch(`${API_BASE_URL}/notifications/${notificationId}/read`, {
        method: 'PATCH',
        headers,
      });

      setNotifications(notifications =>
        notifications.map(notification =>
          notification.id === notificationId
            ? { ...notification, isRead: true }
            : notification
        )
      );
    } catch (error) {
      console.error('Error marking notification as read:', error);
    }
  };

  const loadData = async () => {
    setLoading(true);
    await Promise.all([fetchPriceAlerts(), fetchNotifications()]);
    setLoading(false);
  };

  const onRefresh = async () => {
    setRefreshing(true);
    await loadData();
    setRefreshing(false);
  };

  useEffect(() => {
    loadData();
  }, []);

  const getAlertTypeText = (type: string) => {
    switch (type) {
      case 'PRICE_ABOVE':
        return 'Fiyat üstünde';
      case 'PRICE_BELOW':
        return 'Fiyat altında';
      case 'PRICE_CHANGE':
        return 'Fiyat değişimi';
      default:
        return type;
    }
  };

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'PRICE_ALERT':
        return '📈';
      case 'STRATEGY_SIGNAL':
        return '🎯';
      case 'ACHIEVEMENT':
        return '🏆';
      case 'NEWS':
        return '📰';
      default:
        return '🔔';
    }
  };

  const renderPriceAlerts = () => {
    const activeAlerts = priceAlerts.filter(alert => alert.isActive);
    const triggeredAlerts = priceAlerts.filter(alert => !alert.isActive);

    return (
      <View style={styles.alertsContainer}>
        <View style={styles.sectionHeader}>
          <Text style={styles.sectionTitle}>📈 Aktif Alarmlar</Text>
          <TouchableOpacity
            style={styles.addButton}
            onPress={() => setShowCreateModal(true)}
          >
            <Text style={styles.addButtonText}>+ Yeni Alarm</Text>
          </TouchableOpacity>
        </View>

        {activeAlerts.map((alert) => (
          <View key={alert.id} style={[styles.alertCard, styles.activeAlert]}>
            <View style={styles.alertHeader}>
              <Text style={styles.alertSymbol}>{alert.symbol}</Text>
              <Switch
                value={alert.isActive}
                onValueChange={(value) => toggleAlert(alert.id, value)}
                trackColor={{ false: '#ccc', true: '#667eea' }}
                thumbColor={alert.isActive ? 'white' : '#f4f3f4'}
              />
            </View>
            
            <Text style={styles.alertType}>
              {getAlertTypeText(alert.alertType)}: ${alert.targetPrice.toLocaleString()}
            </Text>
            
            <Text style={styles.currentPrice}>
              Mevcut fiyat: ${alert.currentPrice.toLocaleString()}
            </Text>
            
            {alert.message && (
              <Text style={styles.alertMessage}>"{alert.message}"</Text>
            )}
            
            <View style={styles.alertActions}>
              <Text style={styles.alertDate}>
                {new Date(alert.createdAt).toLocaleDateString('tr-TR')}
              </Text>
              <TouchableOpacity
                style={styles.deleteButton}
                onPress={() => deleteAlert(alert.id)}
              >
                <Text style={styles.deleteButtonText}>🗑️</Text>
              </TouchableOpacity>
            </View>
          </View>
        ))}

        {activeAlerts.length === 0 && (
          <View style={styles.emptyState}>
            <Text style={styles.emptyStateText}>Henüz aktif alarm yok</Text>
          </View>
        )}

        {triggeredAlerts.length > 0 && (
          <>
            <Text style={styles.sectionTitle}>✅ Tetiklenen Alarmlar</Text>
            {triggeredAlerts.map((alert) => (
              <View key={alert.id} style={[styles.alertCard, styles.triggeredAlert]}>
                <View style={styles.alertHeader}>
                  <Text style={styles.alertSymbol}>{alert.symbol}</Text>
                  <Text style={styles.triggeredBadge}>Tetiklendi</Text>
                </View>
                
                <Text style={styles.alertType}>
                  {getAlertTypeText(alert.alertType)}: ${alert.targetPrice.toLocaleString()}
                </Text>
                
                {alert.triggeredAt && (
                  <Text style={styles.triggeredDate}>
                    Tetiklenme: {new Date(alert.triggeredAt).toLocaleDateString('tr-TR')}
                  </Text>
                )}
                
                <TouchableOpacity
                  style={styles.deleteButton}
                  onPress={() => deleteAlert(alert.id)}
                >
                  <Text style={styles.deleteButtonText}>🗑️ Sil</Text>
                </TouchableOpacity>
              </View>
            ))}
          </>
        )}
      </View>
    );
  };

  const renderNotifications = () => {
    const unreadNotifications = notifications.filter(n => !n.isRead);
    const readNotifications = notifications.filter(n => n.isRead);

    return (
      <View style={styles.notificationsContainer}>
        {unreadNotifications.length > 0 && (
          <>
            <Text style={styles.sectionTitle}>🔔 Okunmayan Bildirimler</Text>
            {unreadNotifications.map((notification) => (
              <TouchableOpacity
                key={notification.id}
                style={[styles.notificationCard, styles.unreadNotification]}
                onPress={() => markNotificationAsRead(notification.id)}
              >
                <View style={styles.notificationHeader}>
                  <Text style={styles.notificationIcon}>
                    {getNotificationIcon(notification.type)}
                  </Text>
                  <View style={styles.notificationContent}>
                    <Text style={styles.notificationTitle}>{notification.title}</Text>
                    <Text style={styles.notificationMessage}>{notification.message}</Text>
                  </View>
                  <View style={styles.unreadDot} />
                </View>
                <Text style={styles.notificationDate}>
                  {new Date(notification.createdAt).toLocaleString('tr-TR')}
                </Text>
              </TouchableOpacity>
            ))}
          </>
        )}

        {readNotifications.length > 0 && (
          <>
            <Text style={styles.sectionTitle}>📖 Okunmuş Bildirimler</Text>
            {readNotifications.map((notification) => (
              <View key={notification.id} style={styles.notificationCard}>
                <View style={styles.notificationHeader}>
                  <Text style={styles.notificationIcon}>
                    {getNotificationIcon(notification.type)}
                  </Text>
                  <View style={styles.notificationContent}>
                    <Text style={[styles.notificationTitle, styles.readTitle]}>
                      {notification.title}
                    </Text>
                    <Text style={[styles.notificationMessage, styles.readMessage]}>
                      {notification.message}
                    </Text>
                  </View>
                </View>
                <Text style={styles.notificationDate}>
                  {new Date(notification.createdAt).toLocaleString('tr-TR')}
                </Text>
              </View>
            ))}
          </>
        )}

        {notifications.length === 0 && (
          <View style={styles.emptyState}>
            <Text style={styles.emptyStateText}>Henüz bildirim yok</Text>
          </View>
        )}
      </View>
    );
  };

  const renderCreateModal = () => (
    <Modal visible={showCreateModal} animationType="slide" transparent>
      <View style={styles.modalOverlay}>
        <View style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>Yeni Fiyat Alarmı</Text>
            <TouchableOpacity onPress={() => setShowCreateModal(false)}>
              <Text style={styles.modalClose}>✖️</Text>
            </TouchableOpacity>
          </View>

          <ScrollView style={styles.modalContent}>
            <Text style={styles.formLabel}>Coin</Text>
            <View style={styles.pickerContainer}>
              <Picker
                selectedValue={newAlert.symbol}
                onValueChange={(value) => setNewAlert({ ...newAlert, symbol: value })}
                style={styles.picker}
              >
                {popularSymbols.map((symbol) => (
                  <Picker.Item key={symbol} label={symbol} value={symbol} />
                ))}
              </Picker>
            </View>

            <Text style={styles.formLabel}>Alarm Tipi</Text>
            <View style={styles.pickerContainer}>
              <Picker
                selectedValue={newAlert.alertType}
                onValueChange={(value) => setNewAlert({ ...newAlert, alertType: value })}
                style={styles.picker}
              >
                <Picker.Item label="Fiyat üstünde" value="PRICE_ABOVE" />
                <Picker.Item label="Fiyat altında" value="PRICE_BELOW" />
                <Picker.Item label="Yüzde değişim" value="PRICE_CHANGE" />
              </Picker>
            </View>

            <Text style={styles.formLabel}>Hedef Fiyat ($)</Text>
            <TextInput
              style={styles.input}
              value={newAlert.targetPrice}
              onChangeText={(text) => setNewAlert({ ...newAlert, targetPrice: text })}
              placeholder="50000"
              keyboardType="numeric"
            />

            {newAlert.alertType === 'PRICE_CHANGE' && (
              <>
                <Text style={styles.formLabel}>Yüzde Değişim (%)</Text>
                <TextInput
                  style={styles.input}
                  value={newAlert.percentageChange}
                  onChangeText={(text) => setNewAlert({ ...newAlert, percentageChange: text })}
                  placeholder="5"
                  keyboardType="numeric"
                />
              </>
            )}

            <Text style={styles.formLabel}>Mesaj (Opsiyonel)</Text>
            <TextInput
              style={styles.input}
              value={newAlert.message}
              onChangeText={(text) => setNewAlert({ ...newAlert, message: text })}
              placeholder="Özel alarm mesajı..."
              multiline
            />
          </ScrollView>

          <View style={styles.modalActions}>
            <TouchableOpacity
              style={styles.cancelButton}
              onPress={() => setShowCreateModal(false)}
            >
              <Text style={styles.cancelButtonText}>İptal</Text>
            </TouchableOpacity>
            <TouchableOpacity style={styles.createButton} onPress={createPriceAlert}>
              <Text style={styles.createButtonText}>Oluştur</Text>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </Modal>
  );

  if (!user) {
    return (
      <View style={styles.container}>
        <View style={styles.loginPrompt}>
          <Text style={styles.loginTitle}>🔔 Alarmlar & Bildirimler</Text>
          <Text style={styles.loginText}>Fiyat alarmları oluşturmak için giriş yapın</Text>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>🔔 Alarmlar</Text>
        <View style={styles.tabContainer}>
          <TouchableOpacity
            style={[styles.tab, activeTab === 'alerts' && styles.activeTab]}
            onPress={() => setActiveTab('alerts')}
          >
            <Text style={[styles.tabText, activeTab === 'alerts' && styles.activeTabText]}>
              Alarmlar
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.tab, activeTab === 'notifications' && styles.activeTab]}
            onPress={() => setActiveTab('notifications')}
          >
            <Text style={[styles.tabText, activeTab === 'notifications' && styles.activeTabText]}>
              Bildirimler
            </Text>
          </TouchableOpacity>
        </View>
      </View>

      <ScrollView
        style={styles.content}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
      >
        {loading ? (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color="#667eea" />
            <Text style={styles.loadingText}>Veriler yükleniyor...</Text>
          </View>
        ) : (
          <>
            {activeTab === 'alerts' && renderPriceAlerts()}
            {activeTab === 'notifications' && renderNotifications()}
          </>
        )}
      </ScrollView>

      {renderCreateModal()}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  header: {
    backgroundColor: 'white',
    padding: 20,
    paddingTop: 60,
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#333',
    textAlign: 'center',
    marginBottom: 20,
  },
  tabContainer: {
    flexDirection: 'row',
    backgroundColor: '#f1f5f9',
    borderRadius: 8,
    padding: 4,
  },
  tab: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 6,
    alignItems: 'center',
  },
  activeTab: {
    backgroundColor: '#667eea',
  },
  tabText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#64748b',
  },
  activeTabText: {
    color: 'white',
  },
  content: {
    flex: 1,
    padding: 20,
  },
  sectionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 15,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 15,
  },
  addButton: {
    backgroundColor: '#667eea',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 6,
  },
  addButtonText: {
    color: 'white',
    fontSize: 12,
    fontWeight: '600',
  },
  alertsContainer: {
    marginBottom: 20,
  },
  alertCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 15,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  activeAlert: {
    borderLeftWidth: 4,
    borderLeftColor: '#10b981',
  },
  triggeredAlert: {
    borderLeftWidth: 4,
    borderLeftColor: '#f59e0b',
    opacity: 0.8,
  },
  alertHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  alertSymbol: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
  },
  alertType: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 4,
  },
  currentPrice: {
    fontSize: 14,
    color: '#10b981',
    fontWeight: '600',
    marginBottom: 8,
  },
  alertMessage: {
    fontSize: 14,
    color: '#667eea',
    fontStyle: 'italic',
    marginBottom: 8,
  },
  alertActions: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  alertDate: {
    fontSize: 12,
    color: '#94a3b8',
  },
  deleteButton: {
    padding: 5,
  },
  deleteButtonText: {
    fontSize: 16,
    color: '#ef4444',
  },
  triggeredBadge: {
    backgroundColor: '#f59e0b',
    color: 'white',
    fontSize: 10,
    fontWeight: 'bold',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  triggeredDate: {
    fontSize: 12,
    color: '#f59e0b',
    fontWeight: '600',
    marginBottom: 8,
  },
  notificationsContainer: {
    marginBottom: 20,
  },
  notificationCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 15,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  unreadNotification: {
    borderLeftWidth: 4,
    borderLeftColor: '#667eea',
    backgroundColor: '#f8fafc',
  },
  notificationHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginBottom: 8,
  },
  notificationIcon: {
    fontSize: 20,
    marginRight: 12,
    marginTop: 2,
  },
  notificationContent: {
    flex: 1,
  },
  notificationTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 4,
  },
  readTitle: {
    color: '#64748b',
  },
  notificationMessage: {
    fontSize: 14,
    color: '#64748b',
  },
  readMessage: {
    color: '#94a3b8',
  },
  unreadDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: '#667eea',
    marginTop: 6,
  },
  notificationDate: {
    fontSize: 12,
    color: '#94a3b8',
  },
  emptyState: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 40,
  },
  emptyStateText: {
    fontSize: 16,
    color: '#94a3b8',
  },
  loadingContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 40,
  },
  loadingText: {
    marginTop: 10,
    fontSize: 14,
    color: '#64748b',
  },
  loginPrompt: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 40,
  },
  loginTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 10,
  },
  loginText: {
    fontSize: 16,
    color: '#64748b',
    textAlign: 'center',
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  modalContainer: {
    backgroundColor: 'white',
    borderRadius: 12,
    margin: 20,
    maxHeight: '80%',
    minWidth: '90%',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 20,
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
  },
  modalClose: {
    fontSize: 18,
    color: '#64748b',
  },
  modalContent: {
    padding: 20,
  },
  formLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
    marginBottom: 8,
    marginTop: 12,
  },
  input: {
    borderWidth: 1,
    borderColor: '#e2e8f0',
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    color: '#333',
    backgroundColor: '#f8fafc',
  },
  pickerContainer: {
    borderWidth: 1,
    borderColor: '#e2e8f0',
    borderRadius: 8,
    backgroundColor: '#f8fafc',
  },
  picker: {
    height: 50,
  },
  modalActions: {
    flexDirection: 'row',
    padding: 20,
    borderTopWidth: 1,
    borderTopColor: '#e2e8f0',
  },
  cancelButton: {
    flex: 1,
    padding: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#e2e8f0',
    marginRight: 10,
    alignItems: 'center',
  },
  cancelButtonText: {
    fontSize: 16,
    color: '#64748b',
  },
  createButton: {
    flex: 1,
    padding: 12,
    borderRadius: 8,
    backgroundColor: '#667eea',
    alignItems: 'center',
  },
  createButtonText: {
    fontSize: 16,
    color: 'white',
    fontWeight: '600',
  },
});

export default AlarmsScreen;
