import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  Modal,
  Alert,
  RefreshControl,
  Dimensions,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import { API_BASE_URL } from '../config';

const { width } = Dimensions.get('window');

interface LearningModule {
  id: string;
  title: string;
  description: string;
  category: string;
  difficulty: 'Beginner' | 'Intermediate' | 'Advanced';
  estimatedMinutes: number;
  thumbnailUrl: string;
  tags: string[];
  orderIndex: number;
  isPublished: boolean;
  progress?: LearningProgress;
}

interface LearningProgress {
  userId: string;
  moduleId: string;
  completedLessons: number;
  totalLessons: number;
  completionPercentage: number;
  timeSpentMinutes: number;
  startedAt?: string;
  completedAt?: string;
  lastAccessedAt: string;
}

interface LearningPath {
  id: string;
  name: string;
  description: string;
  category: string;
  difficulty: string;
  moduleIds: string[];
  modules: LearningModule[];
  estimatedHours: number;
  isRecommended: boolean;
  thumbnailUrl: string;
  pathProgress?: LearningProgress;
}

interface Quiz {
  id: string;
  title: string;
  description: string;
  questions: QuizQuestion[];
  passingScore: number;
  maxAttempts: number;
  timeLimit: number;
  lastResult?: QuizResult;
}

interface QuizQuestion {
  id: string;
  question: string;
  type: 'MultipleChoice' | 'TrueFalse' | 'FillInBlank';
  options: string[];
  correctAnswers: number[];
  explanation: string;
  points: number;
}

interface QuizResult {
  id: string;
  userId: string;
  quizId: string;
  score: number;
  totalPoints: number;
  percentage: number;
  passed: boolean;
  completedAt: string;
  attemptNumber: number;
}

