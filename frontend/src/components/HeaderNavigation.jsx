import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import { ChevronDown } from 'lucide-react'

export default function HeaderNavigation({ items, open, onToggle, onClose }) {
  const hostRef = useRef(null)
  const measureRef = useRef(null)
  const buttonRef = useRef(null)
  const listRef = useRef(null)
  const focusOnOpen = useRef(false)
  const [count, setCount] = useState(items.length)
  const { pathname } = useLocation()

  useLayoutEffect(() => {
    const host = hostRef.current
    const measure = measureRef.current
    const update = () => {
      const available = Math.max(0, host.clientWidth - 12)
      const widths = Array.from(measure.children, child => child.getBoundingClientRect().width)
      const moreWidth = widths.pop() || 0
      if (widths.reduce((sum, width) => sum + width, 0) <= available) {
        setCount(items.length)
        return
      }
      let used = moreWidth
      let next = 0
      while (next < widths.length && used + widths[next] <= available) used += widths[next++]
      setCount(next)
    }
    update()
    const observer = new ResizeObserver(update)
    observer.observe(host)
    for (const child of measure.children) observer.observe(child)
    return () => observer.disconnect()
  }, [items])

  const shown = items.slice(0, count)
  const hidden = items.slice(count)
  const hasActiveHidden = hidden.some(item => item.path === pathname)
  const showDropdown = open && hidden.length > 0

  useEffect(() => {
    if (open && hidden.length === 0) onClose()
  }, [open, hidden.length, onClose])

  useEffect(() => {
    if (showDropdown && focusOnOpen.current) {
      listRef.current?.querySelector('a')?.focus()
      focusOnOpen.current = false
    }
  }, [showDropdown])

  useEffect(() => {
    const clickOutside = event => {
      if (open && !hostRef.current?.contains(event.target)) onClose()
    }
    const escape = event => {
      if (event.key === 'Escape' && open) {
        onClose()
        buttonRef.current?.focus()
      }
    }
    document.addEventListener('mousedown', clickOutside)
    document.addEventListener('keydown', escape)
    return () => {
      document.removeEventListener('mousedown', clickOutside)
      document.removeEventListener('keydown', escape)
    }
  }, [open, onClose])

  const dropdownKey = event => {
    const links = Array.from(listRef.current.querySelectorAll('a'))
    const index = links.indexOf(document.activeElement)
    const next = event.key === 'ArrowDown' ? (index + 1) % links.length
      : event.key === 'ArrowUp' ? (index - 1 + links.length) % links.length
        : event.key === 'Home' ? 0 : event.key === 'End' ? links.length - 1 : null
    if (next !== null) {
      event.preventDefault()
      links[next]?.focus()
    }
  }

  return <nav className="app-header__nav" aria-label="Điều hướng chính" ref={hostRef}>
    {/* Measure every permitted label without putting duplicate links in the tab order. */}
    <div className="header-nav-measure" aria-hidden="true" ref={measureRef}>
      {items.map(item => <span className="app-header__tab" key={item.path}>{item.label}</span>)}
      <span className="app-header__tab header-more-toggle">Khác <ChevronDown size={15}/></span>
    </div>
    <div className="header-nav-strip">
      {shown.map(item => <NavLink key={item.path} to={item.path} onClick={onClose}
        className={({ isActive }) => `app-header__tab ${isActive ? 'is-active' : ''}`}>
        {item.label}
      </NavLink>)}
      {hidden.length > 0 && <button ref={buttonRef} type="button"
        className={`app-header__tab header-more-toggle ${hasActiveHidden ? 'is-active' : ''} ${showDropdown ? 'is-open' : ''}`}
        aria-expanded={showDropdown} aria-controls="header-more-links"
        onClick={onToggle} onKeyDown={event => {
          if (event.key === 'ArrowDown') {
            event.preventDefault()
            if (showDropdown) listRef.current?.querySelector('a')?.focus()
            else { focusOnOpen.current = true; onToggle() }
          }
        }}>Khác <ChevronDown size={15}/></button>}
    </div>
    {showDropdown && <div id="header-more-links" className="popover header-more-links" ref={listRef} onKeyDown={dropdownKey}>
      {hidden.map(item => <NavLink key={item.path} to={item.path} onClick={onClose}
        className={({ isActive }) => `header-more-link ${isActive ? 'is-active' : ''}`}>
        <span>{item.label}</span>
      </NavLink>)}
    </div>}
  </nav>
}
