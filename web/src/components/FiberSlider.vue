<script setup>
import { computed, ref } from 'vue'
import { FIBERS, FIBER_COLORS } from '../catalog.js'

// One bar = 100 %. Each fiber is a segment; the dots between segments move the split (1 % steps).
const parts = defineModel({ type: Array, required: true }) // [{ fiber, percent }], always sums to 100

// Boundary k sits after segment k: cumulative percent.
const bounds = computed(() => parts.value.slice(0, -1).map((_, k) => parts.value.slice(0, k + 1).reduce((s, p) => s + p.percent, 0)))

function setBound(k, value) {
  const before = k === 0 ? 0 : bounds.value[k - 1]
  const after = k === bounds.value.length - 1 ? 100 : bounds.value[k + 1]
  const b = Math.min(after - 1, Math.max(before + 1, Math.round(value))) // every fiber keeps at least 1 %
  const next = parts.value.map((p) => ({ ...p }))
  next[k].percent = b - before
  next[k + 1].percent = after - b
  parts.value = next
}

const bar = ref(null)
let dragging = null
function down(k, e) {
  dragging = k
  e.target.setPointerCapture(e.pointerId)
}
function move(e) {
  if (dragging === null) return
  const r = bar.value.getBoundingClientRect()
  setBound(dragging, ((e.clientX - r.left) / r.width) * 100)
}
function key(k, e) {
  const step = { ArrowLeft: -1, ArrowDown: -1, ArrowRight: 1, ArrowUp: 1 }[e.key]
  if (!step) return
  e.preventDefault()
  setBound(k, bounds.value[k] + step)
}
</script>

<template>
  <div ref="bar" class="fiber-bar" @pointermove="move" @pointerup="dragging = null">
    <div
      v-for="p in parts" :key="p.fiber" class="segment"
      :style="{ width: `${p.percent}%`, background: FIBER_COLORS[p.fiber] }" :title="`${FIBERS[p.fiber]} ${p.percent}%`"
    />
    <span
      v-for="(b, k) in bounds" :key="k" class="dot" :style="{ left: `${b}%` }"
      role="slider" tabindex="0" :aria-valuenow="b" aria-valuemin="0" aria-valuemax="100"
      :aria-label="`Split between ${FIBERS[parts[k].fiber]} and ${FIBERS[parts[k + 1].fiber]}`"
      @pointerdown="down(k, $event)" @keydown="key(k, $event)"
    />
  </div>
</template>

<style scoped>
/* Thin line; the dots are larger than the line. Vertical padding keeps the dots inside the box. */
.fiber-bar { position: relative; display: flex; height: 6px; margin: .9rem .6rem; user-select: none; touch-action: none; }
.segment:first-child { border-radius: 3px 0 0 3px; }
.segment:last-of-type { border-radius: 0 3px 3px 0; }
.dot {
  position: absolute; top: 50%; width: 1.25rem; height: 1.25rem; transform: translate(-50%, -50%);
  background: var(--card); border: 2px solid var(--text); border-radius: 50%; cursor: ew-resize;
}
.dot:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
</style>
