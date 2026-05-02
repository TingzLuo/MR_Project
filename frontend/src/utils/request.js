import axios from 'axios'
import { ElMessage } from 'element-plus'
import { getToken, removeToken } from './token'

const request = axios.create({
  baseURL: '/api',
  timeout: 10000
})

request.interceptors.request.use((config) => {
  const token = getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

request.interceptors.response.use(
  (response) => {
    const result = response.data
    const code = result.code ?? result.Code
    const message = result.message ?? result.Message
    const data = result.data ?? result.Data

    if (code !== 200) {
      ElMessage.error(message || '请求失败')
      if (code === 401) {
        removeToken()
        window.location.href = '/login'
      }
      return Promise.reject({ code, message, data })
    }

    return data
  },
  (error) => {
    const message = error.response?.data?.message || error.response?.data?.Message || '网络异常，请稍后再试'
    ElMessage.error(message)
    return Promise.reject(error)
  }
)

export default request
