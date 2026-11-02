import { useMemo, useState, type FormEvent } from 'react'
import './PeoplePage.css'

type Person = { id: string; name: string; role: string; department: string; email: string; location: string; status: 'Active' | 'On leave'; joined: string; tone: string }
const initialPeople: Person[] = [
  { id: 'NS-1048', name: 'Olivia Chen', role: 'Senior Product Designer', department: 'Design', email: 'olivia.chen@northstar.example', location: 'New York, US', status: 'Active', joined: 'Mar 12, 2022', tone: 'lavender' },
  { id: 'NS-1047', name: 'Marcus Johnson', role: 'Engineering Manager', department: 'Engineering', email: 'marcus.j@northstar.example', location: 'Austin, US', status: 'Active', joined: 'Jun 04, 2021', tone: 'blue' },
  { id: 'NS-1046', name: 'Priya Patel', role: 'People Operations Lead', department: 'People', email: 'priya.patel@northstar.example', location: 'London, UK', status: 'On leave', joined: 'Jan 19, 2023', tone: 'peach' },
  { id: 'NS-1045', name: 'Daniel Kim', role: 'Financial Analyst', department: 'Finance', email: 'daniel.kim@northstar.example', location: 'New York, US', status: 'Active', joined: 'Sep 08, 2022', tone: 'mint' },
  { id: 'NS-1044', name: 'Sofia Ramirez', role: 'Customer Success Manager', department: 'Customer Success', email: 'sofia.r@northstar.example', location: 'Remote · US', status: 'Active', joined: 'Feb 26, 2024', tone: 'rose' },
  { id: 'NS-1043', name: 'Ethan Brooks', role: 'Backend Engineer', department: 'Engineering', email: 'ethan.brooks@northstar.example', location: 'Toronto, CA', status: 'Active', joined: 'May 15, 2023', tone: 'sand' },
]

function Icon({ name }: { name: string }) {
  const paths: Record<string, string> = {
    people: 'M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2 M10 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8 M20 21v-2a4 4 0 0 0-3-3.87 M16 3.13a4 4 0 0 1 0 7.75',
    home: 'm3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z',
    calendar: 'M8 2v4m8-4v4M3 10h18M5 4h14a2 2 0 0 1 2 2v14H3V6a2 2 0 0 1 2-2z',
    wallet: 'M3 6.5A2.5 2.5 0 0 1 5.5 4H21v16H5.5A2.5 2.5 0 0 1 3 17.5z M3 7h18M16 13h2',
    chart: 'M3 3v18h18 M8 14l4-4 4 3 5-7',
    search: 'M11 19a8 8 0 1 1 0-16 8 8 0 0 1 0 16 M21 21l-4.3-4.3',
    plus: 'M12 5v14M5 12h14',
    chevron: 'm9 18 6-6-6-6',
    down: 'm7 10 5 5 5-5',
    more: 'M5 12h.01M12 12h.01M19 12h.01',
    close: 'm18 6-12 12M6 6l12 12',
    filter: 'M4 7h16M7 12h10m-7 5h4',
    clock: 'M12 8v4l3 2 M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0',
    sparkle: 'm12 3 1.9 5.8L20 11l-6.1 2.2L12 19l-1.9-5.8L4 11l6.1-2.2L12 3z',
    check: 'm5 12 4 4L19 6',
  }
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name] ?? paths.people} /></svg>
}

function initials(name: string) { return name.split(' ').map((part) => part[0]).slice(0, 2).join('').toUpperCase() }

