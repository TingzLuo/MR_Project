<template>
  <div class="backup-page">
    <el-card class="page-card" shadow="never">
      <template #header>
        <div class="page-header">
          <div>
            <div class="page-title">导出与备份</div>
            <div class="page-subtitle">集中管理系统数据导出、全量备份与操作记录自动清理配置。</div>
          </div>
        </div>
      </template>

      <div class="section-title">数据导出</div>
      <div class="export-grid">
        <el-card class="export-card" shadow="hover">
          <div class="card-icon users">U</div>
          <div class="card-title">用户数据</div>
          <div class="card-desc">导出系统中全部用户基本信息，便于归档、审计和统计分析。</div>
          <el-button type="primary" plain @click="exportAdminUsers">导出用户数据</el-button>
        </el-card>

        <el-card class="export-card" shadow="hover">
          <div class="card-icon docs">D</div>
          <div class="card-title">文档数据</div>
          <div class="card-desc">导出全部待测程序文档记录，包含文件元信息和流程状态。</div>
          <el-button type="primary" plain @click="exportAdminDocuments">导出文档数据</el-button>
        </el-card>

        <el-card class="export-card" shadow="hover">
          <div class="card-icon scg">S</div>
          <div class="card-title">SCG 数据</div>
          <div class="card-desc">导出系统内全部 SCG 记录，适用于结果留存和论文素材整理。</div>
          <el-button type="primary" plain @click="exportAdminScg">导出 SCG 数据</el-button>
        </el-card>

        <el-card class="export-card" shadow="hover">
          <div class="card-icon mr">M</div>
          <div class="card-title">MR 数据</div>
          <div class="card-desc">导出全部蜕变关系记录，便于结果分析、复核与后续整理。</div>
          <el-button type="primary" plain @click="exportAdminMr">导出 MR 数据</el-button>
        </el-card>
      </div>

      <div class="section-title backup-title">系统备份</div>
      <el-card class="backup-card" shadow="never">
        <div class="backup-content">
          <div>
            <div class="backup-name">全量系统备份</div>
            <div class="backup-desc">
              将用户、文档、SCG、MR 等核心记录统一打包为 ZIP 文件，适合阶段性备份和系统迁移留档。
            </div>
          </div>
          <el-button type="success" @click="exportSystemBackup">下载系统备份</el-button>
        </div>
      </el-card>

      <div class="section-title backup-title">操作记录自动清理</div>
      <el-card class="setting-card" shadow="never">
        <el-form :model="cleanupForm" label-width="160px" class="cleanup-form">
          <el-form-item label="是否启用自动清理">
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
        </el-form>
        <div class="setting-actions">
          <el-button type="primary" :loading="savingCleanup" @click="handleSaveCleanupSetting">保存清理配置</el-button>
        </div>
      </el-card>
    </el-card>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  exportAdminDocuments,
  exportAdminMr,
  exportAdminScg,
  exportAdminUsers,
  exportSystemBackup,
  getOperationRecordCleanupSetting,
  updateOperationRecordCleanupSetting
} from '../../api/admin'

const savingCleanup = ref(false)
const cleanupForm = reactive({
  enabled: true,
  retentionDays: 90,
  intervalValue: 24,
  intervalUnit: 'hour'
})

const loadCleanupSetting = async () => {
  const result = await getOperationRecordCleanupSetting()
  Object.assign(cleanupForm, result)
}

const handleSaveCleanupSetting = async () => {
  savingCleanup.value = true
  try {
    const result = await updateOperationRecordCleanupSetting({ ...cleanupForm })
    Object.assign(cleanupForm, result)
    ElMessage.success('操作记录自动清理配置保存成功')
  } finally {
    savingCleanup.value = false
  }
}

onMounted(async () => {
  await loadCleanupSetting()
})
</script>

<style scoped>
.backup-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.page-card {
  border-radius: var(--radius-card);
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.page-title {
  font-size: 24px;
}

.page-subtitle {
  margin-top: 8px;
  font-size: 14px;
}

.section-title {
  margin: 10px 0 16px;
  font-size: 16px;
}

.export-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(280px, 1fr));
  gap: 20px;
}

.export-card {
  border-radius: 16px;
}

.card-icon {
  width: 46px;
  height: 46px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 14px;
  font-size: 18px;
  font-weight: 700;
  margin-bottom: 16px;
}

.card-icon.users {
  background: #e8f0fd;
  color: #2d5b9f;
}

.card-icon.docs {
  background: #e8f6ef;
  color: #1f8a5b;
}

.card-icon.scg {
  background: #fdf2de;
  color: #b67711;
}

.card-icon.mr {
  background: #fce9e9;
  color: #c74a4a;
}

.card-title {
  font-size: 18px;
}

.card-desc {
  margin: 12px 0 18px;
  min-height: 48px;
}

.backup-title {
  margin-top: 30px;
}

.backup-card {
  border: 1px solid #d8ebe1;
  background: linear-gradient(135deg, #f3fbf6 0%, #f8fbfe 100%);
}

.backup-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 24px;
}

.backup-name {
  font-size: 18px;
  font-weight: 700;
  color: #1c5d44;
}

.backup-desc {
  margin-top: 10px;
  max-width: 720px;
}

.setting-card {
  border: 1px solid #e4eaf2;
}

.cleanup-form {
  max-width: 560px;
}

.interval-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.interval-unit-select {
  width: 120px;
}

.setting-actions {
  margin-top: 8px;
}

@media (max-width: 900px) {
  .export-grid {
    grid-template-columns: 1fr;
  }

  .backup-content {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
