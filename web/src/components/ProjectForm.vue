<script setup>
import { reactive, ref } from 'vue'
import { PROJECT_STATUS } from '../catalog.js'

const props = defineProps({ initial: Object })
const emit = defineEmits(['save', 'cancel'])
const f = reactive({
  name: '', status: 'Planned', startDate: new Date().toISOString().slice(0, 10), notes: '',
  ...Object.fromEntries(Object.entries(props.initial).map(([k, v]) => [k, v ?? ''])),
})
const photo = ref(null)

function submit() {
  const { id, name, status, startDate, notes } = f
  emit('save', { id, name, status, startDate: startDate || null, notes: notes || null }, photo.value)
}
</script>

<template>
  <form class="card form" @submit.prevent="submit">
    <h2>{{ initial.id ? 'Edit project' : 'New project' }}</h2>
    <fieldset>
      <label>Name *<input v-model="f.name" required /></label>
      <label>Status
        <select v-model="f.status">
          <option v-for="(text, v) in PROJECT_STATUS" :key="v" :value="v">{{ text }}</option>
        </select>
      </label>
      <label>Start date<input v-model="f.startDate" type="date" /></label>
      <label>Photo<input type="file" accept="image/*" @change="photo = $event.target.files[0]" /></label>
      <label class="span">Notes<textarea v-model="f.notes" rows="4" /></label>
    </fieldset>
    <div class="actions">
      <button type="submit" class="primary">Save</button>
      <button type="button" @click="emit('cancel')">Cancel</button>
    </div>
  </form>
</template>
