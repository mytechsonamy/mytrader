import Constants from 'expo-constants';

const extra: any = (Constants as any).expoConfig?.extra || {};

// Debug: Log the configuration
console.log('Config Debug - __DEV__:', __DEV__);
console.log('Config Debug - Constants.isDevice:', Constants.isDevice);
console.log('Config Debug - extra:', extra);

// Force IP address for testing - ignore extra config
const baseUrl = 'http://192.168.68.103:5002';

console.log('Config Debug - baseUrl:', baseUrl);

export const API_BASE_URL: string = `${baseUrl}/api`;
export const AUTH_BASE_URL: string = API_BASE_URL;
// Default to the server's unified dashboard hub route
export const WS_BASE_URL: string = `${baseUrl}/hubs/dashboard`;

console.log('Config Debug - API_BASE_URL:', API_BASE_URL);
console.log('Config Debug - WS_BASE_URL:', WS_BASE_URL);
