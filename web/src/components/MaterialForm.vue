<script setup>
import { reactive, ref, watch } from 'vue'
import { CARE, FIBERS, FIBER_COLORS, NEEDLE_TYPES, NEEDLE_SIZES } from '../catalog.js'
import FiberSlider from './FiberSlider.vue'
import CareIcon from './CareIcon.vue'
import { currency } from '../settings.js'
import PriceTag from './PriceTag.vue'
import IconButton from './IconButton.vue'

// `initial` is an existing material (edit), a copy without id (duplicate), or {} (new).
const props = defineProps({ initial: Object })
const emit = defineEmits(['save', 'cancel'])
const isNew = !props.initial.id
const f = reactive({
  name: '', color: '', rollWidthCm: '', weightGsm: '', needleType: '', needleSize: '',
  certificate: '', wash: '', bleach: '', iron: '', dryClean: '', tumbleDry: '', link: '', pricePerMeter: '', discountPercent: '',
  store: '', purchaseDate: new Date().toISOString().slice(0, 10), lengthCm: '',
  // nulls -> '' so the empty select option shows
  ...Object.fromEntries(Object.entries(props.initial).map(([k, v]) => [k, v ?? ''])),
  composition: (props.initial.composition ?? []).map((p) => ({ ...p })), // copy, so Cancel leaves the material untouched
})
// Composition always adds up to 100 %: a newly checked fiber takes half of the largest share,
// an unchecked fiber's share goes to the largest remaining one.
const FIBER_ORDER = Object.keys(FIBERS)
const share = (fiber) => f.composition.find((p) => p.fiber === fiber)?.percent
const largest = () => f.composition.reduce((a, b) => (b.percent > a.percent ? b : a))
function toggleFiber(fiber) {
  const i = f.composition.findIndex((p) => p.fiber === fiber)
  if (i >= 0) {
    const [gone] = f.composition.splice(i, 1)
    if (f.composition.length) largest().percent += gone.percent
    return
  }
  let percent = 100
  if (f.composition.length) {
    const big = largest()
    if (big.percent < 2) return // nothing left to split
    percent = Math.floor(big.percent / 2)
    big.percent -= percent
  }
  f.composition.push({ fiber, percent })
  f.composition.sort((a, b) => FIBER_ORDER.indexOf(a.fiber) - FIBER_ORDER.indexOf(b.fiber))
}
const photo = ref(null)

// Live price preview, calculated by the API (same formula as a saved material).
const price = ref(null) // { finalPricePerMeter, totalPrice }
let priceTimer
watch(
  () => [f.pricePerMeter, f.discountPercent, f.lengthCm],
  ([pricePerMeter, discountPercent, lengthCm]) => {
    clearTimeout(priceTimer)
    if (pricePerMeter === '' || pricePerMeter < 0) return (price.value = null)
    priceTimer = setTimeout(async () => {
      const q = new URLSearchParams({ pricePerMeter, discountPercent: discountPercent || 0, lengthCm: lengthCm || 0 })
      const res = await fetch(`/api/prices?${q}`)
      price.value = res.ok ? await res.json() : null
    }, 250)
  },
  { immediate: true },
)
const widthLocked = !!props.initial.cuts?.length // first piece is already cut

function submit() {
  // Empty strings -> null so enums and numbers deserialize on the server.
  const body = Object.fromEntries(Object.entries(f).map(([k, v]) => [k, v === '' ? null : v]))
  emit('save', body, photo.value)
}
</script>

