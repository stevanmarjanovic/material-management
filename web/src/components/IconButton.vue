<script setup>
// Icon button: `label` is the hover tooltip and screen-reader name; optional `text` is shown next to the icon.
// Other attributes (type, disabled, class, @click) pass through to the <button>.
defineProps({ icon: String, label: String, text: String })
</script>

<template>
  <button class="icon-btn" :class="{ 'with-text': text }" :title="label" :aria-label="label">
    <span class="material-symbols-rounded" aria-hidden="true">{{ icon }}</span>
    <span v-if="text" aria-hidden="true">{{ text }}</span>
  </button>
</template>
