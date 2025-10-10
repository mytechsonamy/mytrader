import React, { useState } from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { DashboardErrorBoundary, AccordionErrorBoundary } from '../components/dashboard/ErrorBoundary';

// Component that can throw an error on demand
const ThrowingComponent: React.FC<{ shouldThrow: boolean; message: string }> = ({
  shouldThrow,
  message
}) => {
  if (shouldThrow) {
    throw new Error(message);
  }
  return (
    <View style={styles.successContainer}>
      <Text style={styles.successText}>✅ Component is working!</Text>
    </View>
  );
};

export const TestErrorBoundaryScreen: React.FC = () => {
  const [dashboardError, setDashboardError] = useState(false);
  const [accordionError, setAccordionError] = useState(false);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Error Boundary Test Screen</Text>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Dashboard Error Boundary Test</Text>
        <TouchableOpacity
          style={[styles.button, dashboardError && styles.errorButton]}
          onPress={() => setDashboardError(!dashboardError)}
        >
          <Text style={styles.buttonText}>
            {dashboardError ? 'Fix Dashboard' : 'Trigger Dashboard Error'}
          </Text>
        </TouchableOpacity>

        <DashboardErrorBoundary>
          <ThrowingComponent
            shouldThrow={dashboardError}
            message="Dashboard test error"
          />
        </DashboardErrorBoundary>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Accordion Error Boundary Test</Text>
        <TouchableOpacity
          style={[styles.button, accordionError && styles.errorButton]}
          onPress={() => setAccordionError(!accordionError)}
        >
          <Text style={styles.buttonText}>
            {accordionError ? 'Fix Accordion' : 'Trigger Accordion Error'}
          </Text>
        </TouchableOpacity>

        <AccordionErrorBoundary sectionName="Test Section">
          <ThrowingComponent
            shouldThrow={accordionError}
            message="Accordion test error"
          />
        </AccordionErrorBoundary>
      </View>

      <View style={styles.infoBox}>
        <Text style={styles.infoTitle}>Test Instructions:</Text>
        <Text style={styles.infoText}>
          1. Click "Trigger Dashboard Error" to test DashboardErrorBoundary
        </Text>
        <Text style={styles.infoText}>
          2. Click "Trigger Accordion Error" to test AccordionErrorBoundary
        </Text>
        <Text style={styles.infoText}>
          3. Use the retry buttons in error UI to recover
        </Text>
        <Text style={styles.infoText}>
          4. Check console for error logs
        </Text>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
    padding: 20,
    paddingTop: 60,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1f2937',
    marginBottom: 20,
    textAlign: 'center',
  },
  section: {
    marginBottom: 30,
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#374151',
    marginBottom: 12,
  },
  button: {
    backgroundColor: '#667eea',
    borderRadius: 8,
    paddingHorizontal: 20,
    paddingVertical: 12,
    marginBottom: 16,
    alignItems: 'center',
  },
  errorButton: {
    backgroundColor: '#dc2626',
  },
  buttonText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  successContainer: {
    backgroundColor: '#ecfdf5',
    borderRadius: 8,
    padding: 16,
    alignItems: 'center',
  },
  successText: {
    fontSize: 16,
    color: '#059669',
    fontWeight: '500',
  },
  infoBox: {
    backgroundColor: '#eff6ff',
    borderRadius: 8,
    padding: 16,
    borderWidth: 1,
    borderColor: '#bfdbfe',
  },
  infoTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1e40af',
    marginBottom: 8,
  },
  infoText: {
    fontSize: 14,
    color: '#3730a3',
    marginBottom: 4,
  },
});

export default TestErrorBoundaryScreen;