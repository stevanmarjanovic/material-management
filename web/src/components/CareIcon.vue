<script setup>
import { computed } from 'vue'
import { CARE } from '../catalog.js'

// ISO 3758 symbols from Wikimedia Commons, see web/public/care/CREDITS.txt.
const props = defineProps({ category: String, value: String })
const title = computed(() => `${CARE[props.category].label}: ${CARE[props.category].options[props.value]}`)
</script>

<template>
  <img v-if="value" :src="`/care/${category}-${value}.svg`" :title="title" :alt="title" class="care" />
</template>

<style scoped>
.care { width: 40px; height: 40px; padding: 2px; }
/* The symbols are black SVGs; in dark mode show them white instead. */
@media (prefers-color-scheme: dark) {
  .care { filter: invert(1); }
}
</style>
