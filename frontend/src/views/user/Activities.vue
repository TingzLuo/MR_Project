<template>
  <div class="activities-page">
    <el-card shadow="never">
      <template #header>
        <div class="header-row">
          <span>操作记录中心</span>
          <el-button type="primary" text @click="loadActivities">刷新</el-button>
        </div>
      </template>

      <el-empty v-if="tableData.length === 0" description="暂无操作记录" />
      <template v-else>
        <el-table :data="tableData" border stripe>
          <el-table-column prop="fileName" label="文件名" min-width="260" />
          <el-table-column label="操作类型" width="160">
            <template #default="{ row }">{{ formatActionType(row.actionType) }}</template>
          </el-table-column>
          <el-table-column prop="time" label="时间" min-width="180" />
        </el-table>

        <div class="pagination-bar">
          <div class="pagination-total">Total {{ pagination.total }}</div>
          <div class="pagination-controls">
            <el-select v-model="pagination.pageSize" class="page-size-select" @change="handlePageSizeChange">
              <el-option label="10 / page" :value="10" />
              <el-option label="20 / page" :value="20" />
              <el-option label="50 / page" :value="50" />
            </el-select>
            <el-pagination
              background
              layout="prev, pager, next"
              :total="pagination.total"
              :page-size="pagination.pageSize"
              :current-page="pagination.pageNumber"
              @current-change="handlePageChange"
            />
          </div>
        </div>
      </template>
    </el-card>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { getActivityPagedList } from '../../api/dashboard'

const tableData = ref([])
const pagination = reactive({ pageNumber: 1, pageSize: 10, total: 0 })

const formatActionType = (actionType) => {
  return actionType === 'upload'
    ? '上传文件'
    : actionType === 'generateScg'
      ? '生成SCG'
      : actionType === 'generateMr'
        ? '生成MR'
        : actionType === 'saveScg'
          ? '保存SCG'
          : actionType === 'updateScg'
            ? '修改SCG'
          : actionType === 'saveMr'
            ? '保存MR'
          : actionType === 'addMr'
            ? '新增MR'
            : actionType === 'updateMr'
              ? '修改MR'
              : actionType === 'deleteMr'
                ? '删除MR'
                : actionType === 'updateProfile'
                  ? '修改个人信息'
                  : actionType === 'updatePassword'
                    ? '修改密码'
                : actionType
}

const loadActivities = async () => {
  const result = await getActivityPagedList({ pageNumber: pagination.pageNumber, pageSize: pagination.pageSize })
  tableData.value = result.items
  pagination.total = result.total
  pagination.pageNumber = result.pageNumber
  pagination.pageSize = result.pageSize
}

const handlePageChange = async (page) => {
  pagination.pageNumber = page
  await loadActivities()
}

const handlePageSizeChange = async () => {
  pagination.pageNumber = 1
  await loadActivities()
}

onMounted(async () => {
  await loadActivities()
})
</script>

<style scoped>
.activities-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.pagination-bar {
  margin-top: 18px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
}

.pagination-total {
  font-size: 14px;
}

.pagination-controls {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 12px;
  flex: 1;
}

.page-size-select {
  width: 120px;
}
</style>
