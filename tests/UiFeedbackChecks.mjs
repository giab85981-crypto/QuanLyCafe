import test from 'node:test'
import assert from 'node:assert/strict'
import { errMsg } from '../frontend/src/api/errMsg.js'

test('network failure and timeout have actionable Vietnamese messages', () => {
  assert.match(errMsg({ isAxiosError: true, request: {}, message: 'Network Error' }), /Không kết nối được máy chủ/)
  assert.match(errMsg({ code: 'ETIMEDOUT' }), /quá nhiều thời gian/)
})
test('authentication and authorization remain distinct', () => {
  assert.match(errMsg({ response: { status: 401 } }), /đăng nhập lại/)
  assert.match(errMsg({ response: { status: 403 } }), /không có quyền/)
})
test('server failures do not expose database details or HTML', () => {
  assert.match(errMsg({ response: { status: 500, data: 'SqlException: secret server details' } }), /Máy chủ đang gặp lỗi/)
  assert.equal(errMsg({ response: { status: 400, data: '<html>Internal stack trace</html>' } }, 'Thử lại.'), 'Thử lại.')
})
test('stock and stale invoice business errors remain intact', () => {
  const message = 'Không đủ nguyên liệu Sữa. Chỉ còn 2 phần.'
  assert.equal(errMsg({ response: { status: 400, data: message } }), message)
  const changed = 'Hóa đơn vừa thay đổi. Hãy kiểm tra lại món và số tiền trước khi thanh toán.'
  assert.equal(errMsg({ response: { status: 409, data: { code: 'BILL_CHANGED', message: changed } } }), changed)
})
test('validation and local import errors remain readable', () => {
  assert.equal(errMsg({ response: { status: 400, data: { errors: { Name: ['Nhập tên món.'], Price: ['Giá không được âm.'] } } } }), 'Nhập tên món. Giá không được âm.')
  assert.equal(errMsg(new Error('File tối đa 5 MB.')), 'File tối đa 5 MB.')
})
test('missing payload does not leak generic Axios errors', () => {
  assert.equal(errMsg({ isAxiosError: true, response: { status: 404 }, message: 'Request failed with status code 404' }, 'Chưa tìm thấy dữ liệu.'), 'Chưa tìm thấy dữ liệu.')
  assert.match(errMsg(null), /thử lại/)
})
