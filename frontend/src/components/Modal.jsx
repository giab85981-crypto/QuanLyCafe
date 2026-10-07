import { useEffect, useRef } from 'react'
import './Modal.css'

const openDialogs = []
let previousOverflow = ''
const focusable = 'button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled), a[href], [tabindex]:not([tabindex="-1"])'

function Modal({ title, onClose, children, footer, width = 460 }) {
  const dialog = useRef(null)
  const close = useRef(onClose)
  useEffect(() => { close.current = onClose }, [onClose])
  useEffect(() => {
    const node = dialog.current
    const origin = document.activeElement
    if (!openDialogs.length) previousOverflow = document.body.style.overflow
    openDialogs.push(node)
    document.body.style.overflow = 'hidden'
    const controls = () => [...node.querySelectorAll(focusable)].filter(el => el.getClientRects().length && !el.closest('[hidden]') && el.tabIndex >= 0)
    if (!node.contains(document.activeElement)) (controls().find(el => el.closest('.modal__body') && el.matches('input, select, textarea')) || node).focus()
    const onKey = e => {
      if (openDialogs.at(-1) !== node) return
      if (e.key === 'Escape') { e.preventDefault(); e.stopPropagation(); close.current(); return }
      if (e.key !== 'Tab') return
      const items = controls(), first = items[0], last = items.at(-1)
      if (!first) { e.preventDefault(); node.focus(); return }
      if (!node.contains(document.activeElement) || document.activeElement === node || (e.shiftKey ? document.activeElement === first : document.activeElement === last)) {
        e.preventDefault(); (e.shiftKey ? last : first).focus()
      }
    }
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('keydown', onKey)
      const wasTop = openDialogs.at(-1) === node
      openDialogs.splice(openDialogs.indexOf(node), 1)
      if (!openDialogs.length) document.body.style.overflow = previousOverflow
      if (wasTop) {
        const parent = openDialogs.at(-1)
        if (origin?.isConnected && (!parent || parent.contains(origin))) origin.focus()
        else parent?.focus()
      }
    }
  }, [])

  return (
    <div className="modal-backdrop" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
      <div ref={dialog} tabIndex={-1} className="modal" style={{ width }} role="dialog" aria-modal="true" aria-label={title}>
        <div className="modal__head">
          <h3>{title}</h3>
          <button type="button" className="modal__close" onClick={onClose} aria-label="Đóng">✕</button>
        </div>
        <div className="modal__body">{children}</div>
        {footer && <div className="modal__foot">{footer}</div>}
      </div>
    </div>
  )
}

export default Modal
