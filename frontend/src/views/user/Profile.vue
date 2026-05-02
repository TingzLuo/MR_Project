<template>
  <div class="profile-page">
    <el-card>
      <template #header>
        <div class="header-row">
          <span>个人信息管理</span>
          <div class="header-actions">
            <el-button type="warning" @click="passwordVisible = true">修改密码</el-button>
            <el-button type="primary" :loading="saving" @click="handleSaveProfile">保存信息</el-button>
          </div>
        </div>
      </template>

      <el-form ref="profileFormRef" :model="profileForm" :rules="profileRules" label-width="100px" class="profile-form">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="profileForm.username" />
        </el-form-item>
        <el-form-item label="姓名" prop="realName">
          <el-input v-model="profileForm.realName" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="profileForm.email" />
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="profileForm.phone" />
        </el-form-item>
        <el-form-item label="角色">
          <el-input :model-value="profileInfo.role" disabled />
        </el-form-item>
        <el-form-item label="注册时间">
          <el-input :model-value="formatDateTime(profileInfo.createdAt)" disabled />
        </el-form-item>
        <el-form-item label="个人描述" prop="profileDescription">
          <el-input v-model="profileForm.profileDescription" type="textarea" :rows="5" maxlength="500" show-word-limit />
        </el-form-item>
      </el-form>
    </el-card>

    <el-card>
      <template #header>
        <div class="header-row">
          <span>个人操作记录自动清理</span>
          <el-button type="primary" :loading="savingCleanupSetting" @click="handleSaveCleanupSetting">保存清理配置</el-button>
        </div>
      </template>

      <el-form :model="cleanupForm" label-width="180px" class="cleanup-form">
        <el-form-item label="是否跟随全局配置">
          <el-switch v-model="cleanupForm.useGlobalSetting" />
        </el-form-item>
        <template v-if="cleanupForm.useGlobalSetting">
          <el-form-item label="全局启用状态">
            <el-input :model-value="cleanupForm.globalEnabled ? '已启用' : '未启用'" disabled />
          </el-form-item>
          <el-form-item label="全局保留天数">
            <el-input :model-value="String(cleanupForm.globalRetentionDays)" disabled />
          </el-form-item>
          <el-form-item label="全局清理间隔">
            <el-input :model-value="`${cleanupForm.globalIntervalValue}${formatIntervalUnit(cleanupForm.globalIntervalUnit)}`" disabled />
          </el-form-item>
        </template>
        <template v-else>
          <el-form-item label="是否启用个人自动清理">
            <el-switch v-model="cleanupForm.enabled" />
          </el-form-item>
          <el-form-item label="保留天数">
            <el-input-number v-model="cleanupForm.retentionDays" :min="1" :max="3650" />
          </el-form-item>
          <el-form-item label="清理间隔">
            <div class="interval-row">
              <el-input-number v-model="cleanupForm.intervalValue" :min="1" :max="720" />
              <el-select v-model="cleanupForm.intervalUnit" class="interval-unit-select">
                <el-option label="小时" value="hour" />
                <el-option label="天" value="day" />
                <el-option label="周" value="week" />
                <el-option label="月" value="month" />
              </el-select>
            </div>
          </el-form-item>
        </template>
      </el-form>
    </el-card>

    <el-dialog v-model="passwordVisible" title="修改密码" width="520px">
      <el-form ref="passwordFormRef" :model="passwordForm" :rules="passwordRules" label-width="100px">
        <el-form-item label="旧密码" prop="oldPassword">
          <el-input v-model="passwordForm.oldPassword" type="password" show-password />
        </el-form-item>
        <el-form-item label="新密码" prop="newPassword">
          <el-input v-model="passwordForm.newPassword" type="password" show-password />
        </el-form-item>
        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input v-model="passwordForm.confirmPassword" type="password" show-password />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="passwordVisible = false">取消</el-button>
        <el-button type="primary" :loading="changingPassword" @click="handleChangePassword">确认修改</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useRouter } from 'vue-router'
import {
  getProfile,
  getUserOperationRecordCleanupSetting,
  updatePassword,
  updateProfile,
  updateUserOperationRecordCleanupSetting
} from '../../api/user'
import { useAuthStore } from '../../stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const saving = ref(false)
const changingPassword = ref(false)
const savingCleanupSetting = ref(false)
const passwordVisible = ref(false)
const profileFormRef = ref()
const passwordFormRef = ref()
const profileInfo = ref({ role: '', createdAt: null })

const profileForm = reactive({
  username: '',
  realName: '',
  email: '',
  phone: '',
  profileDescription: ''
})

