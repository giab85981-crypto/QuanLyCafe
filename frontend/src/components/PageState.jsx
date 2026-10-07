import { Inbox, LoaderCircle, CircleAlert } from 'lucide-react'
import './PageState.css'

export default function PageState({ loading, error, title = 'Chưa có dữ liệu phù hợp', description = 'Thử thay đổi từ khóa hoặc bộ lọc để xem thêm kết quả.', onRetry, action }) {
  const Icon = loading ? LoaderCircle : error ? CircleAlert : Inbox
  return <div className={`page-state${loading ? ' is-loading' : ''}${error ? ' is-error' : ''}`} role={error ? 'alert' : 'status'} aria-busy={loading || undefined}>
    <span className="page-state__icon"><Icon size={28} aria-hidden="true" /></span>
    <strong>{loading ? title === 'Chưa có dữ liệu phù hợp' ? 'Đang tải dữ liệu…' : title : error ? 'Chưa tải được dữ liệu' : title}</strong>
    <p>{loading ? 'Vui lòng chờ trong giây lát.' : error || description}</p>
    {!loading && (error && onRetry ? <button type="button" onClick={onRetry}>Thử lại</button> : action)}
  </div>
}