const EducationScreen = () => {
  const { user, getAuthHeaders } = useAuth();
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [activeTab, setActiveTab] = useState<'modules' | 'paths' | 'quizzes'>('modules');
  const [modules, setModules] = useState<LearningModule[]>([]);
  const [paths, setPaths] = useState<LearningPath[]>([]);
  const [quizzes, setQuizzes] = useState<Quiz[]>([]);
  const [selectedModule, setSelectedModule] = useState<LearningModule | null>(null);
  const [selectedQuiz, setSelectedQuiz] = useState<Quiz | null>(null);
  const [showModuleModal, setShowModuleModal] = useState(false);
  const [showQuizModal, setShowQuizModal] = useState(false);

  const difficultyColors = {
    Beginner: '#10b981',
    Intermediate: '#f59e0b',
    Advanced: '#ef4444',
  };

  const categoryIcons: Record<string, string> = {
    'Basics': '📚',
    'Technical Analysis': '📈',
    'Risk Management': '🛡️',
    'Advanced': '🎓',
    'Strategy': '🎯',
    'Psychology': '🧠',
  };

  const fetchModules = async () => {
    try {
      const headers = await getAuthHeaders();
      const response = await fetch(`${API_BASE_URL}/education/modules`, {
        method: 'GET',
        headers: headers || {},
      });

      if (response.ok) {
        const data = await response.json();
        setModules(data.modules || []);
      } else {
        // Mock data for development
        setModules([
          {
            id: '1',
            title: 'Trading Temel Bilgileri',
            description: 'Temel trading kavramları, piyasa yapısı ve terminoloji',
            category: 'Basics',
            difficulty: 'Beginner',
            estimatedMinutes: 45,
            thumbnailUrl: '/images/basics.jpg',
            tags: ['temel', 'trading', 'piyasa'],
            orderIndex: 1,
            isPublished: true,
            progress: {
              userId: user?.id || '',
              moduleId: '1',
              completedLessons: 2,
              totalLessons: 5,
              completionPercentage: 40,
              timeSpentMinutes: 18,
              lastAccessedAt: '2024-01-16T10:00:00Z',
            },
          },
          {
            id: '2',
            title: 'Teknik Analiz Temelleri',
            description: 'Grafik okuma, indikatörler ve teknik analiz prensipleri',
            category: 'Technical Analysis',
            difficulty: 'Intermediate',
            estimatedMinutes: 90,
            thumbnailUrl: '/images/technical.jpg',
            tags: ['teknik analiz', 'grafik', 'indikatör'],
            orderIndex: 2,
            isPublished: true,
          },
          {
            id: '3',
            title: 'Risk Yönetimi',
            description: 'Position sizing, stop loss ve risk kontrol stratejileri',
            category: 'Risk Management',
            difficulty: 'Advanced',
            estimatedMinutes: 60,
            thumbnailUrl: '/images/risk.jpg',
            tags: ['risk', 'stop loss', 'position'],
            orderIndex: 3,
            isPublished: true,
          },
        ]);
      }
    } catch (error) {
      console.error('Error fetching modules:', error);
    }
  };

  const fetchPaths = async () => {
    try {
      const headers = await getAuthHeaders();
      const response = await fetch(`${API_BASE_URL}/education/paths`, {
        method: 'GET',
        headers: headers || {},
      });

      if (response.ok) {
        const data = await response.json();
        setPaths(data.paths || []);
      } else {
        // Mock data for development
        setPaths([
          {
            id: '1',
            name: 'Başlangıç Trader Yolu',
            description: 'Sıfırdan trading öğrenmek isteyenler için kapsamlı yol',
            category: 'Beginner',
            difficulty: 'Beginner',
            moduleIds: ['1', '2'],
            modules: [],
            estimatedHours: 8,
            isRecommended: true,
            thumbnailUrl: '/images/beginner-path.jpg',
          },
          {
            id: '2',
            name: 'Profesyonel Trader',
            description: 'İleri düzey trading stratejileri ve risk yönetimi',
            category: 'Advanced',
            difficulty: 'Advanced',
            moduleIds: ['2', '3'],
            modules: [],
            estimatedHours: 15,
            isRecommended: false,
            thumbnailUrl: '/images/advanced-path.jpg',
          },
        ]);
      }
    } catch (error) {
      console.error('Error fetching paths:', error);
    }
  };

  const fetchQuizzes = async () => {
    try {
      const headers = await getAuthHeaders();
      const response = await fetch(`${API_BASE_URL}/education/quizzes`, {
        method: 'GET',
        headers: headers || {},
      });

      if (response.ok) {
        const data = await response.json();
        setQuizzes(data.quizzes || []);
      } else {
        // Mock data for development
        setQuizzes([
          {
            id: '1',
            title: 'Trading Temel Bilgiler Testi',
            description: 'Temel trading kavramlarını ne kadar biliyorsun?',
            questions: [
              {
                id: '1',
                question: 'Piyasada alım satım işlemi yapmaya ne denir?',
                type: 'MultipleChoice',
                options: ['Trading', 'Banking', 'Saving', 'Investing'],
                correctAnswers: [0],
                explanation: 'Trading, piyasada alım satım işlemi yapmak anlamına gelir.',
                points: 10,
              },
              {
                id: '2',
                question: 'Stop Loss nedir?',
                type: 'MultipleChoice',
                options: [
                  'Kar alma emri',
                  'Zarar durdurma emri',
                  'Alım emri',
                  'Satım emri',
                ],
                correctAnswers: [1],
                explanation: 'Stop Loss, zararı belirli bir seviyede durdurmak için verilen emirdir.',
                points: 10,
              },
            ],
            passingScore: 70,
            maxAttempts: 3,
            timeLimit: 600,
            lastResult: {
              id: '1',
              userId: user?.id || '',
              quizId: '1',
              score: 18,
              totalPoints: 20,
              percentage: 90,
              passed: true,
              completedAt: '2024-01-15T14:30:00Z',
              attemptNumber: 1,
            },
          },
          {
            id: '2',
            title: 'Teknik Analiz Quiz',
            description: 'Teknik analiz bilgilerinizi test edin',
            questions: [],
            passingScore: 80,
            maxAttempts: 2,
            timeLimit: 900,
          },
        ]);
      }
    } catch (error) {
      console.error('Error fetching quizzes:', error);
    }
  };

  const startModule = async (moduleId: string) => {
    if (!user) {
      Alert.alert('Giriş Gerekli', 'Modülleri başlatmak için giriş yapmalısınız');
      return;
    }

    try {
      const headers = await getAuthHeaders();
      const response = await fetch(`${API_BASE_URL}/education/modules/${moduleId}/start`, {
        method: 'POST',
        headers,
      });

      if (response.ok) {
        Alert.alert('Başarılı', 'Modül başlatıldı!');
        fetchModules();
      } else {
        Alert.alert('Başarılı', 'Modül başlatıldı! (Demo)');
      }
    } catch (error) {
      console.error('Error starting module:', error);
      Alert.alert('Başarılı', 'Modül başlatıldı! (Demo)');
    }
  };

  const startQuiz = async (quizId: string) => {
    if (!user) {
      Alert.alert('Giriş Gerekli', 'Quiz başlatmak için giriş yapmalısınız');
      return;
    }

    const quiz = quizzes.find(q => q.id === quizId);
    if (quiz) {
      setSelectedQuiz(quiz);
      setShowQuizModal(true);
    }
  };

  const loadData = async () => {
    setLoading(true);
    await Promise.all([fetchModules(), fetchPaths(), fetchQuizzes()]);
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

  const renderModules = () => {
    const categorizedModules = modules.reduce((acc, module) => {
      if (!acc[module.category]) {
        acc[module.category] = [];
      }
      acc[module.category].push(module);
      return acc;
    }, {} as Record<string, LearningModule[]>);

    return (
      <View style={styles.modulesContainer}>
        {Object.entries(categorizedModules).map(([category, categoryModules]) => (
          <View key={category} style={styles.categorySection}>
            <Text style={styles.categoryTitle}>
              {categoryIcons[category] || '📖'} {category}
            </Text>
            {categoryModules.map((module) => (
              <TouchableOpacity
                key={module.id}
                style={styles.moduleCard}
                onPress={() => {
                  setSelectedModule(module);
                  setShowModuleModal(true);
                }}
              >
                <View style={styles.moduleHeader}>
                  <Text style={styles.moduleTitle}>{module.title}</Text>
                  <View
                    style={[
                      styles.difficultyBadge,
                      { backgroundColor: difficultyColors[module.difficulty] },
                    ]}
                  >
                    <Text style={styles.difficultyText}>{module.difficulty}</Text>
                  </View>
                </View>
                
                <Text style={styles.moduleDescription}>{module.description}</Text>
                
                <View style={styles.moduleFooter}>
                  <Text style={styles.moduleTime}>⏱️ {module.estimatedMinutes} dk</Text>
                  {module.progress && (
                    <View style={styles.progressContainer}>
                      <View style={styles.progressBar}>
                        <View
                          style={[
                            styles.progressFill,
                            { width: `${module.progress.completionPercentage}%` },
                          ]}
                        />
                      </View>
                      <Text style={styles.progressText}>
                        %{Math.round(module.progress.completionPercentage)}
                      </Text>
                    </View>
                  )}
                </View>
                
                <View style={styles.tagsContainer}>
                  {module.tags.slice(0, 3).map((tag, index) => (
                    <View key={index} style={styles.tag}>
                      <Text style={styles.tagText}>{tag}</Text>
                    </View>
                  ))}
                </View>
              </TouchableOpacity>
            ))}
          </View>
        ))}
      </View>
    );
  };

  const renderPaths = () => {
    const recommendedPaths = paths.filter(path => path.isRecommended);
    const otherPaths = paths.filter(path => !path.isRecommended);

    return (
      <View style={styles.pathsContainer}>
        {recommendedPaths.length > 0 && (
          <>
            <Text style={styles.sectionTitle}>⭐ Önerilen Yollar</Text>
            {recommendedPaths.map((path) => (
              <View key={path.id} style={[styles.pathCard, styles.recommendedPath]}>
                <View style={styles.pathHeader}>
                  <Text style={styles.pathTitle}>{path.name}</Text>
                  <Text style={styles.recommendedBadge}>Önerilen</Text>
                </View>
                <Text style={styles.pathDescription}>{path.description}</Text>
                <View style={styles.pathFooter}>
                  <Text style={styles.pathTime}>🕒 {path.estimatedHours} saat</Text>
                  <Text style={styles.pathModules}>📚 {path.moduleIds.length} modül</Text>
                  <View
                    style={[
                      styles.difficultyBadge,
                      { backgroundColor: difficultyColors[path.difficulty as keyof typeof difficultyColors] || '#64748b' },
                    ]}
                  >
                    <Text style={styles.difficultyText}>{path.difficulty}</Text>
                  </View>
                </View>
                <TouchableOpacity style={styles.startPathButton}>
                  <Text style={styles.startPathButtonText}>Yolu Başlat</Text>
                </TouchableOpacity>
              </View>
            ))}
          </>
        )}

        {otherPaths.length > 0 && (
          <>
            <Text style={styles.sectionTitle}>🛤️ Diğer Öğrenme Yolları</Text>
            {otherPaths.map((path) => (
              <View key={path.id} style={styles.pathCard}>
                <Text style={styles.pathTitle}>{path.name}</Text>
                <Text style={styles.pathDescription}>{path.description}</Text>
                <View style={styles.pathFooter}>
                  <Text style={styles.pathTime}>🕒 {path.estimatedHours} saat</Text>
                  <Text style={styles.pathModules}>📚 {path.moduleIds.length} modül</Text>
                  <View
                    style={[
                      styles.difficultyBadge,
                      { backgroundColor: difficultyColors[path.difficulty as keyof typeof difficultyColors] || '#64748b' },
                    ]}
                  >
                    <Text style={styles.difficultyText}>{path.difficulty}</Text>
                  </View>
                </View>
                <TouchableOpacity style={styles.startPathButton}>
                  <Text style={styles.startPathButtonText}>Yolu Başlat</Text>
                </TouchableOpacity>
              </View>
            ))}
          </>
        )}
      </View>
    );
  };

  const renderQuizzes = () => {
    const completedQuizzes = quizzes.filter(quiz => quiz.lastResult?.passed);
    const availableQuizzes = quizzes.filter(quiz => !quiz.lastResult?.passed);

    return (
      <View style={styles.quizzesContainer}>
        {availableQuizzes.length > 0 && (
          <>
            <Text style={styles.sectionTitle}>🎯 Mevcut Quizler</Text>
            {availableQuizzes.map((quiz) => (
              <View key={quiz.id} style={styles.quizCard}>
                <Text style={styles.quizTitle}>{quiz.title}</Text>
                <Text style={styles.quizDescription}>{quiz.description}</Text>
                
                <View style={styles.quizInfo}>
                  <Text style={styles.quizDetail}>❓ {quiz.questions.length} soru</Text>
                  <Text style={styles.quizDetail}>✅ Geçme: %{quiz.passingScore}</Text>
                  <Text style={styles.quizDetail}>🔄 {quiz.maxAttempts} deneme hakkı</Text>
                  {quiz.timeLimit > 0 && (
                    <Text style={styles.quizDetail}>⏰ {Math.floor(quiz.timeLimit / 60)} dakika</Text>
                  )}
                </View>
                
                <TouchableOpacity
                  style={styles.startQuizButton}
                  onPress={() => startQuiz(quiz.id)}
                >
                  <Text style={styles.startQuizButtonText}>Quiz Başlat</Text>
                </TouchableOpacity>
              </View>
            ))}
          </>
        )}

        {completedQuizzes.length > 0 && (
          <>
            <Text style={styles.sectionTitle}>✅ Tamamlanan Quizler</Text>
            {completedQuizzes.map((quiz) => (
              <View key={quiz.id} style={[styles.quizCard, styles.completedQuiz]}>
                <Text style={styles.quizTitle}>{quiz.title}</Text>
                <Text style={styles.quizDescription}>{quiz.description}</Text>
                
                {quiz.lastResult && (
                  <View style={styles.quizResult}>
                    <Text style={styles.resultText}>
                      Skor: {quiz.lastResult.score}/{quiz.lastResult.totalPoints} (%{quiz.lastResult.percentage})
                    </Text>
                    <Text style={styles.resultDate}>
                      {new Date(quiz.lastResult.completedAt).toLocaleDateString('tr-TR')}
                    </Text>
                  </View>
                )}
                
                <TouchableOpacity
                  style={styles.retakeQuizButton}
                  onPress={() => startQuiz(quiz.id)}
                >
                  <Text style={styles.retakeQuizButtonText}>Tekrar Çöz</Text>
                </TouchableOpacity>
              </View>
            ))}
          </>
        )}

        {quizzes.length === 0 && (
          <View style={styles.emptyState}>
            <Text style={styles.emptyStateText}>Henüz quiz yok</Text>
          </View>
        )}
      </View>
    );
  };

  const renderModuleModal = () => (
    <Modal visible={showModuleModal} animationType="slide" transparent>
      <View style={styles.modalOverlay}>
        <View style={styles.modalContainer}>
          {selectedModule && (
            <>
              <View style={styles.modalHeader}>
                <Text style={styles.modalTitle}>{selectedModule.title}</Text>
                <TouchableOpacity onPress={() => setShowModuleModal(false)}>
                  <Text style={styles.modalClose}>✖️</Text>
                </TouchableOpacity>
              </View>
              
              <ScrollView style={styles.modalContent}>
                <Text style={styles.modalDescription}>{selectedModule.description}</Text>
                
                <View style={styles.modalInfo}>
                  <Text style={styles.modalInfoItem}>
                    📚 Kategori: {selectedModule.category}
                  </Text>
                  <Text style={styles.modalInfoItem}>
                    📊 Seviye: {selectedModule.difficulty}
                  </Text>
                  <Text style={styles.modalInfoItem}>
                    ⏱️ Tahmini süre: {selectedModule.estimatedMinutes} dakika
                  </Text>
                </View>
                
                {selectedModule.progress && (
                  <View style={styles.progressInfo}>
                    <Text style={styles.progressTitle}>İlerleme Durumu</Text>
                    <View style={styles.progressContainer}>
                      <View style={styles.progressBar}>
                        <View
                          style={[
                            styles.progressFill,
                            { width: `${selectedModule.progress.completionPercentage}%` },
                          ]}
                        />
                      </View>
                      <Text style={styles.progressText}>
                        %{Math.round(selectedModule.progress.completionPercentage)}
                      </Text>
                    </View>
                    <Text style={styles.progressDetails}>
                      {selectedModule.progress.completedLessons} / {selectedModule.progress.totalLessons} ders tamamlandı
                    </Text>
                  </View>
                )}
                
                <View style={styles.tagsContainer}>
                  {selectedModule.tags.map((tag, index) => (
                    <View key={index} style={styles.tag}>
                      <Text style={styles.tagText}>{tag}</Text>
                    </View>
                  ))}
                </View>
              </ScrollView>
              
              <View style={styles.modalActions}>
                <TouchableOpacity
                  style={styles.startModuleButton}
                  onPress={() => {
                    startModule(selectedModule.id);
                    setShowModuleModal(false);
                  }}
                >
                  <Text style={styles.startModuleButtonText}>
                    {selectedModule.progress ? 'Devam Et' : 'Başlat'}
                  </Text>
                </TouchableOpacity>
              </View>
            </>
          )}
        </View>
      </View>
    </Modal>
  );

  const renderQuizModal = () => (
    <Modal visible={showQuizModal} animationType="slide" transparent>
      <View style={styles.modalOverlay}>
        <View style={styles.modalContainer}>
          {selectedQuiz && (
            <>
              <View style={styles.modalHeader}>
                <Text style={styles.modalTitle}>{selectedQuiz.title}</Text>
                <TouchableOpacity onPress={() => setShowQuizModal(false)}>
                  <Text style={styles.modalClose}>✖️</Text>
                </TouchableOpacity>
              </View>
              
              <ScrollView style={styles.modalContent}>
                <Text style={styles.modalDescription}>{selectedQuiz.description}</Text>
                
                <View style={styles.quizModalInfo}>
                  <Text style={styles.modalInfoItem}>❓ {selectedQuiz.questions.length} soru</Text>
                  <Text style={styles.modalInfoItem}>✅ Geçme notu: %{selectedQuiz.passingScore}</Text>
                  <Text style={styles.modalInfoItem}>🔄 {selectedQuiz.maxAttempts} deneme hakkı</Text>
                  {selectedQuiz.timeLimit > 0 && (
                    <Text style={styles.modalInfoItem}>
                      ⏰ Süre: {Math.floor(selectedQuiz.timeLimit / 60)} dakika
                    </Text>
                  )}
                </View>
                
                {selectedQuiz.lastResult && (
                  <View style={styles.lastResultInfo}>
                    <Text style={styles.lastResultTitle}>Son Sonuç</Text>
                    <Text style={styles.lastResultText}>
                      Skor: {selectedQuiz.lastResult.score}/{selectedQuiz.lastResult.totalPoints}
                    </Text>
                    <Text style={styles.lastResultText}>
                      Yüzde: %{selectedQuiz.lastResult.percentage}
                    </Text>
                    <Text style={styles.lastResultText}>
                      Durum: {selectedQuiz.lastResult.passed ? '✅ Geçti' : '❌ Kaldı'}
                    </Text>
                  </View>
                )}
              </ScrollView>
              
              <View style={styles.modalActions}>
                <TouchableOpacity
                  style={styles.startQuizModalButton}
                  onPress={() => {
                    Alert.alert('Quiz Başlatıldı', 'Quiz çözme ekranı yakında gelecek!');
                    setShowQuizModal(false);
                  }}
                >
                  <Text style={styles.startQuizModalButtonText}>Quiz Başlat</Text>
                </TouchableOpacity>
              </View>
            </>
          )}
        </View>
      </View>
    </Modal>
  );

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>📚 Eğitim</Text>
        <View style={styles.tabContainer}>
          <TouchableOpacity
            style={[styles.tab, activeTab === 'modules' && styles.activeTab]}
            onPress={() => setActiveTab('modules')}
          >
            <Text style={[styles.tabText, activeTab === 'modules' && styles.activeTabText]}>
              Modüller
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.tab, activeTab === 'paths' && styles.activeTab]}
            onPress={() => setActiveTab('paths')}
          >
            <Text style={[styles.tabText, activeTab === 'paths' && styles.activeTabText]}>
              Yollar
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.tab, activeTab === 'quizzes' && styles.activeTab]}
            onPress={() => setActiveTab('quizzes')}
          >
            <Text style={[styles.tabText, activeTab === 'quizzes' && styles.activeTabText]}>
              Quizler
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
            <Text style={styles.loadingText}>Eğitim içerikleri yükleniyor...</Text>
          </View>
        ) : (
          <>
            {activeTab === 'modules' && renderModules()}
            {activeTab === 'paths' && renderPaths()}
            {activeTab === 'quizzes' && renderQuizzes()}
          </>
        )}
      </ScrollView>

      {renderModuleModal()}
      {renderQuizModal()}
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
    paddingHorizontal: 12,
    borderRadius: 6,
    alignItems: 'center',
  },
  activeTab: {
    backgroundColor: '#667eea',
  },
  tabText: {
    fontSize: 13,
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
  modulesContainer: {
    marginBottom: 20,
  },
  categorySection: {
    marginBottom: 25,
  },
  categoryTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 15,
  },
  moduleCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 15,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  moduleHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 8,
  },
  moduleTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
    flex: 1,
    marginRight: 10,
  },
  moduleDescription: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 12,
    lineHeight: 20,
  },
  moduleFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  moduleTime: {
    fontSize: 12,
    color: '#94a3b8',
  },
  progressContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
    marginLeft: 15,
  },
  progressBar: {
    flex: 1,
    height: 6,
    backgroundColor: '#e2e8f0',
    borderRadius: 3,
    marginRight: 8,
  },
  progressFill: {
    height: '100%',
    backgroundColor: '#10b981',
    borderRadius: 3,
  },
  progressText: {
    fontSize: 11,
    color: '#10b981',
    fontWeight: '600',
  },
  tagsContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
  },
  tag: {
    backgroundColor: '#e2e8f0',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
    marginRight: 6,
    marginBottom: 4,
  },
  tagText: {
    fontSize: 10,
    color: '#64748b',
    fontWeight: '500',
  },
  difficultyBadge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  difficultyText: {
    color: 'white',
    fontSize: 10,
    fontWeight: 'bold',
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 15,
  },
  pathsContainer: {
    marginBottom: 20,
  },
  pathCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 15,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  recommendedPath: {
    borderWidth: 2,
    borderColor: '#f59e0b',
  },
  pathHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 8,
  },
  pathTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
    flex: 1,
  },
  recommendedBadge: {
    backgroundColor: '#f59e0b',
    color: 'white',
    fontSize: 10,
    fontWeight: 'bold',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  pathDescription: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 12,
    lineHeight: 20,
  },
  pathFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
  },
  pathTime: {
    fontSize: 12,
    color: '#94a3b8',
    marginRight: 15,
  },
  pathModules: {
    fontSize: 12,
    color: '#94a3b8',
    marginRight: 15,
  },
  startPathButton: {
    backgroundColor: '#667eea',
    paddingVertical: 10,
    borderRadius: 8,
    alignItems: 'center',
  },
  startPathButtonText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  quizzesContainer: {
    marginBottom: 20,
  },
  quizCard: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 15,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  completedQuiz: {
    backgroundColor: '#f0f9ff',
    borderLeftWidth: 4,
    borderLeftColor: '#10b981',
  },
  quizTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 8,
  },
  quizDescription: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 12,
    lineHeight: 20,
  },
  quizInfo: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginBottom: 12,
  },
  quizDetail: {
    fontSize: 12,
    color: '#94a3b8',
    marginRight: 15,
    marginBottom: 4,
  },
  startQuizButton: {
    backgroundColor: '#10b981',
    paddingVertical: 10,
    borderRadius: 8,
    alignItems: 'center',
  },
  startQuizButtonText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  retakeQuizButton: {
    backgroundColor: '#64748b',
    paddingVertical: 8,
    borderRadius: 8,
    alignItems: 'center',
  },
  retakeQuizButtonText: {
    color: 'white',
    fontSize: 12,
    fontWeight: '600',
  },
  quizResult: {
    backgroundColor: '#f1f5f9',
    padding: 10,
    borderRadius: 8,
    marginBottom: 12,
  },
  resultText: {
    fontSize: 14,
    color: '#333',
    fontWeight: '600',
    marginBottom: 4,
  },
  resultDate: {
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
    width: width - 40,
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
    flex: 1,
  },
  modalClose: {
    fontSize: 18,
    color: '#64748b',
  },
  modalContent: {
    padding: 20,
  },
  modalDescription: {
    fontSize: 16,
    color: '#64748b',
    lineHeight: 24,
    marginBottom: 20,
  },
  modalInfo: {
    marginBottom: 20,
  },
  modalInfoItem: {
    fontSize: 14,
    color: '#333',
    marginBottom: 8,
  },
  progressInfo: {
    backgroundColor: '#f8fafc',
    padding: 15,
    borderRadius: 8,
    marginBottom: 20,
  },
  progressTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 10,
  },
  progressDetails: {
    fontSize: 12,
    color: '#64748b',
    marginTop: 5,
  },
  quizModalInfo: {
    backgroundColor: '#f8fafc',
    padding: 15,
    borderRadius: 8,
    marginBottom: 20,
  },
  lastResultInfo: {
    backgroundColor: '#f0f9ff',
    padding: 15,
    borderRadius: 8,
    marginBottom: 20,
  },
  lastResultTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 10,
  },
  lastResultText: {
    fontSize: 14,
    color: '#333',
    marginBottom: 4,
  },
  modalActions: {
    padding: 20,
    borderTopWidth: 1,
    borderTopColor: '#e2e8f0',
  },
  startModuleButton: {
    backgroundColor: '#667eea',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  startModuleButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  startQuizModalButton: {
    backgroundColor: '#10b981',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  startQuizModalButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
});

export default EducationScreen;