const cleanupForm = reactive({
  useGlobalSetting: true,
  enabled: true,
  retentionDays: 90,
  intervalValue: 24,
  intervalUnit: 'hour',
  globalEnabled: true,
  globalRetentionDays: 90,
  globalIntervalValue: 24,
  globalIntervalUnit: 'hour'
})

const passwordForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const validateNewPassword = (rule, value, callback) => {
  if (!value) {
    callback(new Error('请输入新密码'))
    return
  }
  if (value.length < 6 || value.length > 50) {
    callback(new Error('新密码长度必须在6到50之间'))
    return
  }
  if (value === passwordForm.oldPassword) {
    callback(new Error('新密码不能与旧密码一致'))
    return
  }
  callback()
}

const validateConfirmPassword = (rule, value, callback) => {
  if (!value) {
    callback(new Error('请再次输入新密码'))
    return
  }
  if (value !== passwordForm.newPassword) {
    callback(new Error('两次输入的新密码不一致'))
    return
  }
  callback()
}

const profileRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 4, max: 50, message: '用户名长度必须在4到50之间', trigger: 'blur' }
  ],
  realName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '邮箱格式不正确', trigger: ['blur', 'change'] }
  ],
  phone: [{ max: 20, message: '手机号长度不能超过20', trigger: 'blur' }],
  profileDescription: [{ max: 500, message: '个人描述长度不能超过500', trigger: 'blur' }]
}

const passwordRules = {
  oldPassword: [{ required: true, message: '请输入旧密码', trigger: 'blur' }],
  newPassword: [{ validator: validateNewPassword, trigger: 'blur' }],
  confirmPassword: [{ validator: validateConfirmPassword, trigger: 'blur' }]
}

const loadProfile = async () => {
  const result = await getProfile()
  Object.assign(profileForm, {
    username: result.username,
    realName: result.realName,
    email: result.email,
    phone: result.phone,
    profileDescription: result.profileDescription
  })
  profileInfo.value = {
    role: result.role,
    createdAt: result.createdAt
  }
}

const loadCleanupSetting = async () => {
  const result = await getUserOperationRecordCleanupSetting()
  Object.assign(cleanupForm, result)
}

const handleSaveProfile = async () => {
  await profileFormRef.value.validate()
  saving.value = true
  try {
    const result = await updateProfile({ ...profileForm })
    profileInfo.value = {
      role: result.role,
      createdAt: result.createdAt
    }
    authStore.userInfo = {
      ...authStore.userInfo,
      username: result.username,
      realName: result.realName,
      email: result.email,
      phone: result.phone,
      profileDescription: result.profileDescription,
      role: result.role
    }
    ElMessage.success('个人信息保存成功')
  } finally {
    saving.value = false
  }
}

const handleSaveCleanupSetting = async () => {
  savingCleanupSetting.value = true
  try {
    const result = await updateUserOperationRecordCleanupSetting({
      useGlobalSetting: cleanupForm.useGlobalSetting,
      enabled: cleanupForm.enabled,
      retentionDays: cleanupForm.retentionDays,
      intervalValue: cleanupForm.intervalValue,
      intervalUnit: cleanupForm.intervalUnit
    })
    Object.assign(cleanupForm, result)
    ElMessage.success('个人操作记录自动清理配置保存成功')
  } finally {
    savingCleanupSetting.value = false
  }
}

const handleChangePassword = async () => {
  await passwordFormRef.value.validate()
  changingPassword.value = true
  try {
    await updatePassword({ ...passwordForm })
    ElMessage.success('密码修改成功，请重新登录')
    passwordVisible.value = false
    Object.assign(passwordForm, { oldPassword: '', newPassword: '', confirmPassword: '' })
    authStore.logout()
    router.push('/login')
  } finally {
    changingPassword.value = false
  }
}

const formatDateTime = (value) => {
  if (!value) return '-'
  const date = new Date(value)
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')
  const hour = `${date.getHours()}`.padStart(2, '0')
  const minute = `${date.getMinutes()}`.padStart(2, '0')
  const second = `${date.getSeconds()}`.padStart(2, '0')
  return `${year}-${month}-${day} ${hour}:${minute}:${second}`
}

const formatIntervalUnit = (value) => {
  return value === 'day' ? '天' : value === 'week' ? '周' : value === 'month' ? '月' : '小时'
}

onMounted(async () => {
  await loadProfile()
  await loadCleanupSetting()
})
</script>

<style scoped>
.profile-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.header-actions {
  flex-wrap: wrap;
}

.profile-form,
.cleanup-form {
  max-width: 820px;
}

.interval-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.interval-unit-select {
  width: 120px;
}
</style>
