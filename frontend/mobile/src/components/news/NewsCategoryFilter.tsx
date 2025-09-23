import React, { memo } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  ScrollView,
  StyleSheet,
} from 'react-native';

export interface NewsCategory {
  key: string;
  label: string;
  emoji: string;
  color: string;
  count?: number;
}

interface NewsCategoryFilterProps {
  categories: NewsCategory[];
  selectedCategory: string;
  onCategorySelect: (categoryKey: string) => void;
  showCounts?: boolean;
  horizontal?: boolean;
  compact?: boolean;
}

const NewsCategoryFilter: React.FC<NewsCategoryFilterProps> = memo(({
  categories,
  selectedCategory,
  onCategorySelect,
  showCounts = true,
  horizontal = true,
  compact = false,
}) => {
  const containerStyle = horizontal ? styles.horizontalContainer : styles.verticalContainer;
  const scrollStyle = horizontal ? styles.horizontalScroll : styles.verticalScroll;

  return (
    <View style={[containerStyle, compact && styles.compactContainer]}>
      <ScrollView
        horizontal={horizontal}
        showsHorizontalScrollIndicator={false}
        showsVerticalScrollIndicator={false}
        style={scrollStyle}
        contentContainerStyle={
          horizontal ? styles.horizontalContent : styles.verticalContent
        }
      >
        {categories.map((category) => {
          const isSelected = selectedCategory === category.key;
          const hasCount = showCounts && category.count !== undefined;

          return (
            <TouchableOpacity
              key={category.key}
              style={[
                styles.categoryButton,
                compact && styles.compactButton,
                isSelected && styles.selectedButton,
                isSelected && { borderColor: category.color },
              ]}
              onPress={() => onCategorySelect(category.key)}
              activeOpacity={0.8}
            >
              <View style={styles.buttonContent}>
                <Text style={styles.categoryEmoji}>{category.emoji}</Text>
                <Text
                  style={[
                    styles.categoryLabel,
                    compact && styles.compactLabel,
                    isSelected && styles.selectedLabel,
                  ]}
                >
                  {category.label}
                </Text>
                {hasCount && (
                  <View
                    style={[
                      styles.countBadge,
                      { backgroundColor: isSelected ? category.color : '#e2e8f0' },
                    ]}
                  >
                    <Text
                      style={[
                        styles.countText,
                        { color: isSelected ? 'white' : '#64748b' },
                      ]}
                    >
                      {category.count}
                    </Text>
                  </View>
                )}
              </View>
            </TouchableOpacity>
          );
        })}
      </ScrollView>
    </View>
  );
});

const styles = StyleSheet.create({
  horizontalContainer: {
    maxHeight: 60,
    marginBottom: 10,
  },
  verticalContainer: {
    flex: 1,
  },
  compactContainer: {
    maxHeight: 45,
  },
  horizontalScroll: {
    flex: 1,
  },
  verticalScroll: {
    flex: 1,
  },
  horizontalContent: {
    paddingHorizontal: 16,
    paddingVertical: 5,
    alignItems: 'center',
  },
  verticalContent: {
    padding: 16,
  },
  categoryButton: {
    backgroundColor: 'rgba(255, 255, 255, 0.9)',
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 10,
    marginRight: 10,
    marginBottom: 8,
    borderWidth: 2,
    borderColor: 'transparent',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  compactButton: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
  },
  selectedButton: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderWidth: 2,
  },
  buttonContent: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  categoryEmoji: {
    fontSize: 16,
  },
  categoryLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#374151',
  },
  compactLabel: {
    fontSize: 12,
  },
  selectedLabel: {
    fontWeight: '700',
    color: '#1f2937',
  },
  countBadge: {
    minWidth: 20,
    height: 20,
    borderRadius: 10,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 6,
  },
  countText: {
    fontSize: 11,
    fontWeight: '700',
  },
});

export default NewsCategoryFilter;