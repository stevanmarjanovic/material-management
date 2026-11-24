<script setup>
// Original price struck through, discounted price (calculated by the API) next to it, discount % on hover.
defineProps({ price: Number, final: Number, discount: Number, unit: { type: String, default: '' } })
const money = (v) => v.toFixed(2)
</script>

<template>
  <span v-if="discount && final != null" class="price" :title="`−${discount}% discount`">
    <s class="muted">{{ money(price) }}</s> <strong>{{ money(final) }}</strong>{{ unit }}
  </span>
  <span v-else>{{ money(price) }}{{ unit }}</span>
</template>
