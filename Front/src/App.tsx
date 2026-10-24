import { useEffect, useRef, useState } from 'react';
import axios from 'axios';
import { api, errorMessage, list } from './api';
import type { Category, Product, Resource } from './api';

type RecordItem = Category | Product;
const money = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
import Modal from './components/Modal';

import RecordForm from './components/RecordForm';
import Login from './components/Login';
export default function App() {
  const [signedIn, setSignedIn] = useState(false);

  const [resource, setResource] = useState<Resource>('Products');
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [query, setQuery] = useState('');
  const [editor, setEditor] = useState<{ initial: RecordItem | null } | null>(null);
  const [deleting, setDeleting] = useState<RecordItem | null>(null);
  const [busy, setBusy] = useState(false);
  const [formError, setFormError] = useState('');
  const [revision, setRevision] = useState(0);
  const mutation = useRef(false);
  useEffect(() => {
    if (!signedIn) return;
    const controller = new AbortController();
    setLoading(true); setError('');
    Promise.all([list<Product>('Products', controller.signal), list<Category>('Categories', controller.signal)])
      .then(([p, c]) => { setProducts(p); setCategories(c); })
      .catch(e => { if (!axios.isCancel(e)) setError(errorMessage(e)); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [signedIn, revision]);
  useEffect(() => { if (success) { const timer = setTimeout(() => setSuccess(''), 5000); return () => clearTimeout(timer); } }, [success]);

  if (!signedIn) return <Login enter={() => setSignedIn(true)} />;
  const isProduct = resource === 'Products';
  const records = (isProduct ? products : categories).filter(r => r.name.toLowerCase().includes(query.toLowerCase()));
  const switchPage = (value: Resource) => { setResource(value); setQuery(''); setSuccess(''); };
  const openEditor = (initial: RecordItem | null) => { setFormError(''); setEditor({ initial }); };
  const save = async (value: RecordItem) => {
    if (mutation.current) return;
    mutation.current = true; setBusy(true); setFormError('');
    try {
      if (value.name.length < 3 || ('description' in value && value.description.length < 10)) { setFormError('Please check the minimum field lengths after removing surrounding spaces.'); return; }
      if (editor?.initial) await api.put(`/${resource}/${value.id}`, value); else await api.post(`/${resource}`, value);
      setEditor(null); setSuccess(`${isProduct ? 'Product' : 'Category'} saved successfully.`); setRevision(r => r + 1);
    } catch (e) { setFormError(errorMessage(e)); } finally { mutation.current = false; setBusy(false); }
  };
  const remove = async () => {
    if (!deleting || mutation.current) return;
    mutation.current = true; setBusy(true); setFormError('');
    try { await api.delete(`/${resource}/${deleting.id}`); setDeleting(null); setSuccess('Record deleted successfully.'); setRevision(r => r + 1); }
    catch (e) { setFormError(errorMessage(e)); } finally { mutation.current = false; setBusy(false); }
  };
  return <div className="workspace"><aside className="sidebar"><a href="#" className="brand" onClick={e => {e.preventDefault(); switchPage('Products');}}><span className="brand-mark">C</span>CleanArq<span className="brand-dot">.</span></a><p className="nav-caption">WORKSPACE</p><nav><button className={isProduct ? 'active' : ''} onClick={() => switchPage('Products')}><span>▦</span> Products <small>{products.length}</small></button><button className={!isProduct ? 'active' : ''} onClick={() => switchPage('Categories')}><span>▤</span> Categories <small>{categories.length}</small></button></nav><div className="sidebar-bottom"><div className="demo-badge"><i /> Development mode</div><p>Authentication is simulated.</p><button className="sign-out" onClick={() => { setSignedIn(false); setProducts([]); setCategories([]); setSuccess(''); }}>↗ &nbsp; Sign out</button></div></aside><main className="main-content"><header className="topbar"><span>Workspace <span className="muted">/</span> <strong>{resource}</strong></span><span className="avatar" title="Demo session">D</span></header><div className="page-content"><p className="eyebrow">CATALOG MANAGEMENT</p><div className="page-heading"><div><h1>{resource}<span className="brand-dot">.</span></h1><p className="muted">{isProduct ? 'Everything in your catalog, all in one place.' : 'Give every product a place to belong.'}</p></div><button className="primary" disabled={loading || !!error || (isProduct && categories.length === 0)} onClick={() => openEditor(null)}>+ &nbsp; Add {isProduct ? 'product' : 'category'}</button></div><section className="stats"><div><span>{isProduct ? 'TOTAL PRODUCTS' : 'TOTAL CATEGORIES'}</span><strong>{loading || error ? '—' : isProduct ? products.length : categories.length}</strong><small>In your catalog</small></div><div><span>{isProduct ? 'UNITS IN STOCK' : 'LINKED PRODUCTS'}</span><strong>{loading || error ? '—' : isProduct ? products.reduce((sum,p) => sum+p.stock,0) : products.length}</strong><small>{isProduct ? 'Across all products' : 'Across all categories'}</small></div><div className="stat-note"><span>BUILT FOR CLARITY</span><p>A little structure.<br/>A lot more possibility.</p><small>Keep your catalog in good order ↗</small></div></section>{success && <div role="status" className="success">✓ {success}</div>}{error && <div role="alert" className="error">{error} <button className="text-button" onClick={() => setRevision(r => r+1)}>Try again</button></div>}{!loading && !error && isProduct && categories.length === 0 && <div className="notice">Create a category before adding your first product. <button className="text-button" onClick={() => switchPage('Categories')}>Go to categories →</button></div>}<section className="table-panel"><div className="table-toolbar"><h2>All {resource.toLowerCase()} <span className="count">{records.length}</span></h2><div className="toolbar-actions"><input aria-label={`Search ${resource.toLowerCase()}`} placeholder={`Search ${resource.toLowerCase()}…`} value={query} onChange={e => setQuery(e.target.value)} /><button className="secondary" disabled={loading} onClick={() => setRevision(r => r+1)}>↻ Refresh</button></div></div>{loading ? <div role="status" className="empty">Loading your catalog…</div> : error ? <div className="empty">Your catalog could not be loaded.</div> : records.length === 0 ? <div className="empty"><span className="empty-icon">▦</span><h3>{query ? 'No matches found' : `No ${resource.toLowerCase()} yet`}</h3><p>{query ? 'Try a different search.' : 'Add your first record to get started.'}</p></div> : <div className="table-scroll"><table><thead><tr><th>{isProduct ? 'Product' : 'Category'}</th><th>ID</th>{isProduct ? <><th>Category</th><th>Price</th><th>Stock</th></> : <th>Products</th>}<th className="actions-cell">Actions</th></tr></thead><tbody>{records.map(record => { const p = record as Product; return <tr key={record.id}><td><div className="record-name"><span className="record-icon">{record.name.slice(0,1).toUpperCase()}</span><div><strong>{record.name}</strong>{isProduct && <p className="description">{p.description}</p>}</div></div></td><td className="muted">#{record.id}</td>{isProduct ? <><td><span className="tag">{categories.find(c => c.id === p.categoryId)?.name || p.category?.name || `Category #${p.categoryId}`}</span></td><td>{money.format(p.price)}</td><td><span className={`stock ${p.stock < 10 ? 'low' : ''}`}><i />{p.stock} units</span></td></> : <td>{products.filter(p => p.categoryId === record.id).length}</td>}<td><div className="row-actions"><button onClick={() => openEditor(record)}>Edit</button><button className="danger-link" onClick={() => {setFormError(''); setDeleting(record);}}>Delete</button></div></td></tr>; })}</tbody></table></div>}<footer className="table-footer">{!loading && !error ? `${records.length} record${records.length === 1 ? '' : 's'}` : 'Catalog workspace'}<span>CleanArq / Inventory</span></footer></section><footer className="page-footer">A place for everything. A clearer view of your business.<span>DEVELOPMENT PREVIEW</span></footer></div></main>{editor && <RecordForm resource={resource} initial={editor.initial} categories={categories} busy={busy} error={formError} save={save} close={() => setEditor(null)} />}{deleting && <Modal title={`Delete ${isProduct ? 'product' : 'category'}?`} busy={busy} close={() => setDeleting(null)}><p className="delete-copy">Delete <strong>{deleting.name}</strong>? This action cannot be undone.{!isProduct && ' Categories linked to products may not be removable.'}</p>{formError && <p role="alert" className="error">{formError}</p>}<div className="modal-actions"><button className="secondary" disabled={busy} onClick={() => setDeleting(null)}>Cancel</button><button className="danger" disabled={busy} onClick={() => void remove()}>{busy ? 'Deleting…' : 'Delete record'}</button></div></Modal>}</div>;
}

