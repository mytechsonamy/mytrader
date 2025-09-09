import Constants from 'expo-constants';

const extra: any = (Constants as any).expoConfig?.extra || {};

export const API_BASE_URL: string = extra.API_BASE_URL || 'http://localhost:8080/api';
export const WS_BASE_URL: string = extra.WS_BASE_URL || 'ws://localhost:8080/hub';

