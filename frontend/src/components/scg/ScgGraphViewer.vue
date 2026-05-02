<template>
  <div class="graph-editor">
    <div class="toolbar">
      <div class="tool-group">
        <el-button size="small" @click="addNode('input')">新增 Cause</el-button>
        <el-button size="small" @click="addNode('output')">新增 Result</el-button>
        <el-button size="small" @click="addConstraintRelation">新增 Constraint</el-button>
      </div>
      <div class="tool-group">
        <el-button size="small" :disabled="!selectedElement" @click="editSelectedText">修改文本</el-button>
        <el-button size="small" type="danger" :disabled="!selectedElement" @click="deleteSelected">删除选中元素</el-button>
      </div>
    </div>
    <div class="graph-container-wrapper">
      <div ref="containerRef" class="graph-container"></div>
      <div
        v-if="nodeTooltip.visible"
        class="node-text-tooltip"
        :style="{
          left: `${nodeTooltip.x}px`,
          top: `${nodeTooltip.y}px`
        }"
      >
        {{ nodeTooltip.content }}
      </div>
          </div>
    <div class="editor-hint">
      
    </div>
  </div>
</template>

<script setup>
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import LogicFlow, {
  RectNode,
  RectNodeModel,
  CircleNode,
  CircleNodeModel,
  PolylineEdge,
  PolylineEdgeModel,
  h
} from '@logicflow/core'
import '@logicflow/core/lib/style/index.css'

