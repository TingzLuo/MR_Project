import request from '../utils/request'

export function getUserStats() {
  return request({
    url: '/dashboard/user-stats',
    method: 'get',
    timeout: 30000
  })
}

export function getRecentActivities() {
  return request({
    url: '/dashboard/recent-activities',
    method: 'get',
    timeout: 30000
  })
}

export function getActivityPagedList(params) {
  return request({
    url: '/dashboard/activities',
    method: 'get',
    params,
    timeout: 30000
  })
}
