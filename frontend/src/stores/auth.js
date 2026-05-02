import { defineStore } from 'pinia'
import { login, getCurrentUser } from '../api/auth'
import { getToken, setToken, removeToken } from '../utils/token'

function getHomePath(role) {
  return role === 'admin' ? '/admin/dashboard' : '/user/dashboard'
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: getToken(),
    userInfo: null,
    initialized: false
  }),
  getters: {
    isLogin: (state) => !!state.token,
    role: (state) => state.userInfo?.role || '',
    homePath: (state) => getHomePath(state.userInfo?.role || 'user')
  },
  actions: {
    async loginAction(loginForm) {
      const result = await login(loginForm)
      this.token = result.token
      this.userInfo = result.userInfo
      setToken(result.token)
      return result
    },
    async fetchCurrentUser() {
      const userInfo = await getCurrentUser()
      this.userInfo = userInfo
      return userInfo
    },
    async initializeAuth() {
      if (!this.token) {
        this.initialized = true
        return
      }

      try {
        await this.fetchCurrentUser()
      } catch {
        this.logout()
      } finally {
        this.initialized = true
      }
    },
    logout() {
      this.token = ''
      this.userInfo = null
      removeToken()
    },
    getHomePath(role) {
      return getHomePath(role)
    }
  }
})
