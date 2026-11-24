<script setup>
import { reactive, ref } from 'vue'
import { CARE, NEEDLE_TYPES, NEEDLE_SIZES } from '../catalog.js'
import PriceTag from './PriceTag.vue'

// `initial` is an existing material (edit), a copy without id (duplicate), or {} (new).
const props = defineProps({ initial: Object })
const emit = defineEmits(['save', 'cancel'])
const isNew = !props.initial.id
const f = reactive({
  name: '', color: '', composition: '', rollWidthCm: '', weightGsm: '', needleType: '', needleSize: '',
  certificate: '', wash: '', bleach: '', iron: '', dryClean: '', tumbleDry: '', link: '', pricePerMeter: '', discountPercent: '',
  store: '', purchaseDate: new Date().toISOString().slice(0, 10), lengthCm: '',
  // nulls -> '' so the empty select option shows
  ...Object.fromEntries(Object.entries(props.initial).map(([k, v]) => [k, v ?? ''])),
})
const photo = ref(null)
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
      <label>Composition<input v-model="f.composition" placeholder="100% cotton" /></label>
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
      <legend>Needle (Schmetz)</legend>
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
        <PriceTag :price="f.pricePerMeter" :discount="f.discountPercent || 0" unit=" /m" />
        <template v-if="f.lengthCm"><br />Total: {{ (f.pricePerMeter * (1 - (f.discountPercent || 0) / 100) * f.lengthCm / 100).toFixed(2) }}</template>
      </p>
      <label>Store<input v-model="f.store" /></label>
      <label>Date<input v-model="f.purchaseDate" type="date" /></label>
    </fieldset>

    <div class="actions">
      <button type="submit" class="primary">Save</button>
      <button type="button" @click="emit('cancel')">Cancel</button>
    </div>
  </form>
</template>
