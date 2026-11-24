<script setup>
import { reactive, ref, watch } from 'vue'
import { currency } from '../settings.js'
import IconButton from './IconButton.vue'

// One purchases-table row in edit mode. `initial` is an existing purchase (edit) or {} (new).
// Once a purchase is cut, its length is fixed.
const props = defineProps({ initial: Object })
const emit = defineEmits(['save', 'cancel'])
const f = reactive({
  lengthCm: '', pricePerMeter: '', discountPercent: '', purchaseDate: new Date().toISOString().slice(0, 10), storePurchaseId: '',
  ...Object.fromEntries(Object.entries(props.initial).map(([k, v]) => [k, v ?? ''])),
})
const error = ref('')

// Live total, calculated by the API (same formula as a saved purchase).
const total = ref(null)
let timer
watch(
  () => [f.pricePerMeter, f.discountPercent, f.lengthCm],
  ([pricePerMeter, discountPercent, lengthCm]) => {
    clearTimeout(timer)
    if (pricePerMeter === '' || pricePerMeter < 0 || !(lengthCm > 0)) return (total.value = null)
    timer = setTimeout(async () => {
      const res = await fetch(`/api/prices?${new URLSearchParams({ pricePerMeter, discountPercent: discountPercent || 0, lengthCm })}`)
      total.value = res.ok ? (await res.json()).totalPrice : null
    }, 250)
  },
  { immediate: true },
)

function save() {
  if (!(f.lengthCm > 0)) return (error.value = 'Length is required.')
  const orNull = (v) => (v === '' ? null : v)
  const { id, lengthCm, pricePerMeter, discountPercent, purchaseDate, storePurchaseId } = f
  emit('save', {
    id, lengthCm, pricePerMeter: orNull(pricePerMeter), discountPercent: orNull(discountPercent),
    purchaseDate: orNull(purchaseDate), storePurchaseId: orNull(storePurchaseId),
  })
}
</script>

<template>
  <tr class="edit-row" @keydown.enter.prevent="save" @keydown.esc="emit('cancel')">
    <td><input v-model="f.purchaseDate" type="date" aria-label="Date" /></td>
    <td class="num">
      <input v-model.number="f.lengthCm" type="number" step="0.1" min="0.1" aria-label="Length (cm)" :disabled="initial.cut"
        :title="initial.cut ? 'Locked after the first cut' : error" :class="{ invalid: error && !(f.lengthCm > 0) }" />
    </td>
    <td class="num"><input v-model.number="f.pricePerMeter" type="number" step="0.01" min="0" :aria-label="`Price (${currency}/m)`" /></td>
    <td class="num"><input v-model.number="f.discountPercent" type="number" step="0.1" min="0" max="100" aria-label="Discount (%)" /></td>
    <td class="num muted">{{ total != null ? total.toFixed(2) : '' }}</td>
    <td><input v-model.trim="f.storePurchaseId" aria-label="Purchase ID" placeholder="Order no." /></td>
    <td class="action">
      <div class="actions">
        <IconButton icon="save" label="Save purchase" class="plain" @click="save" />
        <IconButton icon="close" label="Cancel" class="plain" @click="emit('cancel')" />
      </div>
    </td>
  </tr>
</template>
