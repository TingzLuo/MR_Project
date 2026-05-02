<template>
  <div class="auth-page">
    <div class="auth-shell">
      <el-card class="auth-card">
        <template #header>
          <div class="card-header">
            <div class="card-eyebrow">MR Project</div>
            <div class="card-title">用户登录</div>
            <div class="card-subtitle">进入系统以继续管理待测程序、SCG 与蜕变关系。</div>
          </div>
        </template>

        <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
          <el-form-item label="用户名/邮箱" prop="username">
            <el-input v-model="form.username" placeholder="请输入用户名或邮箱" />
          </el-form-item>

          <el-form-item label="密码" prop="password">
            <el-input v-model="form.password" type="password" show-password placeholder="请输入密码" />
          </el-form-item>

          <el-button type="primary" class="full-width" :loading="loading" @click="handleLogin">
            登录
          </el-button>

          <div class="footer-link">
            还没有账号？
            <router-link to="/register">去注册</router-link>
          </div>
        </el-form>
      </el-card>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const formRef = ref()
const loading = ref(false)

const form = reactive({
  username: '',
  password: ''
})

const rules = {
  username: [{ required: true, message: '请输入用户名或邮箱', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const handleLogin = async () => {
  try {
    await formRef.value.validate()
    loading.value = true
    const result = await authStore.loginAction(form)
    ElMessage.success('登录成功')
    router.push(authStore.getHomePath(result.userInfo.role))
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 32px 18px;
  background:
    radial-gradient(circle at 15% 20%, rgba(224, 232, 244, 0.9), transparent 28%),
    linear-gradient(180deg, #f8fafc 0%, #edf2f7 100%);
}

.auth-shell {
  width: 100%;
  max-width: 460px;
}

.auth-card {
  width: 100%;
}

.card-header {
  text-align: center;
}

.card-eyebrow {
  margin-bottom: 8px;
  color: var(--brand-primary);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.card-title {
  font-size: 26px;
  font-weight: 700;
}

.card-subtitle {
  margin-top: 10px;
  color: var(--text-secondary);
  font-size: 14px;
  line-height: 1.7;
}

.full-width {
  width: 100%;
  margin-top: 6px;
}

.footer-link {
  margin-top: 18px;
  text-align: center;
  color: var(--text-secondary);
}
</style>
