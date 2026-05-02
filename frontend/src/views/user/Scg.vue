<template>
  <div class="scg-page">
    <el-card>
      <template #header>
        <div class="header-row">
          <span>SCG 编辑与保存</span>
          <div class="header-actions">
            <el-button :disabled="selectedDocumentIds.length === 0" @click="handleLoadScg">加载已有 SCG</el-button>
            <el-button type="primary" :loading="generating" :disabled="selectedDocumentIds.length === 0" @click="handleGenerateScg">生成 SCG</el-button>
            <el-button type="success" :disabled="!currentScg" :loading="saving" @click="handleSaveScg">保存 SCG</el-button>
            <el-button type="info" :disabled="!currentScg" @click="handleExportScg">导出 SCG</el-button>
          </div>
        </div>
      </template>

      <div class="selector-row">
        <el-select v-model="selectedDocumentIds" multiple collapse-tags collapse-tags-tooltip placeholder="请选择一个或多个已上传文件" filterable clearable @change="handleSelectedDocumentsChange">
          <el-option v-for="item in documentOptions" :key="item.id" :label="`${item.documentName} (${item.fileType})`" :value="item.id" />
        </el-select>
        <span class="hint-text">可单选或多选文件合并生成一个 SCG。生成与加载都基于当前选中的整组文件进行处理。保存 SCG 后系统会自动将其视为已确认。</span>
        <el-tag v-if="currentScg" :type="currentScg.isConfirmed ? 'success' : 'info'">
          {{ currentScg.isConfirmed ? '已确认' : '未确认' }}
        </el-tag>
        <div v-if="currentScg" class="scg-meta">
          <span class="meta-label">当前 SCG 名称：</span>
          <span class="meta-value">{{ currentScg.scgName }}</span>
        </div>
      </div>
    </el-card>

    <el-card>
      <template #header>
        <div class="legend-row">
          <span>SCG 图编辑区</span>
          <div class="legend-items">
            <span class="legend-item input">cause</span>
            <span class="legend-item constraint">constraint</span>
            <span class="legend-item output">result</span>
          </div>
        </div>
      </template>

      <el-empty v-if="!currentScg" description="请选择文件并生成或加载 SCG" />
      <ScgGraphViewer v-else ref="graphViewerRef" :graph-data="editableGraph" :editable="true" @graph-change="handleGraphChange" />
    </el-card>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { getDocumentList } from '../../api/document'
import { exportScg, generateScg, getScgByDocuments, saveScg } from '../../api/scg'
import ScgGraphViewer from '../../components/scg/ScgGraphViewer.vue'

const documentOptions = ref([])
const selectedDocumentIds = ref([])
const currentScg = ref(null)
const editableGraph = ref({ nodes: [], edges: [] })
const generating = ref(false)
const saving = ref(false)
const graphViewerRef = ref()

const loadDocuments = async () => {
  const result = await getDocumentList({ keyword: '', pageNumber: 1, pageSize: 100 })
  documentOptions.value = result.items
}

const resetGraph = () => {
  currentScg.value = null
  editableGraph.value = { nodes: [], edges: [] }
}

const handleLoadScg = async () => {
  if (selectedDocumentIds.value.length === 0) {
    ElMessage.warning('请先选择文件')
    return
  }
  try {
    const result = await getScgByDocuments(selectedDocumentIds.value)
    if (!result) {
      resetGraph()
      ElMessage.warning('当前所选文件组合暂未生成 SCG')
      return
    }
    currentScg.value = result
    editableGraph.value = JSON.parse(JSON.stringify(result.scgGraph))
  } catch (error) {
    console.error('SCG load failed:', error)
    resetGraph()
  }
}

const handleGenerateScg = async () => {
  if (selectedDocumentIds.value.length === 0) {
    ElMessage.warning('请先选择文件')
    return
  }
  generating.value = true
  try {
    ElMessage.info('正在调用大模型生成 SCG，请耐心等待...')
    currentScg.value = await generateScg({ documentIds: selectedDocumentIds.value })
    editableGraph.value = JSON.parse(JSON.stringify(currentScg.value.scgGraph))
    ElMessage.success(`SCG 生成成功：${currentScg.value.scgName}`)
  } catch (error) {
    console.error('SCG generate failed:', error)
  } finally {
    generating.value = false
  }
}

const handleSaveScg = async () => {
  if (!currentScg.value) {
    ElMessage.warning('请先生成或加载 SCG')
    return
  }
  saving.value = true
  try {
    const graphData = graphViewerRef.value.getGraphData()
    const result = await saveScg(currentScg.value.id, { scgGraph: graphData })
    currentScg.value = result
    editableGraph.value = JSON.parse(JSON.stringify(result.scgGraph))
    ElMessage.success(`SCG 保存成功，已自动确认：${result.scgName}`)
    await handleLoadScg()
  } catch (error) {
    console.error('SCG save failed:', error)
  } finally {
    saving.value = false
  }
}

const handleExportScg = async () => {
  if (!currentScg.value) {
    ElMessage.warning('请先生成或加载 SCG')
    return
  }
  await exportScg(currentScg.value.id)
}

const handleGraphChange = (graphData) => { editableGraph.value = graphData }
const handleSelectedDocumentsChange = async (value) => { if (!value || value.length === 0) { resetGraph(); return }; await handleLoadScg() }

onMounted(async () => { try { await loadDocuments() } catch (error) { console.error('Load documents failed:', error) } })
</script>

<style scoped>
.scg-page { display: flex; flex-direction: column; gap: 20px; }
.header-row, .legend-row { display: flex; justify-content: space-between; align-items: center; gap: 16px; }
.header-actions, .legend-items { display: flex; gap: 12px; flex-wrap: wrap; }
.selector-row { display: flex; flex-direction: column; gap: 12px; }
.selector-row :deep(.el-select) { width: 520px; }
.hint-text { color: #6b7280; font-size: 14px; }
.scg-meta { display: flex; align-items: center; gap: 8px; color: #374151; font-size: 14px; }
.meta-label { color: #6b7280; }
.meta-value { color: #111827; font-weight: 600; }
.legend-item { display: inline-flex; align-items: center; padding: 4px 10px; border-radius: 999px; font-size: 12px; color: #111827; }
.input { background: #dbeafe; }
.constraint { background: #fef3c7; }
.output { background: #dcfce7; }
</style>
