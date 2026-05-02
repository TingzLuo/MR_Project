<template>
  <div class="admin-users-page">
    <el-card>
      <template #header>
        <div class="header-row">
          <span>用户管理</span>
          <div class="header-actions">
            <el-button type="danger" :disabled="selectedIds.length === 0" @click="handleBatchDelete">批量删除</el-button>
            <el-button type="primary" @click="openCreateDialog">新增用户</el-button>
          </div>
        </div>
      </template>

      <div class="search-row">
        <el-input v-model="queryForm.keyword" placeholder="请输入用户名/姓名/邮箱/手机号搜索" clearable @keyup.enter="loadTableData" @clear="loadTableData" />
        <el-button type="primary" @click="loadTableData">搜索</el-button>
      </div>

      <el-table :data="tableData" border stripe @selection-change="handleSelectionChange">
        <el-table-column type="selection" width="50" />
        <el-table-column prop="id" label="账号ID" width="90" />
        <el-table-column prop="username" label="用户名" min-width="140" />
        <el-table-column prop="phone" label="手机号" min-width="140" />
        <el-table-column prop="email" label="邮箱号" min-width="180" />
        <el-table-column prop="role" label="角色" width="120" />
        <el-table-column label="最后登录" min-width="180">
          <template #default="{ row }">{{ formatDateTime(row.lastLoginAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <div class="action-group">
              <el-button type="primary" text @click="openEditDialog(row)">编辑</el-button>
              <el-button type="danger" text @click="handleDelete(row.id)">删除</el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-row">
        <el-pagination background layout="total, sizes, prev, pager, next" :total="pagination.total" :page-size="pagination.pageSize" :current-page="pagination.pageNumber" :page-sizes="[10,20,50]" @current-change="handlePageChange" @size-change="handleSizeChange" />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="dialogMode === 'create' ? '新增用户' : '编辑用户'" width="720px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="用户名" prop="username"><el-input v-model="form.username" /></el-form-item>
        <el-form-item label="姓名" prop="realName"><el-input v-model="form.realName" /></el-form-item>
        <el-form-item label="邮箱" prop="email"><el-input v-model="form.email" /></el-form-item>
        <el-form-item label="手机号" prop="phone"><el-input v-model="form.phone" /></el-form-item>
        <el-form-item label="角色" prop="role">
          <el-select v-model="form.role"><el-option label="普通用户" value="user" /><el-option label="管理员" value="admin" /></el-select>
        </el-form-item>
        <el-form-item label="个人描述" prop="profileDescription"><el-input v-model="form.profileDescription" type="textarea" :rows="4" /></el-form-item>
        <el-form-item v-if="dialogMode === 'create'" label="密码" prop="password"><el-input v-model="form.password" type="password" show-password /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { batchDeleteAdminUsers, createAdminUser, deleteAdminUser, getAdminUserList, updateAdminUser } from '../../api/admin'

const tableData = ref([])
const selectedIds = ref([])
const dialogVisible = ref(false)
const dialogMode = ref('create')
const submitting = ref(false)
const editingId = ref(0)
const formRef = ref()

const queryForm = reactive({ keyword: '' })
const pagination = reactive({ pageNumber: 1, pageSize: 10, total: 0 })
const form = reactive({ username: '', realName: '', email: '', phone: '', role: 'user', status: 'enabled', profileDescription: '', password: '' })

const rules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  realName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  email: [{ required: true, message: '请输入邮箱', trigger: 'blur' }],
  role: [{ required: true, message: '请选择角色', trigger: 'change' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const loadTableData = async () => {
  const result = await getAdminUserList({ keyword: queryForm.keyword, pageNumber: pagination.pageNumber, pageSize: pagination.pageSize })
  tableData.value = result.items
  pagination.total = result.total
}

const resetForm = () => {
  Object.assign(form, { username: '', realName: '', email: '', phone: '', role: 'user', status: 'enabled', profileDescription: '', password: '' })
}

const openCreateDialog = () => {
  dialogMode.value = 'create'
  editingId.value = 0
  resetForm()
  dialogVisible.value = true
}

const openEditDialog = (row) => {
  dialogMode.value = 'edit'
  editingId.value = row.id
  Object.assign(form, {
    username: row.username,
    realName: row.realName,
    email: row.email,
    phone: row.phone === '未绑定' ? '' : row.phone,
    role: row.role === '管理员' ? 'admin' : 'user',
    status: 'enabled',
    profileDescription: row.profileDescription || '',
    password: ''
  })
  dialogVisible.value = true
}

const handleSubmit = async () => {
  await formRef.value.validate()
  submitting.value = true
  try {
    if (dialogMode.value === 'create') {
      await createAdminUser({ ...form })
      ElMessage.success('新增用户成功')
    } else {
      await updateAdminUser(editingId.value, { ...form })
      ElMessage.success('编辑用户成功')
    }
    dialogVisible.value = false
    await loadTableData()
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (id) => {
  await ElMessageBox.confirm('确认删除该用户吗？', '提示', { type: 'warning' })
  await deleteAdminUser(id)
  ElMessage.success('删除用户成功')
  await loadTableData()
}

const handleBatchDelete = async () => {
  await ElMessageBox.confirm('确认批量删除选中的用户吗？', '提示', { type: 'warning' })
  await batchDeleteAdminUsers({ userIds: selectedIds.value })
  ElMessage.success('批量删除成功')
  selectedIds.value = []
  await loadTableData()
}

const handleSelectionChange = (rows) => {
  selectedIds.value = rows.map(item => item.id)
}

const handlePageChange = async (page) => {
  pagination.pageNumber = page
  await loadTableData()
}

const handleSizeChange = async (size) => {
  pagination.pageSize = size
  pagination.pageNumber = 1
  await loadTableData()
}

const formatDateTime = (value) => {
  if (!value) return '从未登录'
  const date = new Date(value)
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')
  const hour = `${date.getHours()}`.padStart(2, '0')
  const minute = `${date.getMinutes()}`.padStart(2, '0')
  const second = `${date.getSeconds()}`.padStart(2, '0')
  return `${year}/${month}/${day} ${hour}:${minute}:${second}`
}

onMounted(async () => {
  try {
    await loadTableData()
  } catch (error) {
    console.error('Load admin users failed:', error)
  }
})
</script>

<style scoped>
.admin-users-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.header-actions {
  flex-wrap: wrap;
}

.search-row {
  margin-bottom: 18px;
  width: min(100%, 520px);
}

.search-row :deep(.el-input) {
  flex: 1;
}

.pagination-row {
  margin-top: 22px;
  display: flex;
  justify-content: flex-end;
}

.action-group {
  display: flex;
  align-items: center;
  gap: 10px;
  white-space: nowrap;
}
</style>
