import request from '../utils/request'

export function uploadDocuments(formData) {
  return request({
    url: '/documents/upload',
    method: 'post',
    data: formData,
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  })
}

export function getDocumentList(params) {
  return request({
    url: '/documents',
    method: 'get',
    params
  })
}

export function getDocumentDetail(id) {
  return request({
    url: `/documents/${id}`,
    method: 'get'
  })
}

export function deleteDocument(id) {
  return request({
    url: `/documents/${id}`,
    method: 'delete'
  })
}
