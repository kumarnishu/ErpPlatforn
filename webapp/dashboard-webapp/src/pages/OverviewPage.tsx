import { useState } from 'react'
import './OverviewPage.css'

const tenants = [
  { name: 'Aperture Labs', slug: 'aperture-labs', plan: 'Enterprise', members: 248, status: 'Active', initials: 'AL', tone: 'violet' },
  { name: 'Northwind Traders', slug: 'northwind', plan: 'Growth', members: 86, status: 'Active', initials: 'NT', tone: 'blue' },
  { name: 'Monarch Systems', slug: 'monarch-systems', plan: 'Growth', members: 34, status: 'Trial', initials: 'MS', tone: 'orange' },
  { name: 'Goodwell Health', slug: 'goodwell-health', plan: 'Enterprise', members: 172, status: 'Active', initials: 'GH', tone: 'green' },
]
const feed = [
  ['EM', 'Emma Miller', 'created a workspace', 'Northwind Europe', '12 min ago', 'purple'],
  ['JD', 'James Davis', 'completed checklist', 'Month-end close', '48 min ago', 'blue'],
  ['SK', 'Sofia Kim', 'invited 4 teammates to', 'Aperture Labs', '2 hours ago', 'peach'],
]
const valuesByRange: Record<string, number[]> = {
  '7 days': [31, 42, 38, 53, 48, 65, 72, 63, 78, 72, 90, 84],
  '30 days': [25, 34, 31, 46, 40, 57, 49, 66, 59, 78, 70, 92],
  '90 days': [18, 28, 24, 39, 33, 50, 45, 62, 55, 73, 66, 88],
}

function Icon({ name }: { name: string }) {
  const paths: Record<string, string> = {
    grid: 'M3 3h7v7H3zM14 3h7v7h-7zM14 14h7v7h-7zM3 14h7v7H3z',
    overview: 'M3 3h8v8H3zM13 3h8v5h-8zM13 10h8v11h-8zM3 13h8v8H3z',
    users: 'M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M10 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8M20 21v-2a4 4 0 0 0-3-3.87',
    tasks: 'M9 6h11M9 12h11M9 18h11M4 6h.01M4 12h.01M4 18h.01',
    chart: 'M3 3v18h18M8 14l4-4 4 3 5-7',
    settings: 'M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06-1.7 2.94-.09-.03a1.65 1.65 0 0 0-1.75.67l-.05.08h-3.4l-.05-.08a1.65 1.65 0 0 0-1.75-.67l-.09.03-1.7-2.94.06-.06A1.65 1.65 0 0 0 9.6 15l-.1-.04v-3.4l.1-.04a1.65 1.65 0 0 0-.33-1.82l-.06-.06 1.7-2.94.09.03a1.65 1.65 0 0 0 1.75-.67l.05-.08h3.4l.05.08a1.65 1.65 0 0 0 1.75.67l.09-.03 1.7 2.94-.06.06A1.65 1.65 0 0 0 19.6 11.5l.1.04v3.4z',
    plus: 'M12 5v14M5 12h14',
    arrow: 'M7 17 17 7M7 7h10v10',
    clock: 'M12 8v4l3 2M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0',
    dollar: 'M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6',
    bell: 'M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4',
    building: 'M4 21V5l8-3 8 3v16M8 9h.01M12 9h.01M16 9h.01M8 13h.01M12 13h.01M16 13h.01M10 21v-4h4v4',
  }
  return <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round"><path d={paths[name] ?? paths.grid} /></svg>
}

function Metric({ icon, label, value, change, note, color }: { icon: string; label: string; value: string; change: string; note: string; color: string }) {
  return <article className="dash-metric"><span className={`metric-symbol ${color}`}><Icon name={icon} /></span><span className="metric-title">{label}</span><strong className="metric-number">{value}</strong><div className="metric-change"><b>{change}</b><span>{note}</span></div></article>
}

function TrendChart({ range }: { range: string }) {
  const values = valuesByRange[range]
  const points = values.map((value, index) => `${32 + index * 57},${175 - value * 1.35}`).join(' ')
  return <svg className="trend-chart" viewBox="0 0 690 210" role="img" aria-label={`Workspace activity trend for ${range}`}>
    {[30, 70, 110, 150, 190].map((y) => <line key={y} x1="32" x2="660" y1={y} y2={y} className="chart-grid" />)}
    <text x="0" y="34">100</text><text x="7" y="74">75</text><text x="7" y="114">50</text><text x="7" y="154">25</text><text x="12" y="194">0</text>
    <polygon points={`32,175 ${points} 659,175`} className="chart-area" /><polyline points={points} className="chart-line" />
    {values.map((value, index) => <circle key={index} cx={32 + index * 57} cy={175 - value * 1.35} r="3.3" className="chart-dot" />)}
    {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map((day, index) => <text key={day} x={28 + index * 95} y="207" className="chart-day">{day}</text>)}
  </svg>
}

