<script setup>
import { computed, reactive, ref, watch } from 'vue'
import { CARE, FIBERS, NEEDLE_TYPES, NEEDLE_SIZES } from '../catalog.js'
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
const compositionSum = computed(() => f.composition.reduce((s, p) => s + (p.percent || 0), 0))
const compositionError = computed(() => {
  if (!f.composition.length) return ''
  if (new Set(f.composition.map((p) => p.fiber)).size !== f.composition.length) return 'Each fiber only once.'
  return Math.abs(compositionSum.value - 100) > 0.01 ? `Adds up to ${compositionSum.value}%, needs 100%.` : ''
})
const addFiber = () =>
  f.composition.push({ fiber: Object.keys(FIBERS).find((k) => !f.composition.some((p) => p.fiber === k)) ?? 'Other', percent: Math.max(0, 100 - compositionSum.value) })
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
      <div v-for="(p, i) in f.composition" :key="i" class="fiber-row">
        <select v-model="p.fiber" aria-label="Fiber">
          <option v-for="(text, v) in FIBERS" :key="v" :value="v">{{ text }}</option>
        </select>
        <input v-model.number="p.percent" type="number" step="0.1" min="0.1" max="100" aria-label="Percent" /> %
        <IconButton icon="remove" label="Remove fiber" type="button" @click="f.composition.splice(i, 1)" />
      </div>
      <div class="fiber-row">
        <IconButton icon="add" label="Add fiber" type="button" @click="addFiber" />
        <span v-if="compositionError" class="error-text">{{ compositionError }}</span>
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
      <label v-for="(c, key) in CARE" :key="key">{{ c.label }}
        <select v-model="f[key]">
          <option value="">–</option>
          <option v-for="(text, v) in c.options" :key="v" :value="v">{{ text }}</option>
        </select>
      </label>
    </fieldset>

    <fieldset>
      <legend>Purchase</legend>
      <label v-if="isNew">Bought length (cm) *<input v-model.number="f.lengthCm" type="number" step="0.1" min="0.1" required /></label>
      <label>Price per meter<input v-model.number="f.pricePerMeter" type="number" step="0.01" min="0" /></label>
      <label>Discount (%)<input v-model.number="f.discountPercent" type="number" step="0.1" min="0" max="100" /></label>
      <p v-if="f.pricePerMeter" class="muted">
        <PriceTag :price="f.pricePerMeter" :final="price?.finalPricePerMeter" :discount="f.discountPercent || 0" unit=" /m" />
        <template v-if="price?.totalPrice != null"><br />Total: {{ price.totalPrice.toFixed(2) }}</template>
      </p>
      <label>Store<input v-model="f.store" /></label>
      <label>Date<input v-model="f.purchaseDate" type="date" /></label>
    </fieldset>

    <div class="actions">
      <IconButton icon="check" label="Save" type="submit" class="primary" :disabled="!!compositionError" />
      <IconButton icon="close" label="Cancel" type="button" @click="emit('cancel')" />
    </div>
  </form>
</template>
