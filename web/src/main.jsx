import React from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'

async function request(path, options = {}) {
  const token = localStorage.getItem('sentinel_token')
  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers || {}),
    },
  })
  if (!response.ok) {
    const body = await response.text()
    throw new Error(body || `${response.status} request failed`)
  }
  return response.status === 204 ? null : response.json()
}

function Auth({ onAuthenticated }) {
  const [mode, setMode] = React.useState('login')
  const [email, setEmail] = React.useState('')
  const [password, setPassword] = React.useState('')
  const [error, setError] = React.useState('')
  const [busy, setBusy] = React.useState(false)

  async function submit(event) {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      const data = await request(`/api/auth/${mode}`, {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      })
      localStorage.setItem('sentinel_token', data.accessToken)
      onAuthenticated()
    } catch (reason) {
      setError(reason.message)
    } finally {
      setBusy(false)
    }
  }

  return <main className="auth-shell">
    <div className="brand-mark">S</div>
    <p className="eyebrow">SENTINEL / OPERATIONS</p>
    <h1>Know before your users do.</h1>
    <p className="muted auth-copy">A focused command center for uptime, latency, and incident recovery.</p>
    <form className="panel auth-card" onSubmit={submit}>
      <div className="segmented"><button type="button" className={mode === 'login' ? 'active' : ''} onClick={() => setMode('login')}>Sign in</button><button type="button" className={mode === 'register' ? 'active' : ''} onClick={() => setMode('register')}>Create account</button></div>
      <label>Email<input type="email" value={email} onChange={event => setEmail(event.target.value)} required /></label>
      <label>Password<input type="password" value={password} onChange={event => setPassword(event.target.value)} minLength={12} required /></label>
      {error && <p className="error">{error}</p>}
      <button className="primary wide" disabled={busy}>{busy ? 'Working…' : mode === 'login' ? 'Open command center' : 'Create workspace'}</button>
    </form>
  </main>
}

