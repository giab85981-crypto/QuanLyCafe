import { useEffect, useState } from 'react'
import { Moon, Sun } from 'lucide-react'
import './ThemeToggle.css'
import { initialTheme, themeStorageKey as key } from '../utils/theme'
export default function ThemeToggle() {
  const [theme, setTheme] = useState(initialTheme)
  useEffect(() => {
    document.documentElement.dataset.theme = theme
    try { localStorage.setItem(key, theme) } catch { /* The current page can still switch theme. */ }
  }, [theme])
  useEffect(() => {
    const sync = event => { if (event.key === key) setTheme(initialTheme()) }
    window.addEventListener('storage', sync)
    return () => window.removeEventListener('storage', sync)
  }, [])
  return <button className="theme-toggle" type="button" aria-label={theme === 'dark' ? 'Bật chế độ sáng' : 'Bật chế độ tối'} title={theme === 'dark' ? 'Bật chế độ sáng' : 'Bật chế độ tối'} aria-pressed={theme === 'dark'} onClick={() => setTheme(value => value === 'dark' ? 'light' : 'dark')}>{theme === 'dark' ? <Sun size={18}/> : <Moon size={18}/>}<span>{theme === 'dark' ? 'Chế độ sáng' : 'Chế độ tối'}</span></button>
}
