<script setup>
// Original price struck through, discounted price next to it, discount % on hover.
const props = defineProps({ price: Number, discount: Number, unit: { type: String, default: '' } })
const money = (v) => v.toFixed(2)
</script>

<template>
  <span v-if="discount" class="price" :title="`−${discount}% discount`">
    <s class="muted">{{ money(price) }}</s> <strong>{{ money(price * (1 - discount / 100)) }}</strong>{{ unit }}
  </span>
  <span v-else>{{ money(price) }}{{ unit }}</span>
</template>
