<script setup>
import { computed, ref } from 'vue'

// Draws one piece (a set of rectangles, cm). With `editable`, a rectangle can be dragged and is v-model'd as `cut`.
const props = defineProps({ shape: Array, editable: Boolean, color: String })
const cut = defineModel('cut', { default: null })

const SNAP = 0.5 // cm
const width = computed(() => Math.max(...props.shape.map((r) => r.x + r.w)))
const length = computed(() => Math.max(...props.shape.map((r) => r.y + r.h)))
const pad = computed(() => Math.max(width.value, length.value) * 0.08)
const viewBox = computed(() => `${-pad.value} ${-pad.value} ${width.value + 2 * pad.value} ${length.value + 2 * pad.value}`)
const font = computed(() => Math.max(width.value, length.value) * 0.045)

// Outline of the union: compress coordinates into a grid, then draw every edge between a filled and an empty cell.
const outline = computed(() => {
  const xs = [...new Set(props.shape.flatMap((r) => [r.x, r.x + r.w]))].sort((a, b) => a - b)
  const ys = [...new Set(props.shape.flatMap((r) => [r.y, r.y + r.h]))].sort((a, b) => a - b)
  const filled = (i, j) => {
    if (i < 0 || j < 0 || i >= xs.length - 1 || j >= ys.length - 1) return false
    const cx = (xs[i] + xs[i + 1]) / 2, cy = (ys[j] + ys[j + 1]) / 2
    return props.shape.some((r) => cx > r.x && cx < r.x + r.w && cy > r.y && cy < r.y + r.h)
  }
  let d = ''
  for (let i = 0; i < xs.length - 1; i++)
    for (let j = 0; j < ys.length - 1; j++) {
      if (!filled(i, j)) continue
      if (!filled(i - 1, j)) d += `M${xs[i]} ${ys[j]}V${ys[j + 1]}`
      if (!filled(i + 1, j)) d += `M${xs[i + 1]} ${ys[j]}V${ys[j + 1]}`
      if (!filled(i, j - 1)) d += `M${xs[i]} ${ys[j]}H${xs[i + 1]}`
      if (!filled(i, j + 1)) d += `M${xs[i]} ${ys[j + 1]}H${xs[i + 1]}`
    }
  return d
})

const grid = computed(() => {
  let d = ''
  for (let x = 10; x < width.value; x += 10) d += `M${x} 0V${length.value}`
  for (let y = 10; y < length.value; y += 10) d += `M0 ${y}H${width.value}`
  return d
})

const svg = ref(null)
let start = null
const snap = (v, max) => Math.min(max, Math.max(0, Math.round(v / SNAP) * SNAP))
function toCm(e) {
  const p = svg.value.createSVGPoint()
  p.x = e.clientX
  p.y = e.clientY
  const q = p.matrixTransform(svg.value.getScreenCTM().inverse())
  return { x: snap(q.x, width.value), y: snap(q.y, length.value) }
}
function down(e) {
  if (!props.editable) return
  start = toCm(e)
  svg.value.setPointerCapture(e.pointerId)
  cut.value = { x: start.x, y: start.y, w: 0, h: 0 }
}
function move(e) {
  if (!start) return
  const p = toCm(e)
  cut.value = { x: Math.min(start.x, p.x), y: Math.min(start.y, p.y), w: Math.abs(p.x - start.x), h: Math.abs(p.y - start.y) }
}
function up() {
  start = null
}
</script>

<template>
  <svg ref="svg" :viewBox="viewBox" class="piece" :class="{ editable }" @pointerdown="down" @pointermove="move" @pointerup="up">
    <rect v-for="(r, i) in shape" :key="i" :x="r.x" :y="r.y" :width="r.w" :height="r.h" :fill="color || '#d9c7a7'" />
    <path v-if="editable" :d="grid" stroke="#0002" :stroke-width="font * 0.05" fill="none" />
    <path :d="outline" stroke="#333" :stroke-width="font * 0.12" fill="none" stroke-linecap="square" />
    <text :x="width / 2" :y="-pad * 0.3" :font-size="font" text-anchor="middle">{{ width }} cm</text>
    <text :x="-pad * 0.3" :y="length / 2" :font-size="font" text-anchor="middle" :transform="`rotate(-90 ${-pad * 0.3} ${length / 2})`">{{ length }} cm</text>
    <rect
      v-if="cut && cut.w > 0 && cut.h > 0"
      :x="cut.x" :y="cut.y" :width="cut.w" :height="cut.h"
      fill="#e5393540" stroke="#e53935" :stroke-width="font * 0.1" :stroke-dasharray="font * 0.4"
    />
  </svg>
</template>

<style scoped>
.piece { width: 100%; max-height: 70vh; display: block; touch-action: none; user-select: none; }
.piece.editable { cursor: crosshair; }
text { fill: currentColor; }
</style>
