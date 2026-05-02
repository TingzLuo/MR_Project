import request from '../utils/request'

export function getProfile() {
  return request({
    url: '/users/profile',
    method: 'get',
    timeout: 30000
  })
}

export function updateProfile(data) {
  return request({
    url: '/users/profile',
    method: 'put',
    data,
    timeout: 30000
  })
}

export function updatePassword(data) {
  return request({
    url: '/users/password',
    method: 'put',
    data,
    timeout: 30000
  })
}

export function getUserOperationRecordCleanupSetting() {
  return request({
    url: '/users/operation-record-cleanup-setting',
    method: 'get',
    timeout: 30000
  })
}

export function updateUserOperationRecordCleanupSetting(data) {
  return request({
    url: '/users/operation-record-cleanup-setting',
    method: 'put',
    data,
    timeout: 30000
  })
}
