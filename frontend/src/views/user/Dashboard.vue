<template>
  <div class="dashboard-page">
    <el-card shadow="never">
      <UserStatsChart :stats="stats" />
    </el-card>

    <el-card shadow="never">
      <template #header>
        <div class="section-header-row">
          <div class="section-header">最近操作记录</div>
          <el-button type="primary" text @click="router.push('/user/activities')">查看全部记录</el-button>
        </div>
      </template>

      <el-empty v-if="activities.length === 0" description="暂无最近操作记录" />
      <el-table v-else :data="activities" border stripe>
        <el-table-column prop="fileName" label="文件名" min-width="240" />
        <el-table-column label="操作类型" width="140">
          <template #default="{ row }">{{ formatActionType(row.actionType) }}</template>
        </el-table-column>
        <el-table-column prop="time" label="时间" min-width="180" />
      </el-table>
    </el-card>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getRecentActivities, getUserStats } from '../../api/dashboard'
import UserStatsChart from '../../components/dashboard/UserStatsChart.vue'

const router = useRouter()
const stats = reactive({ fileCount: 0, llmCallCount: 0, scgCount: 0, mrCount: 0 })
const activities = ref([])

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

onMounted(async () => {
  Object.assign(stats, await getUserStats())
  activities.value = await getRecentActivities()
})
</script>

<style scoped>
.dashboard-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.section-header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
}

.section-header {
  font-size: 16px;
  font-weight: 600;
  color: #111827;
}
</style>





