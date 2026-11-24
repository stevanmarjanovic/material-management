<script setup>
// Icon-only button: `label` becomes the hover tooltip and the screen-reader name.
// Other attributes (type, disabled, class, @click) pass through to the <button>.
defineProps({ icon: String, label: String })
</script>

<template>
  <button class="icon-btn" :title="label" :aria-label="label">
    <span class="material-symbols-rounded" aria-hidden="true">{{ icon }}</span>
  </button>
</template>
