import React from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
} from 'react-native';

interface EducationItem {
  id: string;
  title: string;
  description: string;
  icon: string;
  duration: string;
  difficulty: 'Başlangıç' | 'Orta' | 'İleri';
  category: 'trading' | 'analysis' | 'risk' | 'psychology';
  isAvailable: boolean;
}

interface EducationSectionProps {
  onItemPress?: (item: EducationItem) => void;
}

const EducationSection: React.FC<EducationSectionProps> = ({ onItemPress }) => {
  const educationItems: EducationItem[] = [
    {
      id: '1',
      title: 'Trading Temelleri',
      description: 'Temel trading kavramları ve terminolojisi',
      icon: '📈',
      duration: '2 saat',
      difficulty: 'Başlangıç',
      category: 'trading',
      isAvailable: true,
    },
    {
      id: '2',
      title: 'Teknik Analiz',
      description: 'Grafikler, göstergeler ve fiyat formasyonları',
      icon: '📊',
      duration: '4 saat',
      difficulty: 'Orta',
      category: 'analysis',
      isAvailable: true,
    },
    {
      id: '3',
      title: 'Risk Yönetimi',
      description: 'Sermaye korunması ve risk kontrolü',
      icon: '🛡️',
      duration: '3 saat',
      difficulty: 'Orta',
      category: 'risk',
      isAvailable: true,
    },
    {
      id: '4',
      title: 'Trading Psikolojisi',
      description: 'Duygusal kontrol ve mental disiplin',
      icon: '🧠',
      duration: '2.5 saat',
      difficulty: 'İleri',
      category: 'psychology',
      isAvailable: false,
    },
    {
      id: '5',
      title: 'Portföy Yönetimi',
      description: 'Çeşitlendirme ve varlık tahsisi',
      icon: '💼',
      duration: '3.5 saat',
      difficulty: 'İleri',
      category: 'trading',
      isAvailable: false,
    },
  ];

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty) {
      case 'Başlangıç': return '#10b981';
      case 'Orta': return '#f59e0b';
      case 'İleri': return '#ef4444';
      default: return '#6b7280';
    }
  };

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case 'trading': return '💹';
      case 'analysis': return '📊';
      case 'risk': return '🛡️';
      case 'psychology': return '🧠';
      default: return '📚';
    }
  };

  return (
    <ScrollView style={styles.container} showsVerticalScrollIndicator={false}>
      <View style={styles.header}>
        <Text style={styles.title}>📚 Eğitim Merkezi</Text>
        <Text style={styles.subtitle}>
          Trading becerilerinizi geliştirmek için kapsamlı eğitim programları
        </Text>
      </View>

      <View style={styles.itemsContainer}>
        {educationItems.map((item) => (
          <TouchableOpacity
            key={item.id}
            style={[styles.educationItem, !item.isAvailable && styles.disabledItem]}
            onPress={() => onItemPress?.(item)}
            disabled={!item.isAvailable}
          >
            <View style={styles.itemHeader}>
              <Text style={styles.itemIcon}>{item.icon}</Text>
              <View style={styles.itemInfo}>
                <Text style={[styles.itemTitle, !item.isAvailable && styles.disabledText]}>
                  {item.title}
                </Text>
                <Text style={[styles.itemDescription, !item.isAvailable && styles.disabledText]}>
                  {item.description}
                </Text>
              </View>
              {!item.isAvailable && (
                <View style={styles.comingSoonBadge}>
                  <Text style={styles.comingSoonText}>Yakında</Text>
                </View>
              )}
            </View>

            <View style={styles.itemFooter}>
              <View style={styles.metaInfo}>
                <View style={styles.metaItem}>
                  <Text style={styles.metaIcon}>⏱️</Text>
                  <Text style={[styles.metaText, !item.isAvailable && styles.disabledText]}>
                    {item.duration}
                  </Text>
                </View>

                <View style={[
                  styles.difficultyBadge,
                  { backgroundColor: item.isAvailable ? getDifficultyColor(item.difficulty) : '#e5e7eb' }
                ]}>
                  <Text style={[
                    styles.difficultyText,
                    { color: item.isAvailable ? 'white' : '#9ca3af' }
                  ]}>
                    {item.difficulty}
                  </Text>
                </View>

                <View style={styles.metaItem}>
                  <Text style={styles.metaIcon}>{getCategoryIcon(item.category)}</Text>
                  <Text style={[styles.metaText, !item.isAvailable && styles.disabledText]}>
                    {item.category}
                  </Text>
                </View>
              </View>

              {item.isAvailable && (
                <Text style={styles.startText}>Başla →</Text>
              )}
            </View>
          </TouchableOpacity>
        ))}
      </View>

      <View style={styles.footer}>
        <Text style={styles.footerText}>
          🚀 Daha fazla eğitim içeriği yakında eklenecek!
        </Text>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  header: {
    padding: 20,
    backgroundColor: 'white',
    borderBottomWidth: 1,
    borderBottomColor: '#e5e7eb',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1f2937',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 14,
    color: '#6b7280',
    lineHeight: 20,
  },
  itemsContainer: {
    padding: 16,
  },
  educationItem: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  disabledItem: {
    opacity: 0.6,
  },
  itemHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginBottom: 12,
  },
  itemIcon: {
    fontSize: 24,
    marginRight: 12,
    marginTop: 2,
  },
  itemInfo: {
    flex: 1,
  },
  itemTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1f2937',
    marginBottom: 4,
  },
  itemDescription: {
    fontSize: 14,
    color: '#6b7280',
    lineHeight: 20,
  },
  disabledText: {
    color: '#9ca3af',
  },
  comingSoonBadge: {
    backgroundColor: '#fef3c7',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#f59e0b',
  },
  comingSoonText: {
    fontSize: 11,
    fontWeight: '600',
    color: '#d97706',
  },
  itemFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  metaInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  metaItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  metaIcon: {
    fontSize: 12,
  },
  metaText: {
    fontSize: 12,
    color: '#6b7280',
    fontWeight: '500',
  },
  difficultyBadge: {
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 8,
  },
  difficultyText: {
    fontSize: 11,
    fontWeight: '600',
  },
  startText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#667eea',
  },
  footer: {
    padding: 20,
    alignItems: 'center',
  },
  footerText: {
    fontSize: 14,
    color: '#6b7280',
    textAlign: 'center',
    fontStyle: 'italic',
  },
});

export default EducationSection;