import Constants from 'expo-constants';

const extra: any = (Constants as any).expoConfig?.extra || {};

export const API_BASE_URL: string = extra.API_BASE_URL || 'http://localhost:5002/api';
// Default to the server's current SignalR hub route
export const WS_BASE_URL: string = extra.WS_BASE_URL || 'ws://localhost:5002/hubs/trading';
