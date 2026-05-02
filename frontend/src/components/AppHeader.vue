<template>
  <header class="app-header">
    <div class="title">{{ title }}</div>
    <div class="actions">
      <span class="welcome">当前用户：{{ authStore.userInfo?.realName || authStore.userInfo?.username }}</span>
      <el-button type="danger" plain @click="handleLogout">退出登录</el-button>
    </div>
  </header>
</template>

<script setup>
import { ElMessageBox } from 'element-plus'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

defineProps({
  title: {
    type: String,
    default: '系统首页'
  }
})

const router = useRouter()
const authStore = useAuthStore()

const handleLogout = async () => {
  await ElMessageBox.confirm('确认退出当前登录账号吗？', '提示', {
    type: 'warning'
  })
  authStore.logout()
  router.push('/login')
}
</script>

<style scoped>
.app-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  background: #1f2d3d;
  color: #fff;
}

.title {
  font-size: 18px;
  font-weight: 600;
}

.actions {
  display: flex;
  align-items: center;
  gap: 16px;
}

.welcome {
  color: #e5eaf3;
}
</style>