export default function PeoplePage() {
  const [people, setPeople] = useState(initialPeople)
  const [search, setSearch] = useState('')
  const [department, setDepartment] = useState('All departments')
  const [modalOpen, setModalOpen] = useState(false)
  const [toast, setToast] = useState('')
  const filteredPeople = useMemo(() => people.filter((person) => {
    const term = search.trim().toLowerCase()
    return (department === 'All departments' || person.department === department) && (!term || `${person.name} ${person.role} ${person.email}`.toLowerCase().includes(term))
  }), [people, search, department])

  function addPerson(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const name = `${String(data.get('firstName')).trim()} ${String(data.get('lastName')).trim()}`
    const email = String(data.get('email')).trim().toLowerCase()
    const department = String(data.get('department'))
    const role = String(data.get('role')).trim()
    const person: Person = { id: `NS-${1049 + people.length - initialPeople.length}`, name, role, department, email, location: 'New York, US', status: 'Active', joined: new Date().toLocaleDateString('en-US', { month: 'short', day: '2-digit', year: 'numeric' }), tone: ['lavender', 'blue', 'peach', 'mint', 'rose'][people.length % 5] }
    setPeople((current) => [person, ...current])
    setModalOpen(false)
    setToast(`${name} was added to the team directory.`)
    window.setTimeout(() => setToast(''), 3000)
  }

  const onLeave = people.filter((person) => person.status === 'On leave').length

  return <div className="hr-shell">
    <aside className="hr-sidebar">
      <a className="hr-brand" href="#people"><span className="hr-mark"><Icon name="people" /></span><span>northstar<small>PEOPLE</small></span></a>
      <div className="hr-workspace"><span className="hr-company-mark">N</span><span><b>Northstar Inc.</b><small>People team</small></span><Icon name="down" /></div>
      <span className="hr-nav-label">PEOPLE</span>
      <nav className="hr-nav" aria-label="People navigation"><a className="active" href="#people"><Icon name="people" />Directory<span>{people.length}</span></a><a href="#time-off"><Icon name="calendar" />Time off<span className="leave-count">{onLeave}</span></a><a href="#compensation"><Icon name="wallet" />Compensation</a><a href="#reports"><Icon name="chart" />Reports</a></nav>
      <div className="hr-side-bottom"><a href="#help"><Icon name="sparkle" />Help center</a><div className="hr-account"><span className="hr-admin-avatar">AM</span><span><b>Alex Morgan</b><small>People admin</small></span><Icon name="more" /></div></div>
    </aside>

    <main className="hr-main" id="people">
      <header className="hr-topbar"><div className="hr-breadcrumb">People <Icon name="chevron" /> <b>Directory</b></div><div className="hr-top-actions"><span className="hr-demo"><i /> Demo workspace</span><span className="hr-top-avatar">AM</span></div></header>
      <div className="hr-content">
        <section className="hr-heading"><div><span className="hr-eyebrow">PEOPLE / DIRECTORY</span><h1>People</h1><p>Keep your team information organized and up to date.</p></div><button className="hr-primary" type="button" onClick={() => setModalOpen(true)}><Icon name="plus" />Add employee</button></section>

        <section className="hr-stats"><Stat label="Total employees" value={String(people.length + 242)} hint="Across all locations" tone="purple" icon="people" /><Stat label="New this month" value="8" hint="2 start next week" tone="blue" icon="sparkle" /><Stat label="On leave today" value={String(onLeave + 5)} hint="3 returning this week" tone="peach" icon="calendar" /><Stat label="Open positions" value="4" hint="2 departments hiring" tone="mint" icon="chart" /></section>

        <section className="hr-lower-grid"><article className="hr-department-card"><div className="hr-panel-head"><div><h2>Team distribution</h2><p>Headcount by department</p></div><button aria-label="More team distribution options" type="button"><Icon name="more" /></button></div><div className="department-list"><Department name="Engineering" count={82} total={248} color="violet" /><Department name="Customer Success" count={54} total={248} color="blue" /><Department name="Design" count={36} total={248} color="rose" /><Department name="Sales & Marketing" count={42} total={248} color="orange" /><Department name="Other teams" count={34} total={248} color="green" /></div><div className="department-foot"><span><i /> 7 departments</span><span>248 people</span></div></article>
          <article className="hr-anniversary"><div className="anniversary-top"><span className="birthday-icon"><Icon name="sparkle" /></span><span className="today-label">THIS WEEK</span></div><h2>Celebrate your people</h2><p>3 work anniversaries coming up this week.</p><div className="anniversary-people"><span className="ann-avatar lavender">JL</span><span className="ann-avatar peach">RB</span><span className="ann-avatar blue">MC</span><span className="ann-count">+0</span></div><div className="anniversary-names"><span>Jordan Lee · 5 years</span><span>Riley Brooks · 2 years</span></div><button type="button" className="anniversary-link">View celebrations <Icon name="chevron" /></button></article></section>

        <section className="hr-directory-card"><div className="hr-directory-head"><div><h2>Team directory <span>{people.length} sample profiles</span></h2><p>Search and manage employee records from this demo workspace.</p></div><button className="hr-filter-button" type="button" onClick={() => { setSearch(''); setDepartment('All departments') }}><Icon name="filter" />Clear filters</button></div><div className="hr-tools"><label className="hr-search"><Icon name="search" /><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search by name, role, or email" aria-label="Search employees" /></label><label className="department-select"><span>Department</span><select value={department} onChange={(event) => setDepartment(event.target.value)}><option>All departments</option>{['Design', 'Engineering', 'People', 'Finance', 'Customer Success'].map((item) => <option key={item}>{item}</option>)}</select><Icon name="down" /></label></div><div className="hr-table-scroll"><table className="hr-table"><thead><tr><th>EMPLOYEE</th><th>DEPARTMENT</th><th>LOCATION</th><th>STATUS</th><th>START DATE</th><th /></tr></thead><tbody>{filteredPeople.map((person) => <tr key={person.id}><td><div className="employee-cell"><span className={`employee-avatar ${person.tone}`}>{initials(person.name)}</span><span><b>{person.name}</b><small>{person.role}</small></span></div></td><td><span className="department-name">{person.department}</span></td><td>{person.location}</td><td><span className={`employee-status ${person.status === 'Active' ? 'active' : 'leave'}`}><i />{person.status}</span></td><td>{person.joined}</td><td><button className="employee-more" type="button" aria-label={`Options for ${person.name}`}><Icon name="more" /></button></td></tr>)}{!filteredPeople.length && <tr><td colSpan={6} className="hr-empty">No employees match those filters.</td></tr>}</tbody></table></div><div className="hr-table-foot">Showing {filteredPeople.length} sample profiles <span>248 total employees · Demo data</span></div></section>
        <footer className="hr-footer"><span>Northstar People · HRMS</span><span>Sample workspace data</span></footer>
      </div>
    </main>

    {modalOpen && <div className="hr-overlay" onMouseDown={(event) => { if (event.target === event.currentTarget) setModalOpen(false) }}><section className="hr-modal" role="dialog" aria-modal="true" aria-labelledby="employee-modal-title"><div className="hr-modal-head"><div><span>TEAM DIRECTORY</span><h2 id="employee-modal-title">Add employee</h2><p>Create an employee profile in this workspace.</p></div><button type="button" aria-label="Close" onClick={() => setModalOpen(false)}><Icon name="close" /></button></div><form onSubmit={addPerson}><div className="hr-form-row"><label>First name<input name="firstName" required maxLength={60} placeholder="First name" /></label><label>Last name<input name="lastName" required maxLength={60} placeholder="Last name" /></label></div><label>Work email<input name="email" type="email" required maxLength={254} placeholder="name@company.com" /></label><label>Job title<input name="role" required maxLength={100} placeholder="e.g. Product Designer" /></label><label>Department<select name="department" defaultValue="Engineering">{['Engineering', 'Design', 'People', 'Finance', 'Customer Success', 'Sales & Marketing'].map((item) => <option key={item}>{item}</option>)}</select></label><div className="hr-modal-actions"><button className="hr-cancel" type="button" onClick={() => setModalOpen(false)}>Cancel</button><button className="hr-primary" type="submit"><Icon name="plus" />Add employee</button></div></form></section></div>}
    {toast && <div className="hr-toast" role="status"><Icon name="check" />{toast}</div>}
  </div>
}

function Stat({ label, value, hint, tone, icon }: { label: string; value: string; hint: string; tone: string; icon: string }) { return <article className="hr-stat"><span className={`hr-stat-icon ${tone}`}><Icon name={icon} /></span><span className="hr-stat-label">{label}</span><strong>{value}</strong><small>{hint}</small></article> }
function Department({ name, count, total, color }: { name: string; count: number; total: number; color: string }) { return <div className="department-row"><div className="department-meta"><span><i className={color} />{name}</span><b>{count}</b></div><div className="department-bar"><span className={color} style={{ width: `${count / total * 100}%` }} /></div></div> }
