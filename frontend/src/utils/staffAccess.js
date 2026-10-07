import { useSyncExternalStore } from 'react'
const routes = { '/dashboard': 'DASHBOARD_VIEW', '/pos': 'POS_VIEW', '/kitchen': 'KITCHEN_VIEW', '/menu': 'MENU_VIEW', '/inventory': 'INVENTORY_VIEW', '/tables': 'TABLES_VIEW', '/orders': 'ORDERS_VIEW', '/customers': 'CUSTOMERS_VIEW', '/staff': 'STAFF_VIEW', '/cashbook': 'CASHBOOK_VIEW', '/reports': 'REPORT_VIEW' }
export function currentUser() { try { return JSON.parse(localStorage.getItem('user') || '{}') } catch { return {} } }
export function can(code, user = currentUser()) { return user.roleName === 'Admin' || (user.permissions || []).includes(code) }
export function canOpen(path, user = currentUser()) { return path === '/shifts' ? can('SHIFT_SELF', user) || can('SHIFT_VIEW', user) : path === '/permissions' ? user.roleName === 'Admin' : !!routes[path] && can(routes[path], user) }
export function landing(user = currentUser()) { return [...Object.keys(routes), '/shifts'].find(path => canOpen(path, user)) || '/no-access' }
export function managementLanding(user = currentUser()) { return Object.keys(routes).find(path => !['/pos', '/kitchen'].includes(path) && canOpen(path, user)) }
export function publishUser(user) { const next = JSON.stringify(user); if (localStorage.getItem('user') !== next) { localStorage.setItem('user', next); window.dispatchEvent(new Event('access-changed')) } }
const subscribe = notify => { window.addEventListener('access-changed', notify); window.addEventListener('storage', notify); return () => { window.removeEventListener('access-changed', notify); window.removeEventListener('storage', notify) } }
export function useAccess() { const raw = useSyncExternalStore(subscribe, () => localStorage.getItem('user') || '{}'); try { return JSON.parse(raw) } catch { return {} } }
