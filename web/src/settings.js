import { ref, watch } from 'vue'

// Display-only settings, kept in this browser (localStorage). Nothing is sent to the API.
function stored(key, fallback) {
  let initial = fallback
  try {
    initial = localStorage.getItem(key) || fallback
  } catch {} // storage blocked: use the default
  const value = ref(initial)
  watch(value, (v) => {
    try {
      localStorage.setItem(key, v)
    } catch {}
  })
  return value
}

// Shown next to prices only; amounts are never converted.
export const currency = stored('currency', 'RSD')
