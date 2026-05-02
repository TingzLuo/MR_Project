<template>
  <div class="admin-dashboard">
    <el-card class="chart-card" shadow="never">
      <template #header>
        <div class="card-header">
          <span>系统统计总览</span>
        </div>
      </template>

      <UserStatsChart
        :stats="chartStats"
        :labels="['用户总数', '文档总数', 'SCG 总数', 'MR 总数']"
        title="系统统计总览"
      />
    </el-card>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive } from 'vue'
import { getAdminOverview } from '../../api/admin'
import UserStatsChart from '../../components/dashboard/UserStatsChart.vue'

const overview = reactive({
  userCount: 0,
  documentCount: 0,
  scgCount: 0,
  mrCount: 0
})

const chartStats = computed(() => ({
  fileCount: overview.userCount,
  llmCallCount: overview.documentCount,
  scgCount: overview.scgCount,
  mrCount: overview.mrCount
}))

onMounted(async () => {
  Object.assign(overview, await getAdminOverview())
})
</script>

<style scoped>
.admin-dashboard {
  display: flex;
  flex-direction: column;
}

.chart-card {
  border: none;
  background: transparent;
}

.card-header {
  font-size: 18px;
  font-weight: 700;
  color: var(--text-primary);
}
</style>
