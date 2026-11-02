import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react'

type TenantStatus = 'Active' | 'Trial' | 'Provisioning' | 'Paused'
type Plan = 'Starter' | 'Growth' | 'Enterprise'
type Tenant = {
  id: string
  name: string
  slug: string
  ownerEmail: string
  plan: Plan
  region: string
  status: TenantStatus
  createdAt: string
  initials: string
  color: string
  seats: number
}

const storageKey = 'northstar-superadmin-tenants'
const initialTenants: Tenant[] = [
  { id: 'tn_8F21', name: 'Aperture Labs', slug: 'aperture-labs', ownerEmail: 'olivia@aperturelabs.com', plan: 'Enterprise', region: 'US East · Virginia', status: 'Active', createdAt: '2025-08-14', initials: 'AL', color: 'violet', seats: 248 },
  { id: 'tn_7C09', name: 'Northwind Traders', slug: 'northwind', ownerEmail: 'sarah@northwind.example', plan: 'Growth', region: 'EU West · Dublin', status: 'Active', createdAt: '2025-09-02', initials: 'NT', color: 'blue', seats: 86 },
  { id: 'tn_6B74', name: 'Monarch Systems', slug: 'monarch-systems', ownerEmail: 'miles@monarch.io', plan: 'Growth', region: 'US East · Virginia', status: 'Trial', createdAt: '2025-09-18', initials: 'MS', color: 'orange', seats: 34 },
  { id: 'tn_5D13', name: 'Goodwell Health', slug: 'goodwell-health', ownerEmail: 'admin@goodwell.health', plan: 'Enterprise', region: 'Asia Pacific · Mumbai', status: 'Active', createdAt: '2025-10-04', initials: 'GH', color: 'green', seats: 172 },
  { id: 'tn_4A88', name: 'Fieldwork Studio', slug: 'fieldwork', ownerEmail: 'hello@fieldwork.studio', plan: 'Starter', region: 'EU West · Dublin', status: 'Provisioning', createdAt: '2025-10-21', initials: 'FS', color: 'pink', seats: 0 },
  { id: 'tn_3E52', name: 'Juniper & Co.', slug: 'juniper-co', ownerEmail: 'it@juniper.co', plan: 'Growth', region: 'US East · Virginia', status: 'Paused', createdAt: '2025-11-03', initials: 'J', color: 'teal', seats: 54 },
  { id: 'tn_2D31', name: 'Papertrail Logistics', slug: 'papertrail', ownerEmail: 'drew@papertrail.io', plan: 'Starter', region: 'Asia Pacific · Mumbai', status: 'Active', createdAt: '2025-11-12', initials: 'PL', color: 'yellow', seats: 19 },
]

const regions = ['US East · Virginia', 'EU West · Dublin', 'Asia Pacific · Mumbai']

function loadTenants(): Tenant[] {
  try {
    const saved = localStorage.getItem(storageKey)
    return saved ? JSON.parse(saved) as Tenant[] : initialTenants
  } catch {
    return initialTenants
  }
}

function initialsFor(name: string) {
  return name.trim().split(/\s+/).slice(0, 2).map((part) => part[0]?.toUpperCase() ?? '').join('')
}

