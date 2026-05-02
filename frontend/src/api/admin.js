import request from '../utils/request'
import { downloadFile } from '../utils/download'

export function getAdminOverview() {
  return request({ url: '/admin/dashboard/overview', method: 'get', timeout: 30000 })
}
export function getAdminUserList(params) {
  return request({ url: '/admin/users', method: 'get', params, timeout: 30000 })
}
export function createAdminUser(data) {
  return request({ url: '/admin/users', method: 'post', data, timeout: 30000 })
}
export function updateAdminUser(id, data) {
  return request({ url: `/admin/users/${id}`, method: 'put', data, timeout: 30000 })
}
export function deleteAdminUser(id) {
  return request({ url: `/admin/users/${id}`, method: 'delete', timeout: 30000 })
}
export function batchDeleteAdminUsers(data) {
  return request({ url: '/admin/users/batch-delete', method: 'post', data, timeout: 30000 })
}
export function getOperationRecordCleanupSetting() {
  return request({ url: '/admin/operation-record-cleanup-setting', method: 'get', timeout: 30000 })
}
export function updateOperationRecordCleanupSetting(data) {
  return request({ url: '/admin/operation-record-cleanup-setting', method: 'put', data, timeout: 30000 })
}
export function exportAdminUsers() { return downloadFile('/admin/export/users', 'users.xlsx') }
export function exportAdminDocuments() { return downloadFile('/admin/export/documents', 'documents.xlsx') }
export function exportAdminScg() { return downloadFile('/admin/export/scg', 'scg_records.json') }
export function exportAdminMr() { return downloadFile('/admin/export/mr', 'mr_records.xlsx') }
export function exportSystemBackup() { return downloadFile('/admin/backup/system', 'system_backup.zip') }
