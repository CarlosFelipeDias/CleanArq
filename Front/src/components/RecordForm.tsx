import { useState } from 'react';
import type { FormEvent } from 'react';
import type { Category, Product, Resource } from '../api';
import Modal from './Modal';
type RecordItem = Category | Product;
export default function RecordForm({ resource, initial, categories, busy, error, save, close }: {resource: Resource; initial: RecordItem | null; categories: Category[]; busy: boolean; error: string; save: (value: RecordItem) => Promise<void>; close: () => void}) {
  const product = resource === 'Products';
  const old = initial as Product | null;
  const [name, setName] = useState(initial?.name || '');
  const [description, setDescription] = useState(old?.description || '');
  const [price, setPrice] = useState(String(old?.price ?? ''));
  const [stock, setStock] = useState(String(old?.stock ?? ''));
  const [imageUrl, setImageUrl] = useState(old?.imageUrl || '');
  const [categoryId, setCategoryId] = useState(String(old?.categoryId || ''));
  const submit = (e: FormEvent) => { e.preventDefault(); void save(product ? { id: initial?.id || 0, name: name.trim(), description: description.trim(), price: Number(price), stock: Number(stock), imageUrl: imageUrl.trim(), categoryId: Number(categoryId) } : { id: initial?.id || 0, name: name.trim() }); };
  return <Modal title={`${initial ? 'Edit' : 'New'} ${product ? 'product' : 'category'}`} close={close} busy={busy}><form onSubmit={submit}><fieldset disabled={busy}><label>Name<input autoFocus required minLength={3} maxLength={100} value={name} onChange={e => setName(e.target.value)} pattern=".*\S.*" /></label>{product && <><label>Description<textarea required minLength={10} maxLength={255} value={description} onChange={e => setDescription(e.target.value)} /><small>10–255 characters</small></label><div className="form-grid"><label>Price<input required type="number" min="0.01" step="0.01" value={price} onChange={e => setPrice(e.target.value)} /></label><label>Stock<input required type="number" min="1" max="10000" step="1" value={stock} onChange={e => setStock(e.target.value)} /></label></div><label>Category<select required value={categoryId} onChange={e => setCategoryId(e.target.value)}><option value="">Select a category</option>{categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></label><label>Image URL <span className="muted">(optional)</span><input type="url" maxLength={220} value={imageUrl} onChange={e => setImageUrl(e.target.value)} placeholder="https://example.com/image.jpg" /></label></>}</fieldset>{error && <p role="alert" className="error">{error}</p>}<div className="modal-actions"><button type="button" disabled={busy} onClick={close} className="secondary">Cancel</button><button disabled={busy} className="primary">{busy ? 'Saving…' : 'Save changes'}</button></div></form></Modal>;
}