<template>
  <form class="card form" @submit.prevent="submit">
    <h2>{{ isNew ? 'New material' : 'Edit material' }}</h2>

    <fieldset>
      <legend>Fabric</legend>
      <label>Name *<input v-model="f.name" required /></label>
      <label>Color<input v-model="f.color" /></label>
      <label>Roll width (cm) *
        <input v-model.number="f.rollWidthCm" type="number" step="0.1" min="0.1" required :disabled="widthLocked"
          :title="widthLocked ? 'Locked after the first cut' : ''" />
      </label>
      <label>Weight (g/m²)<input v-model.number="f.weightGsm" type="number" min="0" /></label>
      <label>Certificate<input v-model="f.certificate" placeholder="OEKO-TEX STANDARD 100" /></label>
      <label>Link<input v-model="f.link" type="url" /></label>
      <label>Photo<input type="file" accept="image/*" @change="photo = $event.target.files[0]" /></label>
    </fieldset>

    <fieldset>
      <legend>Composition</legend>
      <div class="composition">
        <!-- Left: fibers not in this fabric yet -->
        <ul class="fiber-list">
          <template v-for="(text, v) in FIBERS" :key="v">
            <li v-if="share(v) == null">
              {{ text }}
              <IconButton icon="add" :label="`Add ${text}`" type="button" @click="toggleFiber(v)" />
            </li>
          </template>
        </ul>
        <!-- Right: the ratio slider and the fibers in this fabric -->
        <div>
          <p v-if="!f.composition.length" class="muted">Add the fibers this fabric is made of.</p>
          <FiberSlider v-else v-model="f.composition" />
          <ul class="fiber-list">
            <li v-for="p in f.composition" :key="p.fiber">
              <span class="swatch" :style="{ background: FIBER_COLORS[p.fiber] }" />
              {{ FIBERS[p.fiber] }}
              <strong>{{ p.percent }}%</strong>
              <IconButton icon="remove" :label="`Remove ${FIBERS[p.fiber]}`" type="button" @click="toggleFiber(p.fiber)" />
            </li>
          </ul>
        </div>
      </div>
    </fieldset>

    <fieldset>
      <legend>Needle</legend>
      <label>Type
        <select v-model="f.needleType">
          <option value="">–</option>
          <option v-for="(_, t) in NEEDLE_TYPES" :key="t">{{ t }}</option>
        </select>
      </label>
      <label>Size
        <select v-model="f.needleSize">
          <option value="">–</option>
          <option v-for="(s, nm) in NEEDLE_SIZES" :key="nm" :value="+nm">{{ nm }}/{{ s[0] }}</option>
        </select>
      </label>
    </fieldset>

    <fieldset>
      <legend>Care</legend>
      <!-- Every symbol of a category; the selected one is black, the rest greyed out. Click it again to clear. -->
      <div v-for="(c, key) in CARE" :key="key" class="care-pick" role="group" :aria-label="c.label">
        <span class="care-pick-label">{{ c.label }}</span>
        <button
          v-for="(text, v) in c.options" :key="v" type="button" class="care-option" :class="{ selected: f[key] === v }"
          :aria-pressed="f[key] === v" :aria-label="text" @click="f[key] = f[key] === v ? '' : v"
        >
          <CareIcon :category="key" :value="v" />
        </button>
      </div>
    </fieldset>

    <fieldset>
      <legend>Purchase</legend>
      <label v-if="isNew">Bought length (cm) *<input v-model.number="f.lengthCm" type="number" step="0.1" min="0.1" required /></label>
      <label>Price per meter ({{ currency }})<input v-model.number="f.pricePerMeter" type="number" step="0.01" min="0" /></label>
      <label>Discount (%)<input v-model.number="f.discountPercent" type="number" step="0.1" min="0" max="100" /></label>
      <p v-if="f.pricePerMeter" class="muted">
        <PriceTag :price="f.pricePerMeter" :final="price?.finalPricePerMeter" :discount="f.discountPercent || 0" :unit="` ${currency}/m`" />
        <template v-if="price?.totalPrice != null"><br />Total: {{ price.totalPrice.toFixed(2) }} {{ currency }}</template>
      </p>
      <label>Store<input v-model="f.store" /></label>
      <label>Date<input v-model="f.purchaseDate" type="date" /></label>
    </fieldset>

    <div class="actions">
      <IconButton icon="check" label="Save" type="submit" class="primary" />
      <IconButton icon="close" label="Cancel" type="button" @click="emit('cancel')" />
    </div>
  </form>
</template>