const props = defineProps({
  graphData: {
    type: Object,
    default: () => ({ nodes: [], edges: [] })
  },
  editable: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['graph-change'])

const containerRef = ref()
const selectedElement = ref(null)
const nodeTooltip = ref({
  visible: false,
  content: '',
  x: 0,
  y: 0
})
let lf = null
let nodeSeed = 1000
let edgeSeed = 1000
let constraintSeed = 1000
let suppressNextPropRender = false

const FONT_FAMILY = '"Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", sans-serif'
const LABEL_FONT_SIZE = 12
const LABEL_SIDE_PADDING = 9
const LABEL_END_GAP = 18
const SEGMENT_TIE_THRESHOLD = 10
const NODE_TOOLTIP_OFFSET_X = 18
const NODE_TOOLTIP_OFFSET_Y = 18

const normalizeText = (value) => String(value || '').replace(/\r\n/g, '\n').trim()

const ellipsisText = (value, maxLength = 14) => {
  const normalized = normalizeText(value).replace(/\n+/g, ' ')
  const chars = Array.from(normalized)
  if (chars.length <= maxLength) return normalized
  return `${chars.slice(0, maxLength).join('')}...`
}

const getNodeTextLimit = (type) => (type === 'output' ? 6 : 14)
const getNodeDisplayText = (label, type) => ellipsisText(label, getNodeTextLimit(type))

const isNodeTextTruncated = (label, type) => {
  const normalized = normalizeText(label).replace(/\n+/g, ' ')
  return Array.from(normalized).length > getNodeTextLimit(type)
}

const getNodeEditorBox = (nodeData) => {
  const isOutput = nodeData.type === 'output'
  const width = isOutput ? 100 : 220
  const height = isOutput ? 100 : 64
  return {
    width,
    height,
    left: nodeData.x - width / 2,
    top: nodeData.y - height / 2
  }
}

const normalizeConstraintMeta = (title, detail = '') => {
  const normalizedTitle = normalizeText(title)
  const normalizedDetail = normalizeText(detail)
  if (normalizedTitle && normalizedDetail) return { title: normalizedTitle, detail: normalizedDetail }
  if (normalizedTitle) return { title: normalizedTitle, detail: normalizedTitle }
  if (normalizedDetail) return { title: normalizedDetail, detail: normalizedDetail }
  return { title: '', detail: '' }
}

const showTextPreview = async (title, content) => {
  const normalizedContent = normalizeText(content)
  if (!normalizedContent) return
  await ElMessageBox.alert(
    `<div style="white-space: pre-wrap; line-height: 1.7; word-break: break-word;">${normalizedContent}</div>`,
    title,
    {
      dangerouslyUseHTMLString: true,
      confirmButtonText: '知道了'
    }
  )
}

const hideNodeTooltip = () => {
  nodeTooltip.value.visible = false
}

const updateNodeTooltip = (event, data) => {
    const label = data?.properties?.fullText || data?.properties?.label || ''
  if (!isNodeTextTruncated(label, data?.type)) {
    hideNodeTooltip()
    return
  }

  const containerRect = containerRef.value?.getBoundingClientRect()
  if (!containerRect) return

  nodeTooltip.value = {
    visible: true,
    content: normalizeText(label),
    x: event.clientX - containerRect.left + NODE_TOOLTIP_OFFSET_X,
    y: event.clientY - containerRect.top - NODE_TOOLTIP_OFFSET_Y
  }
}

const getNodeFullText = (nodeLike) => {
  if (!nodeLike) return ''
  return String(
    nodeLike.properties?.fullText
      || nodeLike.properties?.label
      || nodeLike.label
      || nodeLike.text?.value
      || ''
  )
}

const getNodeTextById = (nodeId, fallbackNode = null) => {
  const sourceNode = (props.graphData?.nodes || []).find(node => node.id === nodeId)
  const sourceText = normalizeText(sourceNode?.label || '')
  if (sourceText) return sourceText

  const fallbackText = normalizeText(getNodeFullText(fallbackNode))
  if (fallbackText) return fallbackText

  const nodeData = lf?.getNodeDataById?.(nodeId)
  const nodeDataText = normalizeText(getNodeFullText(nodeData))
  if (nodeDataText) return nodeDataText

  const nodeModel = lf?.getNodeModelById?.(nodeId)
  const modelText = normalizeText(getNodeFullText(nodeModel))
  if (modelText) return modelText

  return ''
}

const buildNodeElement = (data) => {
  const fullText = getNodeTextById(data.id, data)
  return {
    kind: 'node',
    id: data.id,
    label: fullText,
    type: data.type,
    properties: {
      ...(data.properties || {}),
      fullText,
      label: fullText
    }
  }
}
class InputNodeView extends RectNode {
  getText() {
    return null
  }

  getShape() {
    const { model } = this.props
    const style = model.getNodeStyle()
    const label = model.properties?.displayText || model.properties?.fullText || model.properties?.label || ''
    return h('g', {}, [
      h('rect', {
        ...style,
        x: model.x - model.width / 2,
        y: model.y - model.height / 2,
        width: model.width,
        height: model.height,
        rx: model.radius,
        ry: model.radius
      }),
      h('text', {
        x: model.x,
        y: model.y,
        'text-anchor': 'middle',
        'dominant-baseline': 'central',
        fill: '#1f2937',
        'font-size': 13,
        'font-family': FONT_FAMILY,
        style: 'pointer-events:none;'
      }, label)
    ])
  }
}

class InputNodeModel extends RectNodeModel {
  initNodeData(data) {
    super.initNodeData(data)
    this.width = 220
    this.height = 64
    this.radius = 10
  }

  getNodeStyle() {
    const style = super.getNodeStyle()
    style.stroke = '#2563eb'
    style.fill = '#dbeafe'
    style.strokeWidth = 2
    return style
  }
}

class OutputNodeView extends CircleNode {
  getText() {
    return null
  }

  getShape() {
    const { model } = this.props
    const style = model.getNodeStyle()
    const label = model.properties?.displayText || model.properties?.fullText || model.properties?.label || ''
    return h('g', {}, [
      h('circle', {
        ...style,
        cx: model.x,
        cy: model.y,
        r: model.r
      }),
      h('text', {
        x: model.x,
        y: model.y,
        'text-anchor': 'middle',
        'dominant-baseline': 'central',
        fill: '#14532d',
        'font-size': 12,
        'font-family': FONT_FAMILY,
        style: 'pointer-events:none;'
      }, label)
    ])
  }
}

class OutputNodeModel extends CircleNodeModel {
  initNodeData(data) {
    super.initNodeData(data)
    this.r = 50
  }

  getNodeStyle() {
    const style = super.getNodeStyle()
    style.stroke = '#16a34a'
    style.fill = '#dcfce7'
    style.strokeWidth = 2
    return style
  }
}

class ParallelConstraintEdgeView extends PolylineEdge {
  getText() {
    const { model } = this.props
    const label = model.properties?.label || ''
    if (!label) return null

    const fullText = normalizeText(label)
    const anchor = this.getTextAnchor(model)
    const fontSize = this.getFontSize(anchor.segmentLength)
    const displayText = ellipsisText(fullText, this.getTextLimit(anchor.segmentLength, fontSize))
    const backgroundWidth = this.getBackgroundWidth(displayText, fontSize)

    return h('g', {
      transform: `rotate(${anchor.angle}, ${anchor.x}, ${anchor.y})`,
      className: 'parallel-edge-label-group'
    }, [
      h('title', {}, fullText),
      h('rect', {
        x: anchor.x - backgroundWidth / 2,
        y: anchor.y - 12,
        width: backgroundWidth,
        height: 24,
        rx: 10,
        ry: 10,
        fill: '#fef3c7',
        stroke: '#f59e0b',
        'stroke-width': 1
      }),
      h('text', {
        x: anchor.x,
        y: anchor.y + 1,
        'text-anchor': 'middle',
        'dominant-baseline': 'central',
        fill: '#92400e',
        'font-size': fontSize,
        'font-family': FONT_FAMILY,
        style: 'pointer-events:auto; cursor:pointer; user-select:none;'
      }, displayText)
    ])
  }

  getBackgroundWidth(text, fontSize) {
    return Math.max(70, Array.from(text).length * fontSize + LABEL_SIDE_PADDING * 2)
  }

  getFontSize(segmentLength) {
    if (segmentLength >= 220) return LABEL_FONT_SIZE
    if (segmentLength >= 160) return 11
    return 10
  }

  getTextLimit(segmentLength, fontSize) {
    const limitedLength = Math.max(42, segmentLength * 0.65)
    const usableLength = Math.max(36, limitedLength - LABEL_END_GAP * 2 - LABEL_SIDE_PADDING * 2)
    return Math.max(4, Math.floor(usableLength / Math.max(10, fontSize)))
  }

  getPolylinePoints(model) {
    const pointsList = Array.isArray(model.pointsList) ? model.pointsList : []
    const normalizedPoints = pointsList
      .map(point => ({ x: Number(point.x), y: Number(point.y) }))
      .filter(point => Number.isFinite(point.x) && Number.isFinite(point.y))

    if (normalizedPoints.length >= 2) {
      return normalizedPoints.filter((point, index) => {
        if (index === 0) return true
        const previousPoint = normalizedPoints[index - 1]
        return previousPoint.x !== point.x || previousPoint.y !== point.y
      })
    }

    const start = model.startPoint || { x: model.sourceNode?.x || 0, y: model.sourceNode?.y || 0 }
    const end = model.endPoint || { x: model.targetNode?.x || 0, y: model.targetNode?.y || 0 }
    return [start, end]
  }

  getSegmentCenterDistance(segment, visualCenter) {
    const midX = (segment.start.x + segment.end.x) / 2
    const midY = (segment.start.y + segment.end.y) / 2
    return Math.hypot(midX - visualCenter.x, midY - visualCenter.y)
  }

  pickLabelSegment(points) {
    const segments = []
    for (let index = 0; index < points.length - 1; index += 1) {
      const start = points[index]
      const end = points[index + 1]
      const length = Math.hypot(end.x - start.x, end.y - start.y)
      if (length < 1) continue
      segments.push({ start, end, length })
    }

    if (segments.length === 0) {
      const start = points[0] || { x: 0, y: 0 }
      const end = points[points.length - 1] || start
      return { start, end, length: Math.hypot(end.x - start.x, end.y - start.y) }
    }

    const box = points.reduce((result, point) => ({
      minX: Math.min(result.minX, point.x),
      maxX: Math.max(result.maxX, point.x),
      minY: Math.min(result.minY, point.y),
      maxY: Math.max(result.maxY, point.y)
    }), {
      minX: Number.POSITIVE_INFINITY,
      maxX: Number.NEGATIVE_INFINITY,
      minY: Number.POSITIVE_INFINITY,
      maxY: Number.NEGATIVE_INFINITY
    })

    const visualCenter = {
      x: (box.minX + box.maxX) / 2,
      y: (box.minY + box.maxY) / 2
    }

    const maxLength = Math.max(...segments.map(segment => segment.length))
    const candidateSegments = segments.filter(segment => maxLength - segment.length <= SEGMENT_TIE_THRESHOLD)

    if (candidateSegments.length === 1) return candidateSegments[0]

    return candidateSegments.slice().sort((left, right) => {
      const distanceDiff = this.getSegmentCenterDistance(left, visualCenter) - this.getSegmentCenterDistance(right, visualCenter)
      if (Math.abs(distanceDiff) > 1) return distanceDiff
      return right.length - left.length
    })[0]
  }

  getTextAnchor(model) {
    const points = this.getPolylinePoints(model)
    const segment = this.pickLabelSegment(points)
    const x = (segment.start.x + segment.end.x) / 2
    const y = (segment.start.y + segment.end.y) / 2
    let angle = Math.atan2(segment.end.y - segment.start.y, segment.end.x - segment.start.x) * 180 / Math.PI
    if (angle > 90 || angle < -90) angle += 180
    return { x, y, angle, segmentLength: segment.length }
  }
}

class ParallelConstraintEdgeModel extends PolylineEdgeModel {
  getEdgeStyle() {
    const style = super.getEdgeStyle()
    style.stroke = '#6b7280'
    style.strokeWidth = 2
    return style
  }

  getTextStyle() {
    const style = super.getTextStyle()
    style.opacity = 0
    return style
  }
}

const registerElements = () => {
  lf.register({ type: 'input', view: InputNodeView, model: InputNodeModel })
  lf.register({ type: 'output', view: OutputNodeView, model: OutputNodeModel })
  lf.register({ type: 'define', view: ParallelConstraintEdgeView, model: ParallelConstraintEdgeModel })
  lf.register({ type: 'causal', view: ParallelConstraintEdgeView, model: ParallelConstraintEdgeModel })
  lf.register({ type: 'condition', view: ParallelConstraintEdgeView, model: ParallelConstraintEdgeModel })
}

const syncSeeds = () => {
  const nodeIds = (props.graphData?.nodes || []).map(item => Number((item.id || '').replace('n', ''))).filter(Number.isFinite)
  const edgeIds = (props.graphData?.edges || []).map(item => Number((item.id || '').replace('e', ''))).filter(Number.isFinite)
  const constraintIds = (props.graphData?.nodes || []).filter(item => item.type === 'constraint').map(item => Number((item.id || '').replace('n', ''))).filter(Number.isFinite)
  nodeSeed = Math.max(1000, ...(nodeIds.length ? nodeIds : [0])) + 1
  edgeSeed = Math.max(1000, ...(edgeIds.length ? edgeIds : [0])) + 1
  constraintSeed = Math.max(1000, ...(constraintIds.length ? constraintIds : [0])) + 1
}

const transformGraphData = () => {
  const rawNodes = props.graphData?.nodes || []
  const rawEdges = props.graphData?.edges || []
  const nodeMap = new Map(rawNodes.map(item => [item.id, item]))

  const visibleNodes = rawNodes.filter(item => item.type === 'input' || item.type === 'output').map(node => ({
    id: node.id,
    type: node.type,
    x: node.x,
    y: node.y,
    text: { value: '', x: node.x, y: node.y },
    properties: {
      label: normalizeText(node.label),
      fullText: normalizeText(node.label),
      displayText: getNodeDisplayText(node.label, node.type)
    }
  }))

  const displayEdges = []
  const displayEdgeMap = new Map()

  rawNodes.filter(item => item.type === 'constraint').forEach(constraintNode => {
    const incomingEdges = rawEdges.filter(edge => edge.targetNodeId === constraintNode.id)
    const outgoingEdges = rawEdges.filter(edge => edge.sourceNodeId === constraintNode.id)

    incomingEdges.forEach(inEdge => {
      outgoingEdges.forEach(outEdge => {
        const sourceNode = nodeMap.get(inEdge.sourceNodeId)
        const targetNode = nodeMap.get(outEdge.targetNodeId)
        if (!sourceNode || !targetNode) return
        if (sourceNode.type !== 'input' || targetNode.type !== 'output') return

        const key = `${sourceNode.id}_${targetNode.id}`
        const meta = normalizeConstraintMeta(constraintNode.label, constraintNode.detail || constraintNode.label)

        if (displayEdgeMap.has(key)) {
          const existing = displayEdgeMap.get(key)
          existing.constraintItems.push({ id: constraintNode.id, title: meta.title, detail: meta.detail })
          existing.constraintIds.push(constraintNode.id)
        } else {
          const edge = {
            id: `display_${sourceNode.id}_${targetNode.id}`,
            type: outEdge.type || 'causal',
            sourceNodeId: sourceNode.id,
            targetNodeId: targetNode.id,
            text: meta.title,
            properties: {
              label: meta.title,
              detail: meta.detail,
              constraintItems: [{ id: constraintNode.id, title: meta.title, detail: meta.detail }],
              constraintIds: [constraintNode.id],
              incomingType: inEdge.type || 'define',
              outgoingType: outEdge.type || 'causal'
            }
          }
          displayEdges.push(edge)
          displayEdgeMap.set(key, edge)
        }
      })
    })
  })

  rawEdges.forEach(edge => {
    const sourceNode = nodeMap.get(edge.sourceNodeId)
    const targetNode = nodeMap.get(edge.targetNodeId)
    if (!sourceNode || !targetNode) return
    if (sourceNode.type === 'input' && targetNode.type === 'output') {
      const key = `${sourceNode.id}_${targetNode.id}`
      if (!displayEdgeMap.has(key)) {
        const directEdge = {
          id: `display_${sourceNode.id}_${targetNode.id}`,
          type: edge.type,
          sourceNodeId: edge.sourceNodeId,
          targetNodeId: edge.targetNodeId,
          text: '',
          properties: {
            label: '',
            detail: '',
            constraintItems: [],
            constraintIds: [],
            incomingType: edge.type,
            outgoingType: edge.type
          }
        }
        displayEdges.push(directEdge)
        displayEdgeMap.set(key, directEdge)
      }
    }
  })

  return { nodes: visibleNodes, edges: displayEdges }
}

const renderGraph = () => {
  if (!lf) return
  lf.render(transformGraphData())
  syncSeeds()
  selectedElement.value = null
  hideNodeTooltip()
}

const buildGraphData = () => {
  const graphData = lf.getGraphData()
  const visibleNodes = []
  const nodeIdSet = new Set()
  ;(graphData.nodes || []).forEach(node => {
    if (nodeIdSet.has(node.id)) return
    nodeIdSet.add(node.id)
    visibleNodes.push({
      id: node.id,
      type: node.type,
      label: normalizeText(node.properties?.label || node.properties?.fullText || ''),
      x: Math.round(node.x),
      y: Math.round(node.y)
    })
  })

  const constraintNodes = []
  const convertedEdges = []
  const edgeIdSet = new Set()
  let localConstraintSeed = constraintSeed

  ;(graphData.edges || []).forEach(edge => {
    const constraintItems = Array.isArray(edge.properties?.constraintItems) && edge.properties.constraintItems.length > 0
      ? edge.properties.constraintItems
      : [normalizeConstraintMeta(edge.properties?.label || '', edge.properties?.detail || '')].filter(item => item.title || item.detail)

    if (constraintItems.length > 0) {
      const sourceNode = graphData.nodes.find(item => item.id === edge.sourceNodeId)
      const targetNode = graphData.nodes.find(item => item.id === edge.targetNodeId)
      const baseX = Math.round(((sourceNode?.x || 0) + (targetNode?.x || 0)) / 2)
      const baseY = Math.round(((sourceNode?.y || 0) + (targetNode?.y || 0)) / 2)

      constraintItems.forEach((item, index) => {
        const meta = normalizeConstraintMeta(item.title, item.detail)
        let constraintId = item.id || edge.properties?.constraintIds?.[index] || `n${localConstraintSeed++}`
        if (nodeIdSet.has(constraintId)) {
          constraintId = `n${localConstraintSeed++}`
        }
        nodeIdSet.add(constraintId)
        constraintNodes.push({ id: constraintId, type: 'constraint', label: meta.title, detail: meta.detail, x: baseX, y: baseY + index * 28 })

        const incomingEdgeId = `${edge.id}_in_${index}`
        if (!edgeIdSet.has(incomingEdgeId)) {
          edgeIdSet.add(incomingEdgeId)
          convertedEdges.push({ id: incomingEdgeId, sourceNodeId: edge.sourceNodeId, targetNodeId: constraintId, type: edge.properties?.incomingType || 'define' })
        }

        const outgoingEdgeId = `${edge.id}_out_${index}`
        if (!edgeIdSet.has(outgoingEdgeId)) {
          edgeIdSet.add(outgoingEdgeId)
          convertedEdges.push({ id: outgoingEdgeId, sourceNodeId: constraintId, targetNodeId: edge.targetNodeId, type: edge.properties?.outgoingType || edge.type || 'causal' })
        }
      })
    } else {
      const directEdgeId = edge.id
      if (!edgeIdSet.has(directEdgeId)) {
        edgeIdSet.add(directEdgeId)
        convertedEdges.push({ id: directEdgeId, sourceNodeId: edge.sourceNodeId, targetNodeId: edge.targetNodeId, type: edge.type || 'define' })
      }
    }
  })

  constraintSeed = localConstraintSeed
  return { nodes: [...visibleNodes, ...constraintNodes], edges: convertedEdges }
}

const emitGraphChange = () => {
  suppressNextPropRender = true
  emit('graph-change', buildGraphData())
}

const addNode = (type) => {
  if (!props.editable) return
  if (type === 'constraint') return addConstraintRelation()

  const id = `n${nodeSeed++}`
  const labelMap = { input: '新输入节点', output: '新输出节点' }
  lf.addNode({
    id,
    type,
    x: 160 + (nodeSeed % 4) * 120,
    y: 180 + (nodeSeed % 3) * 80,
    text: { value: '', x: 0, y: 0 },
    properties: {
      label: labelMap[type],
      fullText: labelMap[type],
      displayText: getNodeDisplayText(labelMap[type], type)
    }
  })
  emitGraphChange()
}

const addConstraintRelation = async () => {
  if (!props.editable) return
  const graphData = lf.getGraphData()
  const inputNode = (graphData.nodes || []).find(node => node.type === 'input')
  const outputNode = (graphData.nodes || []).find(node => node.type === 'output')
  if (!inputNode || !outputNode) {
    ElMessage.warning('请先确保图中至少存在一个 Input 和一个 Output 节点')
    return
  }

  const result = await ElMessageBox.prompt('请输入约束文本，系统将把它显示在 Input 到 Output 的连线上', '新增 Constraint', {
    inputValue: '新约束关系',
    inputPattern: /\S+/,
    inputErrorMessage: '约束文本不能为空'
  }).catch(() => null)
  if (!result?.value) return

  const title = normalizeText(result.value)
  lf.addEdge({
    id: `e${edgeSeed++}`,
    type: 'causal',
    sourceNodeId: inputNode.id,
    targetNodeId: outputNode.id,
    text: title,
    properties: {
      label: title,
      detail: title,
      constraintItems: [{ id: `n${constraintSeed++}`, title, detail: title }],
      constraintIds: [],
      incomingType: 'define',
      outgoingType: 'causal'
    }
  })
  emitGraphChange()
}

const promptForElementText = async (element) => {
  if (!element) return null
  const currentText = element.properties?.detail || element.properties?.label || element.label || ''
  const result = await ElMessageBox.prompt('请输入新的文本内容', element.kind === 'node' ? '修改节点文本' : '修改约束文本', {
    inputValue: currentText,
    inputType: 'textarea',
    inputPattern: /\S+/,
    inputErrorMessage: '文本不能为空'
  }).catch(() => null)
  return result?.value ? normalizeText(result.value) : null
}

const updateNodeText = (element, value) => {
  lf.setProperties(element.id, {
    label: value,
    fullText: value,
    displayText: getNodeDisplayText(value, element.type)
  })
}

const updateEdgeText = (element, value) => {
  lf.updateText(element.id, value)
  lf.setProperties(element.id, {
    ...(element.properties || {}),
    label: value,
    detail: value,
    constraintItems: [{ id: element.properties?.constraintItems?.[0]?.id || `n${constraintSeed++}`, title: value, detail: value }]
  })
}

const editSelectedText = async () => {
  if (!selectedElement.value) return
  const value = await promptForElementText(selectedElement.value)
  if (!value) return

  if (selectedElement.value.kind === 'node') updateNodeText(selectedElement.value, value)
  else updateEdgeText(selectedElement.value, value)

  hideNodeTooltip()
  emitGraphChange()
}

const deleteSelected = () => {
  if (!selectedElement.value || !props.editable) return
  lf.deleteElement(selectedElement.value.id)
  selectedElement.value = null
  hideNodeTooltip()
  emitGraphChange()
}

const bindEvents = () => {
  lf.on('node:click', ({ data }) => {
    selectedElement.value = buildNodeElement(data)
  })

      lf.on('node:dbclick', async ({ data }) => {
    if (!props.editable) return
    const nodeElement = buildNodeElement(data)
    selectedElement.value = nodeElement
    const value = await promptForElementText(nodeElement)
    if (!value) return
    updateNodeText(nodeElement, value)
    hideNodeTooltip()
    emitGraphChange()
  })

  lf.on('node:mouseenter', ({ data, e }) => updateNodeTooltip(e, data))
  lf.on('node:mousemove', ({ data, e }) => updateNodeTooltip(e, data))
  lf.on('node:mouseleave', () => hideNodeTooltip())

  lf.on('edge:click', ({ data }) => {
    selectedElement.value = { kind: 'edge', id: data.id, label: data.properties?.label || '', type: data.type, properties: data.properties || {} }
  })

  lf.on('blank:click', () => {
    selectedElement.value = null
    hideNodeTooltip()
  })

  lf.on('node:drop', () => {
    hideNodeTooltip()
    if (props.editable) emitGraphChange()
  })

  lf.on('edge:add', ({ data }) => {
    if (!props.editable) return
    if (!data.id) data.id = `e${edgeSeed++}`
    if (!data.type) data.type = 'define'
    emitGraphChange()
  })

  lf.on('node:delete', () => {
    hideNodeTooltip()
    if (props.editable) emitGraphChange()
  })

  lf.on('edge:delete', () => {
    if (props.editable) emitGraphChange()
  })

    lf.on('text:click', async ({ data }) => {
    const detail = data.properties?.detail || data.properties?.label || ''
    await showTextPreview('约束详情', detail)
  })

  lf.on('text:dbclick', async ({ data }) => {
    if (!props.editable) return
    const edgeElement = {
      kind: 'edge',
      id: data.id,
      label: data.properties?.label || '',
      type: data.type,
      properties: data.properties || {}
    }
    selectedElement.value = edgeElement
    const value = await promptForElementText(edgeElement)
    if (!value) return
    updateEdgeText(edgeElement, value)
    emitGraphChange()
  })
}

onMounted(() => {
  lf = new LogicFlow({
    container: containerRef.value,
    grid: true,
    stopScrollGraph: true,
    stopMoveGraph: false,
    isSilentMode: !props.editable,
    edgeType: 'define'
  })
  registerElements()
  bindEvents()
  renderGraph()
})

onBeforeUnmount(() => {
  if (lf) {
    lf.destroy()
    lf = null
  }
})

watch(() => props.graphData, async () => {
  if (suppressNextPropRender) {
    suppressNextPropRender = false
    return
  }
  await nextTick()
  renderGraph()
}, { deep: true })

defineExpose({ getGraphData: buildGraphData })
</script>

<style scoped>
.graph-editor { width: 100%; }
.toolbar { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 12px; margin-bottom: 12px; }
.tool-group { display: flex; gap: 8px; flex-wrap: wrap; }
.graph-container-wrapper { position: relative; }
.graph-container { width: 100%; height: 560px; background: #fff; border: 1px solid #e5e7eb; border-radius: 8px; }
.node-text-tooltip {
  position: absolute;
  z-index: 20;
  max-width: 280px;
  padding: 8px 12px;
  border: 1px solid #f59e0b;
  border-radius: 10px;
  background: #fef3c7;
  color: #92400e;
  font-size: 12px;
  line-height: 1.6;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
  white-space: pre-wrap;
  word-break: break-word;
  pointer-events: none;
}
.editor-hint { margin-top: 12px; color: #6b7280; font-size: 13px; }
</style>











