import request from '../utils/request'
import { downloadFile } from '../utils/download'

export function generateMr(data) { return request({ url: '/mr/generate', method: 'post', data, timeout: 180000 }) }
export function getMrList(params) { return request({ url: '/mr', method: 'get', params, timeout: 30000 }) }
export function getMrDetail(id) { return request({ url: `/mr/${id}`, method: 'get', timeout: 30000 }) }
export function getMrByScgId(scgId) { return request({ url: `/mr/scg/${scgId}`, method: 'get', timeout: 30000 }) }
export function saveMr(id, data) { return request({ url: `/mr/${id}`, method: 'put', data, timeout: 60000 }) }
export function addMrItem(id, data) { return request({ url: `/mr/${id}/items`, method: 'post', data, timeout: 30000 }) }
export function updateMrItem(id, itemId, data) { return request({ url: `/mr/${id}/items/${itemId}`, method: 'put', data, timeout: 30000 }) }
export function deleteMrItem(id, itemId) { return request({ url: `/mr/${id}/items/${itemId}`, method: 'delete', timeout: 30000 }) }
export function getMrHistory(id) { return request({ url: `/mr/${id}/history`, method: 'get', timeout: 30000 }) }
export function getMrHistoryDetail(historyId) { return request({ url: `/mr/history/${historyId}`, method: 'get', timeout: 30000 }) }
export function exportMr(id) { return downloadFile(`/mr/${id}/export`, `mr_${id}.xlsx`) }