function Icon({ name, size = 18 }: { name: string; size?: number }) {
  const paths: Record<string, ReactNode> = {
    grid: <><rect x="3" y="3" width="7" height="7" rx="1.5" /><rect x="14" y="3" width="7" height="7" rx="1.5" /><rect x="14" y="14" width="7" height="7" rx="1.5" /><rect x="3" y="14" width="7" height="7" rx="1.5" /></>,
    search: <><circle cx="11" cy="11" r="7" /><path d="m20 20-4-4" /></>,
    plus: <><path d="M12 5v14M5 12h14" /></>,
    chevron: <path d="m9 18 6-6-6-6" />,
    down: <path d="m7 10 5 5 5-5" />,
    building: <><rect x="4" y="3" width="16" height="18" rx="2" /><path d="M9 21v-4h6v4M8 7h1m6 0h1M8 11h1m6 0h1" /></>,
    people: <><path d="M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" /><circle cx="10" cy="7" r="4" /><path d="M20 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" /></>,
    check: <path d="m5 12 4 4L19 6" />,
    clock: <><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>,
    more: <><circle cx="5" cy="12" r="1" /><circle cx="12" cy="12" r="1" /><circle cx="19" cy="12" r="1" /></>,
    close: <><path d="m18 6-12 12M6 6l12 12" /></>,
    globe: <><circle cx="12" cy="12" r="9" /><path d="M3 12h18M12 3a15 15 0 0 1 0 18M12 3a15 15 0 0 0 0 18" /></>,
    layers: <><path d="m12 3 9 5-9 5-9-5 9-5Z" /><path d="m3 12 9 5 9-5M3 16l9 5 9-5" /></>,
    shield: <><path d="M12 22s8-4 8-11V5l-8-3-8 3v6c0 7 8 11 8 11Z" /><path d="m9 12 2 2 4-4" /></>,
    arrow: <><path d="M7 17 17 7M7 7h10v10" /></>,
    filter: <><path d="M4 7h16M7 12h10m-7 5h4" /></>,
  }

  return <svg aria-hidden="true" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">{paths[name] ?? paths.grid}</svg>
}