export default function OverviewPage() {
  const [range, setRange] = useState('30 days')
  const [period, setPeriod] = useState('This month')
  const today = new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' }).toUpperCase()
  return <div className="dash-shell">
    <aside className="dash-sidebar"><a className="dash-brand" href="#overview"><span className="dash-logo"><Icon name="grid" /></span><span>Northstar<small>WORKSPACE</small></span></a><div className="dash-tenant"><span className="tenant-monogram">N</span><span><b>Northstar Inc.</b><small>Growth plan</small></span><span className="tiny-chevron">⌄</span></div><span className="dash-nav-label">WORKSPACE</span><nav className="dash-nav" aria-label="Workspace navigation"><a className="selected" href="#overview"><Icon name="overview" />Overview</a><a href="#tenants"><Icon name="users" />Tenants<span className="nav-pill">24</span></a><a href="#activity"><Icon name="tasks" />Checklists<span className="nav-pill">8</span></a><a href="#reports"><Icon name="chart" />Reports</a></nav><div className="dash-sidebar-bottom"><a href="#settings"><Icon name="settings" />Settings</a><div className="dash-user"><span className="user-avatar">AM</span><span><b>Alex Morgan</b><small>Workspace owner</small></span><span className="more-dots">•••</span></div></div></aside>
    <main className="dash-main" id="overview"><header className="dash-topbar"><div className="dash-breadcrumb">Workspace <span>/</span> <b>Overview</b></div><div className="dash-top-actions"><span className="dash-demo"><i /> Demo workspace</span><label className="period-select"><Icon name="clock" /><select value={period} onChange={(event) => setPeriod(event.target.value)}><option>This month</option><option>Last month</option><option>This quarter</option></select></label><button className="dash-icon-button" aria-label="Notifications" type="button"><Icon name="bell" /></button><span className="top-avatar">AM</span></div></header>
      <div className="dash-content"><section className="dash-welcome"><div><span className="dash-kicker">{today}</span><h1>Good morning, Alex <span>✦</span></h1><p>Here’s what’s happening across your workspace today.</p></div><button className="dash-primary" type="button"><Icon name="plus" />Create report</button></section>
        <section className="dash-metrics"><Metric icon="dollar" label="Total revenue" value="$128,430" change="+12.8%" note="vs. last month" color="indigo" /><Metric icon="users" label="Active members" value="2,846" change="+8.2%" note="vs. last month" color="mint" /><Metric icon="building" label="Active tenants" value="24" change="+3" note="this month" color="peach" /><Metric icon="tasks" label="Open checklists" value="37" change="−5" note="since last week" color="blue" /></section>
        <section className="dash-main-grid"><article className="dash-panel activity-panel"><div className="panel-heading"><div><h2>Workspace activity</h2><p>Member activity over time</p></div><select className="chart-select" value={range} onChange={(event) => setRange(event.target.value)} aria-label="Activity chart time range"><option>7 days</option><option>30 days</option><option>90 days</option></select></div><div className="chart-legend"><span /><b>Activity</b><small>+18.6%</small><span>compared to previous period</span></div><TrendChart range={range} /></article>
          <article className="dash-panel activity-feed" id="activity"><div className="panel-heading"><div><h2>Recent activity</h2><p>Latest updates from your team</p></div><button className="text-button" type="button">View all <Icon name="arrow" /></button></div><div className="feed-list">{feed.map(([initials, name, action, target, time, tone]) => <div className="feed-item" key={name}><span className={`feed-avatar ${tone}`}>{initials}</span><div><p><b>{name}</b> {action} <strong>{target}</strong></p><small>{time}</small></div></div>)}</div><button className="feed-footer" type="button">Open activity log <Icon name="arrow" /></button></article></section>
        <section className="dash-panel tenant-panel" id="tenants"><div className="panel-heading"><div><h2>Recently active tenants</h2><p>A quick look at customer workspaces</p></div><button className="text-button" type="button">All tenants <Icon name="arrow" /></button></div><div className="tenant-table-wrap"><table className="tenant-table"><thead><tr><th>WORKSPACE</th><th>PLAN</th><th>MEMBERS</th><th>STATUS</th><th /></tr></thead><tbody>{tenants.map((tenant) => <tr key={tenant.slug}><td><div className="dash-tenant-cell"><span className={`dash-tenant-avatar ${tenant.tone}`}>{tenant.initials}</span><span><b>{tenant.name}</b><small>{tenant.slug}.northstar.app</small></span></div></td><td><span className={`plan-tag ${tenant.plan.toLowerCase()}`}>{tenant.plan}</span></td><td>{tenant.members}</td><td><span className={`tenant-status ${tenant.status.toLowerCase()}`}><i />{tenant.status}</span></td><td><button className="row-arrow" aria-label={`Open ${tenant.name}`} type="button"><Icon name="arrow" /></button></td></tr>)}</tbody></table></div></section>
        <footer className="dash-footer"><span>Northstar workspace · Sample data</span><span>All systems operational <i /></span></footer>
      </div>
    </main>
  </div>
}
