<template>
  <div class="mr-page">
    <el-card>
      <template #header>
        <div class="header-row">
          <span>蜕变关系生成与管理</span>
          <div class="header-actions">
            <el-button type="primary" :loading="generating" :disabled="!currentScg" @click="handleGenerateMr">生成 MR</el-button>
            <el-button type="success" :disabled="!currentMr" @click="openAddDialog">新增 MR</el-button>
            <el-button type="warning" :disabled="!currentMr" :loading="saving" @click="handleSaveCurrent">保存当前结果</el-button>
            <el-button type="info" :disabled="!currentMr" @click="handleExportMr">导出 MR</el-button>
          </div>
        </div>
      </template>

      <div class="selector-row">
        <el-select v-model="selectedScgId" placeholder="请选择已确认的 SCG" filterable clearable @change="handleSelectedScgChange">
          <el-option v-for="item in scgOptions" :key="item.id" :label="item.scgName" :value="item.id">
            <div class="scg-option-row">
              <span class="scg-option-name">{{ item.scgName }}</span>
              <el-button class="delete-scg-btn" type="danger" text @mousedown.stop @click.stop="handleDeleteScg(item)">删除</el-button>
            </div>
          </el-option>
        </el-select>
        <span class="hint-text">MR 直接基于已确认的 SCG 生成，选择框中仅展示当前用户已确认的 SCG。</span>
        <div v-if="currentScg" class="selected-scg-meta">
          <span class="summary-text">当前 SCG：{{ currentScg.scgName }}</span>
          <span class="summary-text">来源文档：{{ currentScg.documentNamesSummary }}</span>
        </div>
      </div>

      <el-alert
        v-if="currentScg"
        title="当前选择的 SCG 已确认，可生成和管理蜕变关系"
        type="success"
        show-icon
        :closable="false"
      />
    </el-card>

    <el-card>
      <template #header>
        <div class="header-row">
          <span>当前 MR 列表</span>
          <span v-if="currentMr" class="summary-text">{{ currentScg?.scgName }}</span>
        </div>
      </template>

      <el-empty v-if="!currentMr || editableMrItems.length === 0" description="暂无蜕变关系，请先生成 MR" />
      <template v-else>
        <el-table :data="pagedMrItems" border stripe class="mr-table">
          <el-table-column prop="id" label="MR ID" width="120" />
          <el-table-column prop="inputRelation" label="输入关系" min-width="260" show-overflow-tooltip />
          <el-table-column prop="outputRelation" label="输出关系" min-width="280" show-overflow-tooltip />
          <el-table-column prop="description" label="描述" min-width="260" show-overflow-tooltip />
          <el-table-column label="生成时间" min-width="180">
            <template #default>
              {{ formatDateTime(currentMr.updatedAt) }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="180" fixed="right">
            <template #default="{ row }">
              <el-button class="action-link" type="primary" text @click="openEditDialog(row)">编辑</el-button>
              <el-button class="action-link" type="primary" text @click="handleViewDetail(row)">详情</el-button>
              <el-button class="action-link danger" type="danger" text @click="handleDeleteItem(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>

        <div class="pagination-bar">
          <div class="pagination-total">Total {{ mrPagination.total }}</div>
          <div class="pagination-controls">
            <el-select v-model="mrPagination.pageSize" class="page-size-select" @change="handlePageSizeChange">
              <el-option label="10 / page" :value="10" />
              <el-option label="20 / page" :value="20" />
              <el-option label="50 / page" :value="50" />
            </el-select>
            <el-pagination
              background
              layout="prev, pager, next"
              :total="mrPagination.total"
              :page-size="mrPagination.pageSize"
              :current-page="mrPagination.pageNumber"
              @current-change="handlePageChange"
            />
          </div>
        </div>
      </template>
    </el-card>

    <el-dialog v-model="detailVisible" title="蜕变关系详情" width="700px">
      <el-descriptions v-if="currentMrItem" :column="1" border>
        <el-descriptions-item label="MR ID">{{ currentMrItem.id }}</el-descriptions-item>
        <el-descriptions-item label="输入关系">{{ currentMrItem.inputRelation }}</el-descriptions-item>
        <el-descriptions-item label="输出关系">{{ currentMrItem.outputRelation }}</el-descriptions-item>
        <el-descriptions-item label="描述">{{ currentMrItem.description }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>

    <el-dialog v-model="editVisible" :title="editMode === 'add' ? '新增 MR' : '编辑 MR'" width="700px">
      <el-form ref="formRef" :model="editForm" label-width="100px">
        <el-form-item label="MR ID"><el-input v-model="editForm.id" /></el-form-item>
        <el-form-item label="输入关系"><el-input v-model="editForm.inputRelation" type="textarea" :rows="3" /></el-form-item>
        <el-form-item label="输出关系"><el-input v-model="editForm.outputRelation" type="textarea" :rows="3" /></el-form-item>
        <el-form-item label="描述"><el-input v-model="editForm.description" type="textarea" :rows="4" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmitItem">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { addMrItem, deleteMrItem, exportMr, generateMr, getMrByScgId, saveMr, updateMrItem } from '../../api/mr'
import { deleteScg, getConfirmedScgList } from '../../api/scg'

const scgOptions = ref([])
const selectedScgId = ref(null)
const currentScg = ref(null)
const currentMr = ref(null)
const editableMrItems = ref([])
const currentMrItem = ref(null)
const detailVisible = ref(false)
const editVisible = ref(false)
const generating = ref(false)
const saving = ref(false)
const submitting = ref(false)
const editMode = ref('add')
const editItemId = ref('')
const formRef = ref()
const editForm = reactive({ id: '', inputRelation: '', outputRelation: '', description: '' })
const mrPagination = reactive({ pageNumber: 1, pageSize: 10, total: 0 })

const pagedMrItems = computed(() => {
  const start = (mrPagination.pageNumber - 1) * mrPagination.pageSize
  const end = start + mrPagination.pageSize
  return editableMrItems.value.slice(start, end)
})

watch(editableMrItems, (value) => {
  mrPagination.total = value.length
  const maxPage = Math.max(1, Math.ceil(value.length / mrPagination.pageSize))
  if (mrPagination.pageNumber > maxPage) {
    mrPagination.pageNumber = maxPage
  }
}, { deep: true, immediate: true })

const handlePageChange = (page) => {
  mrPagination.pageNumber = page
}

const handlePageSizeChange = () => {
  mrPagination.pageNumber = 1
}

const loadConfirmedScgs = async () => {
  scgOptions.value = await getConfirmedScgList()
}

const resetState = () => {
  currentScg.value = null
  currentMr.value = null
  editableMrItems.value = []
  currentMrItem.value = null
  mrPagination.pageNumber = 1
  mrPagination.total = 0
}

const loadSelectedScg = async () => {
  if (!selectedScgId.value) {
    ElMessage.warning('请先选择已确认的 SCG')
    return
  }
  currentScg.value = scgOptions.value.find(item => item.id === selectedScgId.value) || null
  currentMr.value = await getMrByScgId(selectedScgId.value)
  editableMrItems.value = currentMr.value ? JSON.parse(JSON.stringify(currentMr.value.mrItems)) : []
  mrPagination.pageNumber = 1
}

const handleDeleteScg = async (item) => {
  await ElMessageBox.confirm(`确认删除 SCG「${item.scgName}」吗？删除后将永久移除该 SCG 及其关联的 MR 数据。`, '提示', { type: 'warning' })
  await deleteScg(item.id)
  if (selectedScgId.value === item.id) {
    selectedScgId.value = null
    resetState()
  }
  await loadConfirmedScgs()
  ElMessage.success('SCG 删除成功')
}

const handleGenerateMr = async () => {
  if (!currentScg.value) {
    ElMessage.warning('请先选择已确认的 SCG')
    return
  }
  generating.value = true
  try {
    ElMessage.info('正在调用大模型生成蜕变关系，请耐心等待...')
    currentMr.value = await generateMr({ scgId: currentScg.value.id })
    editableMrItems.value = JSON.parse(JSON.stringify(currentMr.value.mrItems))
    mrPagination.pageNumber = 1
    ElMessage.success('蜕变关系生成成功')
  } catch (error) {
    console.error('MR generate failed:', error)
  } finally {
    generating.value = false
  }
}

const handleSaveCurrent = async () => {
  if (!currentMr.value) {
    ElMessage.warning('当前没有可保存的 MR')
    return
  }
  saving.value = true
  try {
    currentMr.value = await saveMr(currentMr.value.id, { mrItems: editableMrItems.value })
    editableMrItems.value = JSON.parse(JSON.stringify(currentMr.value.mrItems))
    ElMessage.success('蜕变关系保存成功')
  } catch (error) {
    console.error('MR save failed:', error)
  } finally {
    saving.value = false
  }
}

const handleExportMr = async () => {
  if (!currentMr.value) {
    ElMessage.warning('当前没有可导出的 MR')
    return
  }
  await exportMr(currentMr.value.id)
}

const openAddDialog = () => {
  if (!currentMr.value) {
    ElMessage.warning('请先生成或加载 MR')
    return
  }
  editMode.value = 'add'
  editItemId.value = ''
  Object.assign(editForm, { id: '', inputRelation: '', outputRelation: '', description: '' })
  editVisible.value = true
}

const openEditDialog = (item) => {
  editMode.value = 'edit'
  editItemId.value = item.id
  Object.assign(editForm, JSON.parse(JSON.stringify(item)))
  editVisible.value = true
}

const handleSubmitItem = async () => {
  if (!currentMr.value) return
  submitting.value = true
  try {
    const payload = { ...editForm }
    if (editMode.value === 'add') {
      currentMr.value = await addMrItem(currentMr.value.id, payload)
      ElMessage.success('新增 MR 成功')
    } else {
      currentMr.value = await updateMrItem(currentMr.value.id, editItemId.value, payload)
      ElMessage.success('修改 MR 成功')
    }
    editableMrItems.value = JSON.parse(JSON.stringify(currentMr.value.mrItems))
    editVisible.value = false
  } catch (error) {
    console.error('MR submit failed:', error)
  } finally {
    submitting.value = false
  }
}

const handleDeleteItem = async (item) => {
  if (!currentMr.value) return
  await ElMessageBox.confirm('确认删除该条 MR 吗？', '提示', { type: 'warning' })
  currentMr.value = await deleteMrItem(currentMr.value.id, item.id)
  editableMrItems.value = JSON.parse(JSON.stringify(currentMr.value.mrItems))
  ElMessage.success('删除 MR 成功')
}

const handleViewDetail = (item) => {
  currentMrItem.value = item
  detailVisible.value = true
}

const handleSelectedScgChange = async (value) => {
  if (!value) {
    resetState()
    return
  }
  await loadSelectedScg()
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

onMounted(async () => {
  try {
    await loadConfirmedScgs()
  } catch (error) {
    console.error('Load confirmed SCGs failed:', error)
  }
})
</script>

<style scoped>
.mr-page { display: flex; flex-direction: column; gap: 20px; }
.header-row { display: flex; justify-content: space-between; align-items: center; gap: 16px; }
.header-actions { display: flex; gap: 12px; flex-wrap: wrap; }
.selector-row { display: flex; flex-direction: column; gap: 12px; }
.selector-row :deep(.el-select) { width: 520px; }
.hint-text, .summary-text { color: #6b7280; font-size: 14px; }
.selected-scg-meta { display: flex; flex-direction: column; gap: 6px; }
.scg-option-row { display: flex; justify-content: space-between; align-items: center; gap: 12px; width: 100%; }
.scg-option-name { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.delete-scg-btn { padding: 0; }
.mr-table :deep(.el-table__cell) { vertical-align: top; }
.mr-table :deep(.cell) { line-height: 1.6; }
.action-link { padding: 0; font-size: 13px; font-weight: 400; }
.action-link.danger { color: #ef4444; }
.mr-table :deep(.el-table__fixed-right .cell),
.mr-table :deep(td:last-child .cell) {
  display: flex;
  align-items: center;
  gap: 14px;
  flex-wrap: wrap;
}

.action-link + .action-link {
  margin-left: 0;
}
.pagination-bar { margin-top: 16px; display: flex; justify-content: space-between; align-items: center; gap: 16px; color: #6b7280; }
.pagination-total { font-size: 14px; }
.pagination-controls { display: flex; justify-content: flex-end; align-items: center; gap: 12px; flex: 1; }
.page-size-select { width: 120px; }
</style>