function App() {
  const [tenants, setTenants] = useState<Tenant[]>(loadTenants)
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState('All tenants')
  const [createOpen, setCreateOpen] = useState(false)
  const [selectedTenant, setSelectedTenant] = useState<Tenant | null>(null)
  const [toast, setToast] = useState('')

  useEffect(() => {
    localStorage.setItem(storageKey, JSON.stringify(tenants))
  }, [tenants])

  useEffect(() => {
    if (!toast) return
    const timer = window.setTimeout(() => setToast(''), 3200)
    return () => window.clearTimeout(timer)
  }, [toast])

  useEffect(() => {
    const handleShortcut = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault()
        document.getElementById('tenant-search')?.focus()
      }
      if (event.key === 'Escape') {
        setCreateOpen(false)
        setSelectedTenant(null)
      }
    }
    window.addEventListener('keydown', handleShortcut)
    return () => window.removeEventListener('keydown', handleShortcut)
  }, [])

  const visibleTenants = useMemo(() => tenants.filter((tenant) => {
    const term = search.toLowerCase().trim()
    const matchesSearch = !term || [tenant.name, tenant.slug, tenant.ownerEmail].some((value) => value.toLowerCase().includes(term))
    const matchesFilter = filter === 'All tenants' || tenant.status === filter
    return matchesSearch && matchesFilter
  }), [tenants, search, filter])

  const activeCount = tenants.filter((tenant) => tenant.status === 'Active').length
  const trialCount = tenants.filter((tenant) => tenant.status === 'Trial').length
  const pendingCount = tenants.filter((tenant) => tenant.status === 'Provisioning').length
  const totalSeats = tenants.reduce((sum, tenant) => sum + tenant.seats, 0)

  function createTenant(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const name = String(form.get('name') ?? '').trim()
    const slug = String(form.get('slug') ?? '').trim().toLowerCase()
    const ownerEmail = String(form.get('ownerEmail') ?? '').trim().toLowerCase()
    const plan = String(form.get('plan') ?? 'Starter') as Plan
    const region = String(form.get('region') ?? regions[0])

    if (tenants.some((tenant) => tenant.slug === slug)) {
      setToast('That tenant URL is already in use.')
      return
    }

    const tenant: Tenant = {
      id: `tn_${Math.random().toString(36).slice(2, 7).toUpperCase()}`,
      name,
      slug,
      ownerEmail,
      plan,
      region,
      status: 'Provisioning',
      createdAt: new Date().toISOString().slice(0, 10),
      initials: initialsFor(name),
      color: ['violet', 'blue', 'orange', 'green', 'pink', 'teal'][tenants.length % 6],
      seats: 0,
    }
    setTenants((current) => [tenant, ...current])
    setCreateOpen(false)
    setToast(`${name} added to the provisioning queue.`)
  }

  function toggleTenantStatus(tenant: Tenant) {
    const status: TenantStatus = tenant.status === 'Paused' ? 'Active' : 'Paused'
    setTenants((current) => current.map((item) => item.id === tenant.id ? { ...item, status } : item))
    setSelectedTenant({ ...tenant, status })
    setToast(`${tenant.name} is now ${status.toLowerCase()}.`)
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="#top" aria-label="Northstar platform home">
          <span className="brand-mark"><Icon name="layers" size={20} /></span>
          <span className="brand-copy"><strong>northstar</strong><small>PLATFORM</small></span>
        </a>

        <div className="workspace-switcher">
          <div className="workspace-icon">N</div>
          <div className="workspace-name"><strong>Northstar Inc.</strong><span>Platform workspace</span></div>
          <Icon name="down" size={15} />
        </div>

        <div className="nav-label">PLATFORM</div>
        <nav className="side-nav" aria-label="Platform navigation">
          <button className="nav-item active" type="button"><Icon name="building" /><span>Tenants</span><span className="nav-count">{tenants.length}</span></button>
        </nav>

        <div className="sidebar-bottom">
          <div className="support-card">
            <span className="support-icon"><Icon name="shield" size={17} /></span>
            <div><strong>Superadmin access</strong><span>Platform owner</span></div>
            <span className="green-dot" />
          </div>
          <div className="profile-row">
            <div className="profile-avatar">AS</div>
            <div className="profile-copy"><strong>Alex Morgan</strong><span>Superadmin</span></div>
            <button className="icon-button profile-menu" type="button" aria-label="Account menu"><Icon name="more" /></button>
          </div>
        </div>
      </aside>

      <main className="main-content" id="top">
        <header className="topbar">
          <div className="breadcrumbs"><span>Platform</span><Icon name="chevron" size={14} /><strong>Tenants</strong></div>
          <div className="topbar-right">
            <span className="environment"><i /> Demo environment</span>
            <span className="top-divider" />
            <span className="admin-chip"><span className="admin-avatar">AS</span><span>Alex Morgan</span><Icon name="down" size={14} /></span>
          </div>
        </header>

        <div className="content-wrap">
          <div className="demo-notice"><span className="notice-dot" /><span><strong>Preview mode</strong> — tenant changes are saved in this browser only.</span><span className="notice-spacer" /><span className="notice-detail">Connect the platform API to manage live tenants.</span></div>

          <section className="page-heading">
            <div>
              <div className="eyebrow"><span className="eyebrow-line" /> PLATFORM ADMINISTRATION</div>
              <h1>Tenant management</h1>
              <p>Set up customer workspaces and manage their platform access.</p>
            </div>
            <button className="primary-button" type="button" onClick={() => setCreateOpen(true)}><Icon name="plus" size={17} /> Add tenant</button>
          </section>

          <section className="metrics-grid" aria-label="Tenant overview">
            <Metric icon="building" label="Total tenants" value={tenants.length.toString()} detail="Across all regions" accent="lavender" />
            <Metric icon="check" label="Active tenants" value={activeCount.toString()} detail={`${tenants.length ? Math.round(activeCount / tenants.length * 100) : 0}% of all tenants`} accent="mint" />
            <Metric icon="clock" label="In trial" value={trialCount.toString()} detail="Currently evaluating" accent="peach" />
            <Metric icon="people" label="Provisioning" value={pendingCount.toString()} detail={`${totalSeats.toLocaleString()} active seats`} accent="sky" />
          </section>

          <section className="table-card">
            <div className="table-heading">
              <div><h2>All tenants <span className="table-total">{tenants.length}</span></h2><p>Manage customer workspaces across your platform.</p></div>
              <button className="secondary-button" type="button" onClick={() => { setSearch(''); setFilter('All tenants') }}><Icon name="filter" size={16} /> Reset filters</button>
            </div>
            <div className="table-tools">
              <label className="search-field"><Icon name="search" size={17} /><input id="tenant-search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search tenants..." aria-label="Search tenants" /><kbd>⌘ K</kbd></label>
              <label className="status-filter"><span>Status:</span><select value={filter} onChange={(event) => setFilter(event.target.value)} aria-label="Filter by status"><option>All tenants</option><option>Active</option><option>Trial</option><option>Provisioning</option><option>Paused</option></select><Icon name="down" size={14} /></label>
            </div>

            <div className="table-scroll">
              <table>
                <thead><tr><th className="tenant-column">TENANT</th><th>PLAN</th><th>STATUS</th><th>REGION</th><th>SEATS</th><th>CREATED</th><th><span className="sr-only">Actions</span></th></tr></thead>
                <tbody>
                  {visibleTenants.map((tenant) => <tr key={tenant.id} onClick={() => setSelectedTenant(tenant)} tabIndex={0} onKeyDown={(event) => { if (event.key === 'Enter') setSelectedTenant(tenant) }}>
                    <td><div className="tenant-cell"><div className={`tenant-logo ${tenant.color}`}>{tenant.initials}</div><div className="tenant-identity"><strong>{tenant.name}</strong><span>{tenant.slug}.northstar.app</span></div></div></td>
                    <td><PlanBadge plan={tenant.plan} /></td>
                    <td><StatusBadge status={tenant.status} /></td>
                    <td><span className="region-cell"><Icon name="globe" size={14} />{tenant.region}</span></td>
                    <td><span className="seat-count">{tenant.seats ? tenant.seats.toLocaleString() : '—'}</span></td>
                    <td><span className="date-cell">{new Date(`${tenant.createdAt}T12:00:00`).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}</span></td>
                    <td><button className="row-menu" type="button" aria-label={`Open ${tenant.name}`} onClick={(event) => { event.stopPropagation(); setSelectedTenant(tenant) }}><Icon name="more" /></button></td>
                  </tr>)}
                  {!visibleTenants.length && <tr><td colSpan={7}><div className="empty-state"><span className="empty-icon"><Icon name="search" size={20} /></span><strong>No tenants found</strong><p>Try another search or clear the status filter.</p></div></td></tr>}
                </tbody>
              </table>
            </div>
            <div className="table-footer"><span>Showing <strong>{visibleTenants.length ? 1 : 0}–{visibleTenants.length}</strong> of <strong>{visibleTenants.length}</strong> tenants</span><span className="footer-note"><span className="footer-dot" /> Tenant data is isolated by workspace</span></div>
          </section>

          <footer className="page-footer"><span>Northstar Platform <span>·</span> Superadmin console</span><span>Tenant setup workspace</span></footer>
        </div>
      </main>

      {createOpen && <CreateTenantModal onClose={() => setCreateOpen(false)} onSubmit={createTenant} />}
      {selectedTenant && <TenantDetails tenant={selectedTenant} onClose={() => setSelectedTenant(null)} onToggle={() => toggleTenantStatus(selectedTenant)} />}
      {toast && <div className="toast" role="status"><span className="toast-check"><Icon name="check" size={15} /></span>{toast}</div>}
    </div>
  )
}

