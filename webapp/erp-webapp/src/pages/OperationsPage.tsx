import { useMemo, useState, type FormEvent } from 'react'
import './OperationsPage.css'

type Order = { id: string; vendor: string; summary: string; created: string; amount: number; status: 'Awaiting review' | 'Approved' | 'Received' }
const starterOrders: Order[] = [
  { id: 'PO-2481', vendor: 'Meridian Office Supply', summary: 'Office equipment · 8 items', created: 'Oct 24, 2026', amount: 4820, status: 'Awaiting review' },
  { id: 'PO-2480', vendor: 'Cloudline Technologies', summary: 'IT hardware · 12 items', created: 'Oct 23, 2026', amount: 12840, status: 'Approved' },
  { id: 'PO-2479', vendor: 'Paper & Pine Co.', summary: 'Office supplies · 24 items', created: 'Oct 22, 2026', amount: 1320, status: 'Received' },
  { id: 'PO-2478', vendor: 'Atlas Industrial', summary: 'Warehouse equipment · 3 items', created: 'Oct 21, 2026', amount: 9360, status: 'Awaiting review' },
  { id: 'PO-2477', vendor: 'Brightpath Energy', summary: 'Facilities · 2 items', created: 'Oct 20, 2026', amount: 5840, status: 'Approved' },
]

function Icon({ name }: { name: string }) {
  const paths: Record<string, string> = {
    box: 'M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z M3.3 7 12 12l8.7-5 M12 22V12 M7.5 4.3l9 5.2',
    home: 'm3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z',
    cart: 'M3 3h2l2.4 12.4a2 2 0 0 0 2 1.6h8.8a2 2 0 0 0 2-1.6L22 8H6 M10 21h.01M18 21h.01',
    file: 'M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6M8 13h8M8 17h8',
    warehouse: 'M3 21V8l9-5 9 5v13 M3 10h18M7 14h3v3H7zM14 14h3v3h-3z',
    users: 'M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2 M10 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8 M20 21v-2a4 4 0 0 0-3-3.87',
    plus: 'M12 5v14M5 12h14',
    search: 'M11 19a8 8 0 1 1 0-16 8 8 0 0 1 0 16 M21 21l-4.3-4.3',
    chevron: 'm9 18 6-6-6-6',
    down: 'm7 10 5 5 5-5',
    trend: 'M3 17l6-6 4 4 8-9 M15 6h6v6',
    close: 'm18 6-12 12M6 6l12 12',
    alert: 'M12 9v4 M12 17h.01 M10.3 3.9 2.4 17.5A2 2 0 0 0 4.1 20h15.8a2 2 0 0 0 1.7-2.5L13.7 3.9a2 2 0 0 0-3.4 0z',
    clock: 'M12 8v4l3 2 M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0',
    check: 'm5 12 4 4L19 6',
    more: 'M5 12h.01M12 12h.01M19 12h.01',
  }
  return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name] ?? paths.box} /></svg>
}

