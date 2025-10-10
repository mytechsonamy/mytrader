/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_BACKEND_URL: string
  readonly VITE_WEBSOCKET_HUB_PATH: string
  readonly VITE_API_VERSION: string
  readonly VITE_DEBUG_WEBSOCKET: string
  readonly VITE_ENABLE_MOCK_DATA: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}