function Metric({ icon, label, value, detail, accent }: { icon: string; label: string; value: string; detail: string; accent: string }) {
  return <article className="metric-card"><div className={`metric-icon ${accent}`}><Icon name={icon} size={18} /></div><span className="metric-label">{label}</span><div className="metric-value-row"><strong>{value}</strong><span>{detail}</span></div></article>
}

function PlanBadge({ plan }: { plan: Plan }) {
  return <span className={`plan-badge plan-${plan.toLowerCase()}`}><span />{plan}</span>
}

function StatusBadge({ status }: { status: TenantStatus }) {
  const className = status.toLowerCase()
  return <span className={`status-badge status-${className}`}><i />{status}</span>
}

function CreateTenantModal({ onClose, onSubmit }: { onClose: () => void; onSubmit: (event: FormEvent<HTMLFormElement>) => void }) {
  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [slugEdited, setSlugEdited] = useState(false)

  return <div className="overlay" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}>
    <section className="modal" role="dialog" aria-modal="true" aria-labelledby="create-title">
      <div className="modal-header"><div><span className="modal-kicker">WORKSPACE SETUP</span><h2 id="create-title">Create a tenant</h2><p>Set up an isolated workspace for a new customer.</p></div><button className="icon-button modal-close" type="button" onClick={onClose} aria-label="Close dialog"><Icon name="close" /></button></div>
      <form onSubmit={onSubmit}>
        <div className="form-section-title"><span>01</span> Tenant details</div>
        <label className="form-field">Organization name<input name="name" required minLength={2} maxLength={120} value={name} onChange={(event) => { const nextName = event.target.value; setName(nextName); if (!slugEdited) setSlug(nextName.toLowerCase().trim().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')) }} placeholder="e.g. Acme Corporation" autoFocus /></label>
        <label className="form-field">Tenant URL<div className="slug-input"><span>app.northstar.io/</span><input name="slug" required pattern="[a-z0-9]+(-[a-z0-9]+)*" maxLength={60} value={slug} onChange={(event) => { setSlugEdited(true); setSlug(event.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '')) }} placeholder="acme-corporation" /></div><small>Letters, numbers, and hyphens. This cannot be changed later.</small></label>
        <label className="form-field">Primary administrator email<input name="ownerEmail" type="email" required maxLength={254} placeholder="name@company.com" /><small>An invitation will be sent after the tenant is connected to the platform API.</small></label>
        <div className="form-row">
          <label className="form-field">Subscription plan<select name="plan" defaultValue="Growth"><option>Starter</option><option>Growth</option><option>Enterprise</option></select></label>
          <label className="form-field">Data region<select name="region" defaultValue={regions[0]}>{regions.map((region) => <option key={region}>{region}</option>)}</select></label>
        </div>
        <div className="provisioning-note"><span className="note-icon"><Icon name="shield" size={16} /></span><span><strong>Tenant isolation enabled</strong><small>Workspace data will be separated from other tenants.</small></span></div>
        <div className="modal-actions"><button className="secondary-button" type="button" onClick={onClose}>Cancel</button><button className="primary-button" type="submit"><Icon name="plus" size={16} /> Create tenant</button></div>
      </form>
    </section>
  </div>
}

function TenantDetails({ tenant, onClose, onToggle }: { tenant: Tenant; onClose: () => void; onToggle: () => void }) {
  return <div className="overlay" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}>
    <section className="details-panel" role="dialog" aria-modal="true" aria-labelledby="details-title">
      <div className="details-top"><span className="modal-kicker">TENANT OVERVIEW</span><button className="icon-button" type="button" onClick={onClose} aria-label="Close details"><Icon name="close" /></button></div>
      <div className="details-brand"><div className={`tenant-logo tenant-logo-large ${tenant.color}`}>{tenant.initials}</div><div><h2 id="details-title">{tenant.name}</h2><span>{tenant.slug}.northstar.app</span></div></div>
      <div className="detail-status-row"><StatusBadge status={tenant.status} /><PlanBadge plan={tenant.plan} /><span className="detail-id">{tenant.id}</span></div>
      <div className="detail-divider" />
      <div className="detail-section-label">WORKSPACE DETAILS</div>
      <DetailRow label="Primary admin" value={tenant.ownerEmail} />
      <DetailRow label="Data region" value={tenant.region} />
      <DetailRow label="Active seats" value={tenant.seats.toLocaleString()} />
      <DetailRow label="Created" value={new Date(`${tenant.createdAt}T12:00:00`).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })} />
      <div className="details-callout"><span className="callout-icon"><Icon name="shield" size={17} /></span><div><strong>Tenant data isolation</strong><p>This workspace is assigned its own tenant identifier. All service access must be scoped to this tenant.</p></div></div>
      <div className="details-actions"><button className={`secondary-button ${tenant.status === 'Paused' ? 'resume-button' : 'pause-button'}`} type="button" onClick={onToggle}>{tenant.status === 'Paused' ? 'Resume tenant' : 'Pause tenant'}</button><button className="primary-button" type="button" onClick={onClose}>Done</button></div>
    </section>
  </div>
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return <div className="detail-row"><span>{label}</span><strong>{value}</strong></div>
}

export default App
