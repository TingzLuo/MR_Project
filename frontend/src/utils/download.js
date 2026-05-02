import axios from 'axios'
import { getToken } from './token'

export async function downloadFile(url, fileName = '') {
  const response = await axios({
    url,
    method: 'get',
    baseURL: '/api',
    responseType: 'blob',
    timeout: 120000,
    headers: {
      Authorization: `Bearer ${getToken()}`
    }
  })

  const blob = new Blob([response.data])
  const downloadUrl = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = downloadUrl

  const disposition = response.headers['content-disposition'] || ''
  const matched = disposition.match(/filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/)
  const resolvedFileName = decodeURIComponent(matched?.[1] || matched?.[2] || fileName || 'download.dat')

  link.setAttribute('download', resolvedFileName)
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(downloadUrl)
}
