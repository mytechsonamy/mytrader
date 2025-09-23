import React, { memo, useState, useEffect } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
} from 'react-native';

interface NewsSearchProps {
  placeholder?: string;
  onSearch: (query: string) => void;
  onClear?: () => void;
  value?: string;
  showRecentSearches?: boolean;
  recentSearches?: string[];
  onRecentSearchPress?: (query: string) => void;
  showSuggestions?: boolean;
  suggestions?: string[];
  onSuggestionPress?: (suggestion: string) => void;
  isLoading?: boolean;
  debounceMs?: number;
  autoFocus?: boolean;
  variant?: 'default' | 'compact';
}

const NewsSearch: React.FC<NewsSearchProps> = memo(({
  placeholder = 'Haberlerde ara...',
  onSearch,
  onClear,
  value: externalValue,
  showRecentSearches = true,
  recentSearches = [],
  onRecentSearchPress,
  showSuggestions = true,
  suggestions = [],
  onSuggestionPress,
  isLoading = false,
  debounceMs = 300,
  autoFocus = false,
  variant = 'default',
}) => {
  const [internalValue, setInternalValue] = useState(externalValue || '');
  const [isFocused, setIsFocused] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);

  const value = externalValue !== undefined ? externalValue : internalValue;

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
      if (value.trim()) {
        onSearch(value.trim());
      }
    }, debounceMs);

    return () => clearTimeout(timer);
  }, [value, onSearch, debounceMs]);

  const handleValueChange = (text: string) => {
    if (externalValue === undefined) {
      setInternalValue(text);
    }

    if (text.trim()) {
      setShowDropdown(true);
    } else {
      setShowDropdown(false);
      onClear?.();
    }
  };

  const handleClear = () => {
    if (externalValue === undefined) {
      setInternalValue('');
    }
    setShowDropdown(false);
    onClear?.();
  };

  const handleRecentSearchPress = (query: string) => {
    if (externalValue === undefined) {
      setInternalValue(query);
    }
    setShowDropdown(false);
    onRecentSearchPress?.(query);
    onSearch(query);
  };

  const handleSuggestionPress = (suggestion: string) => {
    if (externalValue === undefined) {
      setInternalValue(suggestion);
    }
    setShowDropdown(false);
    onSuggestionPress?.(suggestion);
    onSearch(suggestion);
  };

  const handleFocus = () => {
    setIsFocused(true);
    if ((showRecentSearches && recentSearches.length > 0) ||
        (showSuggestions && suggestions.length > 0)) {
      setShowDropdown(true);
    }
  };

  const handleBlur = () => {
    setIsFocused(false);
    // Delay hiding dropdown to allow for taps
    setTimeout(() => setShowDropdown(false), 150);
  };

  const handleSubmit = () => {
    if (value.trim()) {
      setShowDropdown(false);
      onSearch(value.trim());
    }
  };

  const filteredSuggestions = suggestions.filter(suggestion =>
    suggestion.toLowerCase().includes(value.toLowerCase())
  ).slice(0, 5);

  const shouldShowRecents = showRecentSearches &&
    recentSearches.length > 0 &&
    !value.trim() &&
    isFocused;

  const shouldShowSuggestions = showSuggestions &&
    filteredSuggestions.length > 0 &&
    value.trim() &&
    isFocused;

  const containerStyle = variant === 'compact' ? styles.compactContainer : styles.container;
  const inputStyle = variant === 'compact' ? styles.compactInput : styles.input;

  return (
    <View style={containerStyle}>
      <View style={styles.searchInputContainer}>
        <View style={styles.searchIconContainer}>
          <Text style={styles.searchIcon}>🔍</Text>
        </View>

        <TextInput
          style={[inputStyle, isFocused && styles.inputFocused]}
          value={value}
          onChangeText={handleValueChange}
          onFocus={handleFocus}
          onBlur={handleBlur}
          onSubmitEditing={handleSubmit}
          placeholder={placeholder}
          placeholderTextColor="#9ca3af"
          autoFocus={autoFocus}
          returnKeyType="search"
          clearButtonMode="never"
        />

        <View style={styles.rightActions}>
          {isLoading && (
            <ActivityIndicator size="small" color="#667eea" style={styles.loader} />
          )}

          {value.length > 0 && (
            <TouchableOpacity
              onPress={handleClear}
              style={styles.clearButton}
              hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
            >
              <Text style={styles.clearIcon}>✕</Text>
            </TouchableOpacity>
          )}
        </View>
      </View>

      {showDropdown && (shouldShowRecents || shouldShowSuggestions) && (
        <View style={styles.dropdown}>
          <ScrollView
            style={styles.dropdownScroll}
            keyboardShouldPersistTaps="handled"
            showsVerticalScrollIndicator={false}
          >
            {shouldShowRecents && (
              <View style={styles.section}>
                <Text style={styles.sectionTitle}>Son Aramalar</Text>
                {recentSearches.slice(0, 5).map((search, index) => (
                  <TouchableOpacity
                    key={index}
                    style={styles.dropdownItem}
                    onPress={() => handleRecentSearchPress(search)}
                  >
                    <Text style={styles.recentIcon}>🕒</Text>
                    <Text style={styles.dropdownItemText}>{search}</Text>
                  </TouchableOpacity>
                ))}
              </View>
            )}

            {shouldShowSuggestions && (
              <View style={styles.section}>
                <Text style={styles.sectionTitle}>Öneriler</Text>
                {filteredSuggestions.map((suggestion, index) => (
                  <TouchableOpacity
                    key={index}
                    style={styles.dropdownItem}
                    onPress={() => handleSuggestionPress(suggestion)}
                  >
                    <Text style={styles.suggestionIcon}>💡</Text>
                    <Text style={styles.dropdownItemText}>{suggestion}</Text>
                  </TouchableOpacity>
                ))}
              </View>
            )}
          </ScrollView>
        </View>
      )}
    </View>
  );
});

