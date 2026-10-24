import { useEffect, useRef } from 'react';
import type { ReactNode } from 'react';
export default function Modal({ title, children, close, busy }: { title: string; children: ReactNode; close: () => void; busy: boolean }) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => { ref.current?.showModal(); }, []);
  return <dialog ref={ref} onCancel={e => { e.preventDefault(); if (!busy) close(); }} aria-label={title}>
    <div className="modal-head"><h2>{title}</h2><button disabled={busy} onClick={close} aria-label="Close dialog" className="icon-button">×</button></div>{children}
  </dialog>;
}
