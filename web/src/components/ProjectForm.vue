<script setup>
import { reactive, ref } from 'vue'
import { CARE, PROJECT_STATUS } from '../catalog.js'
import IconButton from './IconButton.vue'

const props = defineProps({ initial: Object })
const emit = defineEmits(['save', 'cancel'])
const f = reactive({
  name: '', status: 'Planned', startDate: new Date().toISOString().slice(0, 10), notes: '',
  wash: '', bleach: '', iron: '', dryClean: '', tumbleDry: '',
  ...Object.fromEntries(Object.entries(props.initial).map(([k, v]) => [k, v ?? ''])),
})
const photo = ref(null)
const calculated = props.initial.care ?? {} // strictest value from the project's materials

function submit() {
  const { id, name, status, startDate, notes } = f
  // '' = no override, use the calculated value
  const care = Object.fromEntries(Object.keys(CARE).map((k) => [k, f[k] || null]))
  emit('save', { id, name, status, startDate: startDate || null, notes: notes || null, ...care }, photo.value)
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
    <fieldset>
      <legend>Care for the finished product</legend>
      <label v-for="(c, key) in CARE" :key="key">{{ c.label }}
        <select v-model="f[key]">
          <option value="">Calculated{{ calculated[key] ? ` (${c.options[calculated[key]]})` : '' }}</option>
          <option v-for="(text, v) in c.options" :key="v" :value="v">{{ text }}</option>
        </select>
      </label>
    </fieldset>
    <div class="actions">
      <IconButton icon="save" label="Save" text="Save" type="submit" class="primary" />
      <IconButton icon="close" label="Cancel" text="Cancel" type="button" @click="emit('cancel')" />
    </div>
  </form>
</template>
