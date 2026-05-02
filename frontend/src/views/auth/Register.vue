<template>
  <div class="auth-page">
    <div class="auth-shell">
      <el-card class="auth-card">
        <template #header>
          <div class="card-header">
            <div class="card-eyebrow">MR Project</div>
            <div class="card-title">用户注册</div>
            <div class="card-subtitle">创建账户后即可使用文档上传、SCG 生成与 MR 管理功能。</div>
          </div>
        </template>

        <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
          <el-form-item label="用户名" prop="username">
            <el-input v-model="form.username" placeholder="请输入用户名" />
          </el-form-item>

          <el-form-item label="姓名" prop="realName">
            <el-input v-model="form.realName" placeholder="请输入姓名" />
          </el-form-item>

          <el-form-item label="邮箱" prop="email">
            <el-input v-model="form.email" placeholder="请输入邮箱" />
          </el-form-item>

          <el-form-item label="密码" prop="password">
            <el-input v-model="form.password" type="password" show-password placeholder="请输入密码" />
          </el-form-item>

          <el-form-item label="确认密码" prop="confirmPassword">
            <el-input v-model="form.confirmPassword" type="password" show-password placeholder="请再次输入密码" />
          </el-form-item>

          <el-button type="primary" class="full-width" :loading="loading" @click="handleRegister">
            注册
          </el-button>

          <div class="footer-link">
            已有账号？
            <router-link to="/login">去登录</router-link>
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
import { register } from '../../api/auth'

const router = useRouter()
const formRef = ref()
const loading = ref(false)

const form = reactive({
  username: '',
  realName: '',
  email: '',
  password: '',
  confirmPassword: ''
})

const validateConfirmPassword = (rule, value, callback) => {
  if (!value) {
    callback(new Error('请再次输入密码'))
    return
  }

  if (value !== form.password) {
    callback(new Error('两次输入的密码不一致'))
    return
  }

  callback()
}

const rules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 4, max: 50, message: '用户名长度必须在4到50之间', trigger: 'blur' }
  ],
  realName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '邮箱格式不正确', trigger: ['blur', 'change'] }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, max: 50, message: '密码长度必须在6到50之间', trigger: 'blur' }
  ],
  confirmPassword: [{ validator: validateConfirmPassword, trigger: 'blur' }]
}

const handleRegister = async () => {
  try {
    await formRef.value.validate()
    loading.value = true
    await register(form)
    ElMessage.success('注册成功，请登录')
    router.push('/login')
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
  max-width: 500px;
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
