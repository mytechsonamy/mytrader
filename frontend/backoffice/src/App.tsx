import React, { useEffect, useState } from 'react'
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom'

const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080/api'

function useToken() {
  // TODO: implement real auth
  return localStorage.getItem('token') || ''
}

interface Symbol {
  id: string;
  ticker: string;
  display: string;
  venue: string;
  baseCcy: string;
  quoteCcy: string;
  isTracked: boolean;
}

interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  preferences?: any;
}

function Dashboard() {
  const token = useToken()
  const [symbols, setSymbols] = useState<Symbol[]>([])
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const fetchData = async () => {
      try {
        const headers = token ? { Authorization: `Bearer ${token}` } : {}
        
        // Fetch symbols and user data in parallel
        const [symbolsResponse, userResponse] = await Promise.all([
          fetch(`${baseUrl}/symbols/tracked`, { headers }),
          fetch(`${baseUrl}/users/me`, { headers })
        ])

        if (symbolsResponse.ok) {
          const symbolsData = await symbolsResponse.json()
          setSymbols(symbolsData)
        }

        if (userResponse.ok) {
          const userData = await userResponse.json()
          setUser(userData)
        }
      } catch (error) {
        console.error('Error fetching data:', error)
      } finally {
        setLoading(false)
      }
    }

    fetchData()
  }, [token])

  if (loading) {
    return <div style={styles.loading}>Yükleniyor...</div>
  }

  return (
    <div style={styles.container}>
      <h1>myTrader Backoffice</h1>
      
      <nav style={styles.nav}>
        <Link to="/">Dashboard</Link>
        <Link to="/symbols">Semboller</Link>
        <Link to="/strategies">Stratejiler</Link>
        <Link to="/indicators">İndikatörler</Link>
      </nav>

      <section style={styles.section}>
        <h2>Kullanıcı Bilgisi</h2>
        {user ? (
          <div>
            <p><strong>Email:</strong> {user.email}</p>
            <p><strong>Ad:</strong> {user.firstName} {user.lastName}</p>
            <p><strong>ID:</strong> {user.id}</p>
          </div>
        ) : (
          <p>Kullanıcı bilgisi yüklenemedi</p>
        )}
      </section>

      <section style={styles.section}>
        <h2>Takip Edilen Semboller</h2>
        {symbols.length > 0 ? (
          <ul>
            {symbols.map(symbol => (
              <li key={symbol.id} style={styles.symbolItem}>
                <strong>{symbol.ticker}</strong> ({symbol.display}) 
                - {symbol.venue} - {symbol.baseCcy}/{symbol.quoteCcy}
                {symbol.isTracked && <span style={styles.tracked}>✓ Takipte</span>}
              </li>
            ))}
          </ul>
        ) : (
          <p>Henüz takip edilen sembol bulunamadı</p>
        )}
      </section>

      <section style={styles.section}>
        <h2>Hızlı Aksiyonlar</h2>
        <button onClick={() => alert('Stratejiler sayfası yakında eklenecek')}>
          Stratejileri Görüntüle
        </button>
        <button onClick={() => alert('Backtest tetikleme yakında eklenecek')}>
          Backtest Tetikle
        </button>
        <button onClick={() => alert('Yeni sembol ekleme yakında eklenecek')}>
          Yeni Sembol Ekle
        </button>
      </section>
    </div>
  )
}

function SymbolsPage() {
  return <div style={styles.container}><h2>Semboller</h2><p>TODO: Sembol yönetimi</p></div>
}

function StrategiesPage() {
  return <div style={styles.container}><h2>Stratejiler</h2><p>TODO: Strateji yönetimi</p></div>
}

function IndicatorsPage() {
  return <div style={styles.container}><h2>İndikatörler</h2><p>TODO: İndikatör yönetimi</p></div>
}

export default function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/symbols" element={<SymbolsPage />} />
        <Route path="/strategies" element={<StrategiesPage />} />
        <Route path="/indicators" element={<IndicatorsPage />} />
      </Routes>
    </Router>
  )
}

const styles = {
  container: {
    padding: '20px',
    fontFamily: 'system-ui, -apple-system, sans-serif',
    maxWidth: '1200px',
    margin: '0 auto'
  },
  nav: {
    display: 'flex',
    gap: '16px',
    marginBottom: '24px',
    paddingBottom: '16px',
    borderBottom: '1px solid #eee'
  } as React.CSSProperties,
  section: {
    marginBottom: '32px',
    padding: '16px',
    border: '1px solid #ddd',
    borderRadius: '8px'
  },
  symbolItem: {
    marginBottom: '8px',
    padding: '8px',
    backgroundColor: '#f8f9fa',
    borderRadius: '4px'
  },
  tracked: {
    color: '#28a745',
    marginLeft: '8px'
  },
  loading: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    height: '100vh',
    fontSize: '18px'
  }
}