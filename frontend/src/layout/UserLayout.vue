<template>
  <div class="layout-container">
    <header class="app-header merged-header">
      <div class="menu-area">
        <el-menu :default-active="activeMenu" mode="horizontal" router class="user-top-menu">
          <el-menu-item index="/user/dashboard">首页</el-menu-item>
          <el-menu-item index="/user/documents">待测程序管理</el-menu-item>
          <el-menu-item index="/user/scg">SCG 生成与展示</el-menu-item>
          <el-menu-item index="/user/mr">蜕变关系生成</el-menu-item>
          <el-menu-item index="/user/activities">操作记录</el-menu-item>
          <el-menu-item index="/user/profile">个人信息</el-menu-item>
        </el-menu>
      </div>

      <div class="actions">
        <span class="welcome">当前用户：{{ authStore.userInfo?.realName || authStore.userInfo?.username }}</span>
        <el-button type="danger" plain @click="handleLogout">退出登录</el-button>
      </div>
    </header>

    <main class="layout-content">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { ElMessageBox } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const activeMenu = computed(() => route.path)

const handleLogout = async () => {
  await ElMessageBox.confirm('确认退出当前登录账号吗？', '提示', {
    type: 'warning'
  })
  authStore.logout()
  router.push('/login')
}
</script>

<style scoped>
.layout-container {
  min-height: 100vh;
  background: transparent;
}

.app-header.merged-header {
  position: sticky;
  top: 0;
  z-index: 20;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  padding: 0 28px;
  background: rgba(248, 250, 252, 0.9);
  color: var(--text-primary);
  backdrop-filter: blur(14px);
  border-bottom: 1px solid rgba(212, 220, 232, 0.75);
  box-shadow: 0 8px 30px rgba(15, 23, 42, 0.04);
}

.menu-area {
  flex: 1;
  min-width: 0;
}

.user-top-menu {
  border-bottom: none;
  background: transparent;
}

.user-top-menu :deep(.el-menu-item) {
  height: 68px;
  line-height: 68px;
  padding: 0 18px;
  color: var(--text-regular);
  background: transparent;
  border-bottom-width: 3px;
  font-weight: 600;
}

.user-top-menu :deep(.el-menu-item:hover) {
  color: var(--text-primary);
  background: rgba(234, 241, 251, 0.5);
}

.user-top-menu :deep(.el-menu-item.is-active) {
  color: var(--brand-primary);
  background: transparent;
  border-bottom-color: var(--brand-primary);
}

.actions {
  gap: 14px;
  flex-shrink: 0;
}

.welcome {
  white-space: nowrap;
  font-size: 14px;
}

.layout-content {
  padding: 28px;
}

@media (max-width: 1100px) {
  .app-header.merged-header {
    flex-direction: column;
    align-items: stretch;
    padding: 0 18px 16px;
  }

  .actions {
    justify-content: flex-end;
  }

  .user-top-menu :deep(.el-menu-item) {
    height: 54px;
    line-height: 54px;
    padding: 0 14px;
  }

  .layout-content {
    padding: 20px 16px 24px;
  }
}
</style>