export default function OperationsPage() {
  const [orders, setOrders] = useState(starterOrders)
  const [filter, setFilter] = useState('All orders')
  const [search, setSearch] = useState('')
  const [modalOpen, setModalOpen] = useState(false)
  const [toast, setToast] = useState('')
  const awaiting = orders.filter((order) => order.status === 'Awaiting review').length
  const shown = useMemo(() => orders.filter((order) => {
    const term = search.trim().toLowerCase()
    return (filter === 'All orders' || order.status === filter) && (!term || `${order.id} ${order.vendor} ${order.summary}`.toLowerCase().includes(term))
  }), [orders, filter, search])

  function addOrder(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const vendor = String(data.get('vendor'))
    const amount = Number(data.get('amount'))
    const itemCount = Number(data.get('itemCount'))
    const order: Order = { id: `PO-${2482 + orders.length - starterOrders.length}`, vendor, summary: `Purchase request · ${itemCount} items`, created: new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }), amount, status: 'Awaiting review' }
    setOrders((current) => [order, ...current])
    setModalOpen(false)
    setToast(`${order.id} created and added to review.`)
    window.setTimeout(() => setToast(''), 3000)
  }

  return <div className="erp-shell">
    <aside className="erp-sidebar">
      <a className="erp-brand" href="#operations"><span className="erp-brand-icon"><Icon name="box" /></span><span>northstar<small>ERP PLATFORM</small></span></a>
      <div className="erp-company"><span className="company-initial">N</span><span><b>Northstar Inc.</b><small>Workspace</small></span><Icon name="down" /></div>
      <span className="erp-nav-label">OPERATIONS</span>
      <nav className="erp-nav" aria-label="ERP navigation"><a className="current" href="#operations"><Icon name="home" />Overview</a><a href="#purchase-orders"><Icon name="cart" />Purchase orders<span>{orders.length}</span></a><a href="#inventory"><Icon name="warehouse" />Inventory<span className="alert-count">6</span></a><a href="#vendors"><Icon name="users" />Vendors</a><a href="#documents"><Icon name="file" />Documents</a></nav>
      <div className="erp-sidebar-foot"><div className="erp-health"><span className="health-light"><Icon name="check" /></span><span><b>All systems normal</b><small>Last synced just now</small></span></div><div className="erp-user"><span>AM</span><b>Alex Morgan</b><small>Admin</small><Icon name="more" /></div></div>
    </aside>

    <main className="erp-main" id="operations">
      <header className="erp-topbar"><div className="erp-crumb">Workspace <Icon name="chevron" /> <b>Operations</b></div><div className="erp-top-right"><span className="erp-demo"><i /> Demo workspace</span><span className="erp-top-avatar">AM</span></div></header>
      <div className="erp-content">
        <div className="erp-title-row"><div><span className="erp-eyebrow">OPERATIONS / OVERVIEW</span><h1>Operations overview</h1><p>Track purchasing, inventory, and approvals across your business.</p></div><button className="erp-create" type="button" onClick={() => setModalOpen(true)}><Icon name="plus" />New purchase order</button></div>
        <section className="erp-stats"><Stat icon="cart" label="Open purchase orders" value={String(orders.length)} trend="2 need your review" tone="leaf" /><Stat icon="clock" label="Awaiting approval" value={String(awaiting)} trend="Requires attention" tone="gold" /><Stat icon="box" label="Received this month" value="47" trend="+12% from last month" tone="blue" /><Stat icon="alert" label="Low stock items" value="6" trend="3 critical · 3 watch" tone="rose" /></section>
        <section className="erp-highlight-grid"><article className="erp-budget-card"><div className="budget-head"><div><span>MONTHLY PROCUREMENT</span><strong>$84,620</strong><small>of $100,000 budget</small></div><span className="budget-trend"><Icon name="trend" /> 8.4%</span></div><div className="budget-bar"><span /></div><div className="budget-foot"><span>$15,380 remaining</span><span>October 2026</span></div></article><article className="erp-stock-card"><span className="stock-icon"><Icon name="alert" /></span><div><span className="stock-label">INVENTORY ALERT</span><h3>Items need restocking</h3><p>3 items are below their reorder threshold.</p></div><button type="button" aria-label="View inventory alerts"><Icon name="chevron" /></button></article></section>
        <section className="erp-orders-card" id="purchase-orders"><div className="orders-heading"><div><h2>Purchase orders</h2><p>Review recent requests and follow delivery status.</p></div><button className="orders-link" type="button">View all <Icon name="chevron" /></button></div><div className="orders-tools"><label className="erp-search"><Icon name="search" /><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search orders or vendors" aria-label="Search orders or vendors" /></label><div className="erp-filters">{['All orders', 'Awaiting review', 'Approved', 'Received'].map((item) => <button key={item} className={filter === item ? 'active' : ''} type="button" onClick={() => setFilter(item)}>{item}{item === 'Awaiting review' && <span>{awaiting}</span>}</button>)}</div></div><div className="erp-table-scroll"><table className="erp-orders-table"><thead><tr><th>ORDER</th><th>VENDOR</th><th>CREATED</th><th>AMOUNT</th><th>STATUS</th><th /></tr></thead><tbody>{shown.map((order) => <tr key={order.id}><td><b className="order-id">{order.id}</b><small>{order.summary}</small></td><td>{order.vendor}</td><td>{order.created}</td><td className="order-amount">${order.amount.toLocaleString('en-US', { minimumFractionDigits: 2 })}</td><td><Status status={order.status} /></td><td><button className="erp-row-action" type="button" aria-label={`Options for ${order.id}`}><Icon name="more" /></button></td></tr>)}{!shown.length && <tr><td colSpan={6} className="erp-no-results">No purchase orders match this filter.</td></tr>}</tbody></table></div><div className="erp-table-footer">Showing {shown.length} of {orders.length} purchase orders <span>Updated moments ago</span></div></section>
        <footer className="erp-footer"><span>Northstar ERP · Operations</span><span>Sample workspace data</span></footer>
      </div>
    </main>

    {modalOpen && <div className="erp-overlay" onMouseDown={(event) => { if (event.target === event.currentTarget) setModalOpen(false) }}><section className="erp-modal" role="dialog" aria-modal="true" aria-labelledby="po-modal-title"><div className="erp-modal-head"><div><span>PROCUREMENT REQUEST</span><h2 id="po-modal-title">New purchase order</h2><p>Enter the request details to send it for review.</p></div><button type="button" aria-label="Close" onClick={() => setModalOpen(false)}><Icon name="close" /></button></div><form onSubmit={addOrder}><label>Vendor name<input name="vendor" required maxLength={100} placeholder="Choose or enter a vendor" /></label><div className="erp-form-row"><label>Estimated amount<input name="amount" type="number" min="1" step="0.01" required placeholder="0.00" /></label><label>Number of items<input name="itemCount" type="number" min="1" max="999" defaultValue="1" required /></label></div><div className="erp-modal-note"><Icon name="clock" />This request will be added to the approval queue.</div><div className="erp-modal-actions"><button className="erp-cancel" type="button" onClick={() => setModalOpen(false)}>Cancel</button><button className="erp-create" type="submit">Submit for review</button></div></form></section></div>}
    {toast && <div className="erp-toast" role="status"><Icon name="check" />{toast}</div>}
  </div>
}

function Stat({ icon, label, value, trend, tone }: { icon: string; label: string; value: string; trend: string; tone: string }) {
  return <article className="erp-stat"><span className={`stat-icon ${tone}`}><Icon name={icon} /></span><span className="stat-label">{label}</span><strong>{value}</strong><small>{trend}</small></article>
}

function Status({ status }: { status: Order['status'] }) {
  return <span className={`po-status ${status === 'Awaiting review' ? 'review' : status.toLowerCase()}`}><i />{status}</span>
}
