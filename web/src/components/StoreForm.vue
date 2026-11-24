<script setup>
import { reactive } from 'vue'
import IconButton from './IconButton.vue'

const props = defineProps({ initial: Object })
const emit = defineEmits(['save', 'cancel'])
const f = reactive({
  name: '', address: '', website: '', workingHours: '', purchaseUrlTemplate: '',
  ...Object.fromEntries(Object.entries(props.initial).map(([k, v]) => [k, v ?? ''])),
})

function submit() {
  const { id, name, address, website, workingHours, purchaseUrlTemplate } = f
  emit('save', { id, name, address, website, workingHours, purchaseUrlTemplate })
}
</script>

<template>
  <form class="card form" @submit.prevent="submit">
    <h2>{{ initial.id ? 'Edit store' : 'New store' }}</h2>
    <fieldset>
      <label>Name *<input v-model="f.name" required /></label>
      <label>Address<input v-model="f.address" /></label>
      <label>Website<input v-model="f.website" type="url" placeholder="https://…" /></label>
      <label class="span">Working hours<textarea v-model="f.workingHours" rows="2" placeholder="Mon–Fri 9–17, Sat 9–14" /></label>
      <label class="span">Purchase link
        <input v-model="f.purchaseUrlTemplate" placeholder="https://shop.rs/orders/{id}" />
        <small class="muted">Put <code>{id}</code> where a purchase's ID goes; each purchase with an ID then links to its page.</small>
      </label>
    </fieldset>
    <div class="actions">
      <IconButton icon="save" label="Save" text="Save" type="submit" class="primary" />
      <IconButton icon="close" label="Cancel" text="Cancel" type="button" @click="emit('cancel')" />
    </div>
  </form>
</template>
