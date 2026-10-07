const key = 'cafe.theme'
export function initialTheme() {
  try { const saved = localStorage.getItem(key); if (saved === 'dark' || saved === 'light') return saved } catch { /* Use the device preference when storage is unavailable. */ }
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}
export const themeStorageKey = key
