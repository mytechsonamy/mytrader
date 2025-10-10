import Constants from 'expo-constants';

const extra: any = (Constants as any).expoConfig?.extra || {};

// Debug: Log the configuration
console.log('Config Debug - __DEV__:', __DEV__);
console.log('Config Debug - Constants.isDevice:', Constants.isDevice);
console.log('Config Debug - extra:', extra);

// Use IP address from app.json or fallback to hardcoded for testing
const baseUrl = extra.API_BASE_URL?.replace('/api', '') || 'http://192.168.68.102:8080';

console.log('Config Debug - baseUrl:', baseUrl);

export const API_BASE_URL: string = `${baseUrl}/api`;
export const AUTH_BASE_URL: string = API_BASE_URL;
// Use WS_BASE_URL from app.json or fallback to market-data hub
export const WS_BASE_URL: string = extra.WS_BASE_URL || `${baseUrl}/hubs/market-data`;

console.log('Config Debug - API_BASE_URL:', API_BASE_URL);
console.log('Config Debug - WS_BASE_URL:', WS_BASE_URL);
