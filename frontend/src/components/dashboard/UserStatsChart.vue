<template>
  <div ref="chartRef" class="chart-container"></div>
</template>

<script setup>
import * as echarts from 'echarts'
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'

const props = defineProps({
  stats: {
    type: Object,
    default: () => ({ fileCount: 0, llmCallCount: 0, scgCount: 0, mrCount: 0 })
  },
  labels: {
    type: Array,
    default: () => ['上传文件总数', 'LLM调用次数', 'SCG生成数量', 'MR生成次数']
  },
  title: {
    type: String,
    default: '系统统计总览'
  }
})

const chartRef = ref()
let chart = null

const buildScale = (values) => {
  const maxDataValue = Math.max(...values, 0)

  if (maxDataValue <= 5) {
    return { max: 5, interval: 1 }
  }

  const roughInterval = Math.ceil(maxDataValue / 6)
  const magnitude = 10 ** Math.floor(Math.log10(roughInterval))
  const normalized = roughInterval / magnitude

  const niceFactor = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10
  const interval = niceFactor * magnitude
  const max = Math.ceil(maxDataValue / interval) * interval

  return { max, interval }
}

const renderChart = () => {
  if (!chart) return

  const values = [props.stats.fileCount, props.stats.llmCallCount, props.stats.scgCount, props.stats.mrCount]
  const colors = ['#4f79b6', '#d2a04a', '#8f9cb1', '#4d9773']
  const { max, interval } = buildScale(values)

  chart.setOption({
    backgroundColor: 'transparent',
    title: {
      text: props.title,
      left: 'center',
      top: 8,
      textStyle: {
        color: '#18212f',
        fontSize: 18,
        fontWeight: 700
      }
    },
    grid: {
      left: 42,
      right: 24,
      top: 72,
      bottom: 52
    },
    xAxis: {
      type: 'category',
      data: props.labels,
      axisTick: { show: false },
      axisLine: { lineStyle: { color: '#d8e0ea' } },
      axisLabel: {
        color: '#435062',
        fontSize: 13,
        interval: 0
      }
    },
    yAxis: {
      type: 'value',
      min: 0,
      max,
      interval,
      axisLine: { show: false },
      axisTick: { show: false },
      axisLabel: { color: '#6e7a8c' },
      splitLine: { lineStyle: { color: '#e6ebf2' } }
    },
    series: [
      {
        type: 'bar',
        data: values.map((value, index) => ({ value, itemStyle: { color: colors[index], borderRadius: [10, 10, 0, 0] } })),
        barWidth: 58,
        label: {
          show: true,
          position: 'top',
          formatter: ({ value }) => `总量：${value}`,
          color: '#435062',
          fontSize: 13,
          fontWeight: 600,
          distance: 10
        }
      }
    ],
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' }
    }
  })
}

const resizeChart = () => {
  chart?.resize()
}

onMounted(() => {
  chart = echarts.init(chartRef.value)
  renderChart()
  window.addEventListener('resize', resizeChart)
})

watch(() => [props.stats, props.labels, props.title], () => renderChart(), { deep: true })

onBeforeUnmount(() => {
  window.removeEventListener('resize', resizeChart)
  chart?.dispose()
})
</script>

<style scoped>
.chart-container {
  width: 100%;
  height: 420px;
}
</style>

