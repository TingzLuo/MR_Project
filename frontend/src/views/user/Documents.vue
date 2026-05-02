<template>
  <div class="document-page">
    <el-card class="upload-card">
      <template #header>
        <span>文件上传</span>
      </template>
      <DocumentUpload
        ref="documentUploadRef"
        :loading="uploading"
        :max-file-size-mb="maxFileSizeMb"
        @upload="handleUpload"
      />
    </el-card>

    <el-card class="table-card">
      <template #header>
        <div class="header-row">
          <span>我的文件</span>
          <div class="search-row">
            <el-input
              v-model="queryForm.keyword"
              placeholder="请输入文件名搜索"
              clearable
              @keyup.enter="handleSearch"
              @clear="handleSearch"
            />
            <el-button type="primary" @click="handleSearch">搜索</el-button>
          </div>
        </div>
      </template>

      <el-table :data="tableData" v-loading="tableLoading" border>
        <el-table-column prop="documentName" label="文档名称" min-width="180" />
        <el-table-column prop="originalFileName" label="原始文件名" min-width="220" />
        <el-table-column prop="fileType" label="类型" width="100" />
        <el-table-column label="大小" width="120">
          <template #default="{ row }">
            {{ formatFileSize(row.fileSize) }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="160">
          <template #default="{ row }">
            <el-tag :type="getProcessStatusTagType(row.processStatus)" effect="light">
              {{ formatProcessStatus(row.processStatus) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="上传时间" min-width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link @click="handleViewDetail(row.id)">详情</el-button>
            <el-button type="danger" link @click="handleDelete(row.id)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-row">
        <el-pagination
          background
          layout="total, sizes, prev, pager, next"
          :total="pagination.total"
          :page-size="pagination.pageSize"
          :current-page="pagination.pageNumber"
          :page-sizes="[10, 20, 50]"
          @current-change="handlePageChange"
          @size-change="handleSizeChange"
        />
      </div>
    </el-card>

    <el-dialog v-model="detailVisible" title="文件详情" width="600px">
      <el-descriptions v-if="detailData" :column="1" border>
        <el-descriptions-item label="文档名称">{{ detailData.documentName }}</el-descriptions-item>
        <el-descriptions-item label="原始文件名">{{ detailData.originalFileName }}</el-descriptions-item>
        <el-descriptions-item label="存储文件名">{{ detailData.storedFileName }}</el-descriptions-item>
        <el-descriptions-item label="文件类型">{{ detailData.fileType }}</el-descriptions-item>
        <el-descriptions-item label="文件大小">{{ formatFileSize(detailData.fileSize) }}</el-descriptions-item>
        <el-descriptions-item label="处理状态">
          <el-tag :type="getProcessStatusTagType(detailData.processStatus)" effect="light">
            {{ formatProcessStatus(detailData.processStatus) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="文件路径">{{ detailData.filePath }}</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ formatDateTime(detailData.createdAt) }}</el-descriptions-item>
        <el-descriptions-item label="更新时间">{{ formatDateTime(detailData.updatedAt) }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import DocumentUpload from '../../components/document/DocumentUpload.vue'
import { deleteDocument, getDocumentDetail, getDocumentList, uploadDocuments } from '../../api/document'

const maxFileSizeMb = 20
const uploading = ref(false)
const tableLoading = ref(false)
const detailVisible = ref(false)
const detailData = ref(null)
const tableData = ref([])
const documentUploadRef = ref()

const queryForm = reactive({ keyword: '' })
const pagination = reactive({ pageNumber: 1, pageSize: 10, total: 0 })

const loadTableData = async () => {
  tableLoading.value = true
  try {
    const result = await getDocumentList({ keyword: queryForm.keyword, pageNumber: pagination.pageNumber, pageSize: pagination.pageSize })
    tableData.value = result.items
    pagination.total = result.total
  } finally {
    tableLoading.value = false
  }
}

const handleUpload = async (files) => {
  const formData = new FormData()
  files.forEach(file => formData.append('files', file))
  uploading.value = true
  try {
    const result = await uploadDocuments(formData)
    ElMessage.success(`上传成功，共上传 ${result.uploadedItems.length} 个文件`)
    documentUploadRef.value.clearFiles()
    pagination.pageNumber = 1
    await loadTableData()
  } finally {
    uploading.value = false
  }
}

const handleSearch = async () => {
  pagination.pageNumber = 1
  await loadTableData()
}

const handlePageChange = async (pageNumber) => {
  pagination.pageNumber = pageNumber
  await loadTableData()
}

const handleSizeChange = async (pageSize) => {
  pagination.pageSize = pageSize
  pagination.pageNumber = 1
  await loadTableData()
}

const handleViewDetail = async (id) => {
  detailData.value = await getDocumentDetail(id)
  detailVisible.value = true
}

const handleDelete = async (id) => {
  await ElMessageBox.confirm('确认删除该文件吗？删除后将无法恢复。', '提示', { type: 'warning' })
  await deleteDocument(id)
  ElMessage.success('删除成功')
  if (tableData.value.length === 1 && pagination.pageNumber > 1) pagination.pageNumber -= 1
  await loadTableData()
}

const formatFileSize = (size) => {
  if (!size && size !== 0) return '-'
  if (size < 1024) return `${size} B`
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(2)} KB`
  return `${(size / (1024 * 1024)).toFixed(2)} MB`
}

const formatProcessStatus = (status) => {
  return status === 'uploaded'
    ? '已上传'
    : status === 'parsed'
      ? '文档已解析'
      : status === 'scgGenerated'
        ? 'SCG已生成'
        : status === 'scgConfirmed'
          ? 'SCG已确认'
          : status === 'mrGenerated'
            ? 'MR已生成'
            : status === 'archived'
              ? '已归档'
              : status || '-'
}

const getProcessStatusTagType = (status) => {
  return status === 'uploaded'
    ? 'info'
    : status === 'parsed'
      ? 'warning'
      : status === 'scgGenerated'
        ? 'primary'
        : status === 'scgConfirmed'
          ? 'success'
          : status === 'mrGenerated'
            ? 'success'
            : status === 'archived'
              ? 'info'
              : 'info'
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
  await loadTableData()
})
</script>

<style scoped>
.document-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.upload-card,
.table-card {
  width: 100%;
}

.header-row {
  align-items: center;
}

.search-row {
  width: min(100%, 420px);
}

.search-row :deep(.el-input) {
  flex: 1;
}

.document-page :deep(td:last-child .cell) {
  display: flex;
  align-items: center;
  gap: 14px;
  flex-wrap: wrap;
}

.document-page :deep(td:last-child .cell .el-button + .el-button) {
  margin-left: 0;
}

.pagination-row {
  margin-top: 22px;
  display: flex;
  justify-content: flex-end;
}
</style>