function Dashboard({ onSignOut }) {
  const [monitors, setMonitors] = React.useState([])
  const [selected, setSelected] = React.useState(null)
  const [details, setDetails] = React.useState({ stats: null, checks: [], incidents: [] })
  const [form, setForm] = React.useState({ name: '', url: '', intervalSeconds: 60, timeoutMilliseconds: 5000 })
  const [editing, setEditing] = React.useState(false)
  const [error, setError] = React.useState('')
  const [loading, setLoading] = React.useState(true)

  async function loadMonitors() {
    setLoading(true)
    try { setMonitors(await request('/api/monitors')); setError('') } catch (reason) { setError(reason.message) } finally { setLoading(false) }
  }
  async function loadDetails(monitor) {
    setSelected(monitor)
    const [stats, checks, incidents] = await Promise.all([
      request(`/api/monitors/${monitor.id}/stats`),
      request(`/api/monitors/${monitor.id}/checks?limit=8`),
      request(`/api/monitors/${monitor.id}/incidents?limit=8`),
    ])
    setDetails({ stats, checks, incidents })
  }
  React.useEffect(() => { loadMonitors() }, [])
  React.useEffect(() => { if (selected) loadDetails(selected) }, [])

  function beginCreate() { setEditing(false); setSelected(null); setForm({ name: '', url: '', intervalSeconds: 60, timeoutMilliseconds: 5000 }) }
  function beginEdit(monitor) { setEditing(true); setSelected(monitor); setForm({ name: monitor.name, url: monitor.url, intervalSeconds: monitor.intervalSeconds, timeoutMilliseconds: monitor.timeoutMilliseconds }) }
  async function save(event) {
    event.preventDefault(); setError('')
    try {
      const payload = { ...form, intervalSeconds: Number(form.intervalSeconds), timeoutMilliseconds: Number(form.timeoutMilliseconds), ...(editing ? { isEnabled: true } : {}) }
      const saved = editing ? await request(`/api/monitors/${selected.id}`, { method: 'PUT', body: JSON.stringify(payload) }) : await request('/api/monitors', { method: 'POST', body: JSON.stringify(payload) })
      await loadMonitors(); await loadDetails(saved); setEditing(true)
    } catch (reason) { setError(reason.message) }
  }
  async function remove(monitor) {
    if (!window.confirm(`Delete ${monitor.name}?`)) return
    try { await request(`/api/monitors/${monitor.id}`, { method: 'DELETE' }); beginCreate(); await loadMonitors() } catch (reason) { setError(reason.message) }
  }

  const activeIncident = details.incidents.find(incident => incident.status === 'Open')
  return <div className="app-shell">
    <header className="topbar"><div className="wordmark"><span className="brand-mark small">S</span><span>sentinel</span></div><div className="top-actions"><span className="status-dot" /> Monitoring online <button className="ghost" onClick={onSignOut}>Sign out</button></div></header>
    <main className="dashboard">
      <section className="hero"><div><p className="eyebrow">OVERVIEW / LAST 7 DAYS</p><h1>Good morning, operator.</h1><p className="muted">A calm view of everything your users depend on.</p></div><button className="primary" onClick={beginCreate}>+ Add monitor</button></section>
      {error && <div className="alert error">{error}</div>}
      <div className="layout">
        <section className="panel monitor-panel"><div className="panel-heading"><div><p className="eyebrow">MONITORS</p><h2>{monitors.length} endpoints</h2></div><span className="muted">60s cadence</span></div>
          {loading ? <p className="muted empty">Loading monitors…</p> : monitors.length === 0 ? <div className="empty"><p>No monitors yet.</p><button className="secondary" onClick={beginCreate}>Create your first monitor</button></div> : <div className="monitor-list">{monitors.map(monitor => <button className={`monitor-row ${selected?.id === monitor.id ? 'selected' : ''}`} key={monitor.id} onClick={() => loadDetails(monitor)}><span className="status-dot" /><span className="monitor-info"><strong>{monitor.name}</strong><small>{monitor.url}</small></span><span className="row-arrow">›</span></button>)}</div>}
        </section>
        <section className="panel form-panel"><div className="panel-heading"><div><p className="eyebrow">{editing ? 'CONFIGURATION' : 'NEW MONITOR'}</p><h2>{editing ? 'Edit endpoint' : 'Add endpoint'}</h2></div></div><form onSubmit={save}><label>Name<input value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} placeholder="Public API" required /></label><label>URL<input value={form.url} onChange={event => setForm({ ...form, url: event.target.value })} placeholder="https://api.example.com/health" type="url" required /></label><div className="form-grid"><label>Interval (seconds)<input value={form.intervalSeconds} onChange={event => setForm({ ...form, intervalSeconds: event.target.value })} type="number" min="30" required /></label><label>Timeout (ms)<input value={form.timeoutMilliseconds} onChange={event => setForm({ ...form, timeoutMilliseconds: event.target.value })} type="number" min="100" required /></label></div><button className="primary wide">{editing ? 'Save changes' : 'Start monitoring'}</button>{editing && <div className="form-actions"><button type="button" className="ghost" onClick={beginCreate}>New monitor</button><button type="button" className="danger" onClick={() => remove(selected)}>Delete</button></div>}</form></section>
      </div>
      {selected && <section className="detail-grid"><div className="panel stats-panel"><div className="panel-heading"><div><p className="eyebrow">SIGNAL</p><h2>{selected.name}</h2></div><span className={`badge ${activeIncident ? 'bad' : 'good'}`}>{activeIncident ? 'Incident open' : 'Operational'}</span></div><div className="metric-grid"><div><span>Uptime</span><strong>{details.stats ? `${details.stats.uptimePercentage.toFixed(2)}%` : '—'}</strong></div><div><span>Avg latency</span><strong>{details.stats?.averageLatencyMs ? `${Math.round(details.stats.averageLatencyMs)}ms` : '—'}</strong></div><div><span>Checks</span><strong>{details.stats?.totalChecks ?? '—'}</strong></div></div><div className="section-title">Recent checks</div><div className="check-list">{details.checks.length ? details.checks.map((check, index) => <div className="check-row" key={`${check.checkedAt}-${index}`}><span className={`status-dot ${check.isHealthy ? '' : 'down'}`} /><span>{new Date(check.checkedAt).toLocaleString()}</span><span>{check.statusCode || check.errorType}</span><strong>{check.latencyMs ? `${check.latencyMs}ms` : '—'}</strong></div>) : <p className="muted">No checks recorded in this window.</p>}</div></div><div className="panel incident-panel"><div className="panel-heading"><div><p className="eyebrow">INCIDENT TIMELINE</p><h2>Recovery history</h2></div></div>{details.incidents.length ? <div className="timeline">{details.incidents.map(incident => <div className="timeline-item" key={incident.id}><span className={`timeline-marker ${incident.status === 'Open' ? 'open' : ''}`} /><div><strong>{incident.status === 'Open' ? 'Outage detected' : 'Incident recovered'}</strong><p className="muted">{new Date(incident.openedAt).toLocaleString()}</p>{incident.recoveredAt && <small>Recovered {new Date(incident.recoveredAt).toLocaleString()}</small>}</div></div>)}</div> : <div className="empty"><span className="large-check">✓</span><p>No incidents in the last 31 days.</p></div>}</div></section>}
    </main>
  </div>
}

function App() {
  const [authenticated, setAuthenticated] = React.useState(Boolean(localStorage.getItem('sentinel_token')))
  function signOut() { localStorage.removeItem('sentinel_token'); setAuthenticated(false) }
  return authenticated ? <Dashboard onSignOut={signOut} /> : <Auth onAuthenticated={() => setAuthenticated(true)} />
}

createRoot(document.getElementById('root')).render(<React.StrictMode><App /></React.StrictMode>)
