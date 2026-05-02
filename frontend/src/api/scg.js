import request from '../utils/request'
import { downloadFile } from '../utils/download'

export function confirmScg(id) {
  return request({ url: `/scg/${id}/confirm`, method: 'post', timeout: 30000 })
}

export function generateScg(data) {
  return request({ url: '/scg/generate', method: 'post', data, timeout: 180000 })
}

export function getScgByDocuments(documentIds) {
  return request({ url: '/scg/query', method: 'get', params: { documentIds: documentIds.join(',') }, timeout: 30000 })
}

export function getConfirmedScgList() {
  return request({ url: '/scg/confirmed', method: 'get', timeout: 30000 })
}

export function deleteScg(id) {
  return request({ url: `/scg/${id}`, method: 'delete', timeout: 30000 })
}

export function saveScg(id, data) {
  return request({ url: `/scg/${id}`, method: 'put', data, timeout: 60000 })
}

export function exportScg(id) {
  return downloadFile(`/scg/${id}/export`, `scg_${id}.json`)
}
