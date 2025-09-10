export type RootStackParamList = {
  MainTabs: undefined;
  AuthStack: undefined;
  StrategyTest: { strategyId?: string };
};

export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
  ForgotPasswordStart: undefined;
  ForgotPasswordVerify: { email: string };
  ResetPassword: { token: string; email: string };
};

export type MainTabsParamList = {
  Dashboard: undefined;
  News: undefined;
  Strategies: undefined;
  Gamification: undefined;
  Alarms: undefined;
  Education: undefined;
  Profile: undefined;
};