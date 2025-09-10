// UserMenu.tsx
import React from 'react';
import { View, Text, Button } from 'react-native';

export default function UserMenu() {
  // Wire with /api/users/me and PATCH /api/users/me
  return (
    <View style={{ padding: 16 }}>
      <Text style={{ fontSize: 18, fontWeight: '600' }}>Kullanıcı</Text>
      <Text>Email: (from /api/users/me)</Text>
      <Button title="Profili Düzenle" onPress={() => { /* navigate */ }} />
      <Button title="Tercihler" onPress={() => { /* navigate */ }} />
      <Button title="Çıkış" onPress={() => { /* logout */ }} />
    </View>
  );
}
