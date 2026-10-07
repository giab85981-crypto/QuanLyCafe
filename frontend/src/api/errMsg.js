export function errMsg(error, fallback = 'Không thực hiện được thao tác. Vui lòng thử lại.') {
  const status = error?.response?.status
  if (status === 401) return 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.'
  if (status === 403) return 'Bạn không có quyền thực hiện thao tác này.'
  if (status >= 500) return 'Máy chủ đang gặp lỗi. Vui lòng thử lại sau.'
  if (status === 429) return 'Bạn thao tác quá nhanh. Vui lòng chờ một chút rồi thử lại.'
  if (['ECONNABORTED', 'ETIMEDOUT'].includes(error?.code)) return 'Yêu cầu mất quá nhiều thời gian. Kiểm tra kết nối rồi thử lại.'
  if (!error?.response && (error?.request || error?.code === 'ERR_NETWORK')) return 'Không kết nối được máy chủ. Kiểm tra kết nối và thử lại.'
  const data = error?.response?.data
  const message = typeof data === 'string' ? data : data?.message || Object.values(data?.errors || {}).flat().join(' ')
  if (typeof message === 'string' && message.trim() && !/<(?:!doctype|html|body)|Exception:|\bat .*\bin .*:line /i.test(message)) return message.trim()
  if (!error?.response && !error?.isAxiosError && error?.message) return error.message
  return fallback
}
