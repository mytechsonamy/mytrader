import React, { useEffect, useState } from 'react'

const baseUrl = import.meta.env.VITE_API || 'http://localhost:5000'

function useToken() {
  // TODO: implement real auth
  return ''
}

export default function App() {
  const token = useToken()
  const [symbols, setSymbols] = useState([])
  const [me, setMe] = useState(null)

  useEffect(() => {
    const hdr = token ? { Authorization: `Bearer ${token}` } : {}
    fetch(`${baseUrl}/api/symbols/tracked`, { headers: hdr }).then(r => r.json()).then(setSymbols)
    fetch(`${baseUrl}/api/users/me`, { headers: hdr }).then(r => r.json()).then(setMe)
  }, [token])

  return (
    <div style={{ padding: 20, fontFamily: 'system-ui' }}>
      <h1>myTrader Backoffice</h1>
      <section>
        <h2>Kullanıcı</h2>
        <pre>{JSON.stringify(me, null, 2)}</pre>
      </section>
      <section>
        <h2>Semboller</h2>
        <ul>
          {symbols.map(s => <li key={s.id}>{s.ticker} ({s.display})</li>)}
        </ul>
      </section>
      <section>
        <h2>Stratejiler</h2>
        <p>TODO: Listele, backtest tetikleme, metrikler</p>
      </section>
      <section>
        <h2>İndikatörler</h2>
        <p>TODO: Yeni indikatör ekleme formu</p>
      </section>
    </div>
  )
}