const styles = StyleSheet.create({
  container: {
    position: 'relative',
    zIndex: 1000,
  },
  compactContainer: {
    position: 'relative',
    zIndex: 1000,
  },
  searchInputContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: 'white',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#e2e8f0',
    paddingHorizontal: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  searchIconContainer: {
    marginRight: 8,
  },
  searchIcon: {
    fontSize: 16,
    color: '#9ca3af',
  },
  input: {
    flex: 1,
    fontSize: 16,
    color: '#1f2937',
    paddingVertical: 12,
    paddingHorizontal: 0,
  },
  compactInput: {
    flex: 1,
    fontSize: 14,
    color: '#1f2937',
    paddingVertical: 8,
    paddingHorizontal: 0,
  },
  inputFocused: {
    // You can add focused styles here if needed
  },
  rightActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  loader: {
    marginLeft: 4,
  },
  clearButton: {
    padding: 4,
  },
  clearIcon: {
    fontSize: 14,
    color: '#9ca3af',
    fontWeight: 'bold',
  },
  dropdown: {
    position: 'absolute',
    top: '100%',
    left: 0,
    right: 0,
    backgroundColor: 'white',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#e2e8f0',
    borderTopWidth: 0,
    borderTopLeftRadius: 0,
    borderTopRightRadius: 0,
    maxHeight: 300,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 5,
    zIndex: 1001,
  },
  dropdownScroll: {
    maxHeight: 300,
  },
  section: {
    paddingVertical: 8,
  },
  sectionTitle: {
    fontSize: 12,
    fontWeight: '600',
    color: '#6b7280',
    paddingHorizontal: 16,
    paddingVertical: 8,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  dropdownItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  recentIcon: {
    fontSize: 14,
    marginRight: 12,
    color: '#9ca3af',
  },
  suggestionIcon: {
    fontSize: 14,
    marginRight: 12,
    color: '#f59e0b',
  },
  dropdownItemText: {
    fontSize: 14,
    color: '#374151',
    flex: 1,
  },
});

export default NewsSearch;