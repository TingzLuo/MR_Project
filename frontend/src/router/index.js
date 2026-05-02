import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  { path: '/', redirect: '/login' },
  { path: '/login', name: 'login', component: () => import('../views/auth/Login.vue'), meta: { requiresAuth: false } },
  { path: '/register', name: 'register', component: () => import('../views/auth/Register.vue'), meta: { requiresAuth: false } },
  {
    path: '/user',
    component: () => import('../layout/UserLayout.vue'),
    meta: { requiresAuth: true, role: 'user' },
    children: [
      { path: 'dashboard', name: 'userDashboard', component: () => import('../views/user/Dashboard.vue') },
      { path: 'documents', name: 'userDocuments', component: () => import('../views/user/Documents.vue') },
      { path: 'scg', name: 'userScg', component: () => import('../views/user/Scg.vue') },
      { path: 'mr', name: 'userMr', component: () => import('../views/user/Mr.vue') },
      { path: 'activities', name: 'userActivities', component: () => import('../views/user/Activities.vue') },
      { path: 'profile', name: 'userProfile', component: () => import('../views/user/Profile.vue') }
    ]
  },
  {
    path: '/admin',
    component: () => import('../layout/AdminLayout.vue'),
    meta: { requiresAuth: true, role: 'admin' },
    children: [
      { path: 'dashboard', name: 'adminDashboard', component: () => import('../views/admin/Dashboard.vue') },
      { path: 'users', name: 'adminUsers', component: () => import('../views/admin/UserManagement.vue') },
      { path: 'export', name: 'adminExport', component: () => import('../views/admin/BackupExport.vue') }
    ]
  },
  { path: '/:pathMatch(.*)*', redirect: '/login' }
]

const router = createRouter({ history: createWebHistory(), routes })
router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore()
  if (!authStore.initialized) await authStore.initializeAuth()
  if (to.meta.requiresAuth && !authStore.isLogin) return next('/login')
  if ((to.path === '/login' || to.path === '/register') && authStore.isLogin) {
    if (!authStore.userInfo) {
      try { await authStore.fetchCurrentUser() } catch { authStore.logout(); return next('/login') }
    }
    return next(authStore.homePath)
  }
  if (to.meta.requiresAuth) {
    if (!authStore.userInfo) {
      try { await authStore.fetchCurrentUser() } catch { authStore.logout(); return next('/login') }
    }
    const requiredRole = to.meta.role
    if (requiredRole && authStore.role !== requiredRole) return next(authStore.homePath)
  }
  next()
})
export default router
