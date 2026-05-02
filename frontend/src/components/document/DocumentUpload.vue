<template>
  <el-upload
    ref="uploadRef"
    drag
    multiple
    :auto-upload="false"
    :limit="10"
    :file-list="fileList"
    accept=".docx,.pdf"
    :on-change="handleChange"
    :on-remove="handleRemove"
    :on-exceed="handleExceed"
  >
    <div class="upload-content">
      <div class="upload-title">拖拽文件到此处或点击上传</div>
      <div class="upload-desc">仅支持 docx 或 pdf，单个文件不超过 {{ maxFileSizeMb }} MB</div>
    </div>
  </el-upload>

  <div class="upload-actions">
    <el-button @click="clearFiles">清空文件</el-button>
    <el-button type="primary" :loading="loading" @click="submitUpload">开始上传</el-button>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { ElMessage } from 'element-plus'

const props = defineProps({
  loading: {
    type: Boolean,
    default: false
  },
  maxFileSizeMb: {
    type: Number,
    default: 20
  }
})

const emit = defineEmits(['upload'])

const uploadRef = ref()
const fileList = ref([])

const allowedExtensions = ['docx', 'pdf']

const handleChange = (file, currentFileList) => {
  const extension = file.name.split('.').pop()?.toLowerCase()
  const sizeLimit = props.maxFileSizeMb * 1024 * 1024

  if (!extension || !allowedExtensions.includes(extension)) {
    ElMessage.error('仅支持上传 docx 或 pdf 文件')
    uploadRef.value.handleRemove(file)
    return
  }

  if (file.size <= 0) {
    ElMessage.error('不允许上传空文件')
    uploadRef.value.handleRemove(file)
    return
  }

  if (file.size > sizeLimit) {
    ElMessage.error(`文件大小不能超过 ${props.maxFileSizeMb} MB`)
    uploadRef.value.handleRemove(file)
    return
  }

  fileList.value = currentFileList
}

const handleRemove = (file, currentFileList) => {
  fileList.value = currentFileList
}

const handleExceed = () => {
  ElMessage.error('单次最多选择 10 个文件')
}

const clearFiles = () => {
  uploadRef.value.clearFiles()
  fileList.value = []
}

const submitUpload = () => {
  if (fileList.value.length === 0) {
    ElMessage.error('请先选择文件')
    return
  }

  emit('upload', fileList.value.map(item => item.raw))
}

defineExpose({
  clearFiles
})
</script>

<style scoped>
.upload-content {
  padding: 24px 0;
  text-align: center;
}

.upload-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.upload-desc {
  margin-top: 8px;
  color: #909399;
}

.upload-actions {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
