<script setup>
import { computed, onMounted, ref } from 'vue'
import { CARE, FIBERS, NEEDLE_TYPES, NEEDLE_SIZES, PROJECT_STATUS, formatComposition } from './catalog.js'
import CareIcon from './components/CareIcon.vue'
import IconButton from './components/IconButton.vue'
import MaterialForm from './components/MaterialForm.vue'
import ProjectForm from './components/ProjectForm.vue'
import PieceShape from './components/PieceShape.vue'
import PriceTag from './components/PriceTag.vue'
import { currency } from './settings.js'

const tab = ref('materials') // 'materials' | 'projects'
const materials = ref([])
const projects = ref([])
const selectedId = ref(null)
const projectId = ref(null)
const editing = ref(null) // object passed to MaterialForm / ProjectForm, or null
const pieceId = ref(null)
const cut = ref(null)
const cutLabel = ref('')
const cutProjectId = ref('') // kept between cuts: you usually cut several pieces for one project
const error = ref('')
const showSettings = ref(false)
const photoVersion = ref(Date.now()) // cache-buster after photo upload
const CUT_HELP = 'Drag a rectangle on the fabric (snaps to 0.5 cm), then fine-tune the numbers. Origin is the top-left corner.'
const FIBER_HELP = 'By weight: cut area × g/m² × fiber %'
const CARE_HELP = 'The most delicate fabric sets the limit: lowest wash temperature, gentlest ironing and drying. Dry cleaning that mixes wet cleaning (W) with solvents (P/F) becomes do not dry clean. Edit the project to override any value.'

const selected = computed(() => materials.value.find((m) => m.id === selectedId.value))
const project = computed(() => projects.value.find((p) => p.id === projectId.value))
const piece = computed(() => selected.value?.pieces.find((p) => p.id === pieceId.value))
const m2 = (cm2) => (cm2 / 10000).toFixed(2)
const money = (v) => (v == null ? '–' : v.toFixed(2))

async function api(method, url, body) {
  error.value = ''
  const isForm = body instanceof FormData
  const res = await fetch(`/api${url}`, {
    method,
    body: body && (isForm ? body : JSON.stringify(body)),
    headers: body && !isForm ? { 'Content-Type': 'application/json' } : {},
  })
  if (!res.ok) {
    error.value = (await res.text()) || `${res.status} ${res.statusText}`
    throw new Error(error.value)
  }
  return res.status === 204 ? null : res.json()
}

const loadMaterials = async () => (materials.value = await api('GET', '/materials'))
const loadProjects = async () => (projects.value = await api('GET', '/projects'))
function replace(list, item) {
  const i = list.value.findIndex((x) => x.id === item.id)
  if (i < 0) list.value.push(item)
  else list.value[i] = item
}

async function uploadPhoto(kind, id, photo) {
  const fd = new FormData()
  fd.append('file', photo)
  await api('PUT', `/${kind}/${id}/photo`, fd)
  photoVersion.value = Date.now()
}

// --- Materials ---

function select(id) {
  showSettings.value = false
  tab.value = 'materials'
  selectedId.value = id
  editing.value = null
  pieceId.value = null
  cut.value = null
}

async function saveMaterial(body, photo) {
  let m = body.id ? await api('PUT', `/materials/${body.id}`, body) : await api('POST', '/materials', body)
  if (photo) {
    await uploadPhoto('materials', m.id, photo)
    m = await api('GET', `/materials/${m.id}`) // the server sets the preview color from the photo
  }
  replace(materials, m)
  select(m.id)
  loadProjects() // costs may have changed
}

function duplicate() {
  const { id, pieces, cuts, hasPhoto, ...props } = selected.value
  editing.value = { ...props, name: `${props.name} (copy)` }
}

async function removeMaterial() {
  if (!confirm(`Delete "${selected.value.name}" with all its pieces and cut history?`)) return
  await api('DELETE', `/materials/${selectedId.value}`)
  materials.value = materials.value.filter((m) => m.id !== selectedId.value)
  select(null)
  loadProjects()
}

async function saveCut() {
  const body = { ...cut.value, label: cutLabel.value || null, projectId: cutProjectId.value || null }
  const m = await api('POST', `/pieces/${pieceId.value}/cuts`, body)
  replace(materials, m)
  cut.value = null
  cutLabel.value = ''
  pieceId.value = m.pieces.length === 1 ? m.pieces[0].id : null
  loadProjects()
}

async function undoCut() {
  replace(materials, await api('DELETE', `/materials/${selectedId.value}/cuts/last`))
  pieceId.value = null
  cut.value = null
  loadProjects()
}

// --- Projects ---

function selectProject(id) {
  showSettings.value = false
  tab.value = 'projects'
  projectId.value = id
  editing.value = null
}

async function saveProject(body, photo) {
  const p = body.id ? await api('PUT', `/projects/${body.id}`, body) : await api('POST', '/projects', body)
  if (photo) {
    await uploadPhoto('projects', p.id, photo)
    p.hasPhoto = true
  }
  replace(projects, p)
  selectProject(p.id)
  loadMaterials() // project names show in cut logs
}

async function removeProject() {
  if (!confirm(`Delete project "${project.value.name}"? Its cuts stay, without a project.`)) return
  await api('DELETE', `/projects/${projectId.value}`)
  projects.value = projects.value.filter((p) => p.id !== projectId.value)
  if (cutProjectId.value === projectId.value) cutProjectId.value = ''
  selectProject(null)
  loadMaterials()
}

onMounted(() => Promise.all([loadMaterials(), loadProjects()]))
</script>

<template>
  <div class="layout">
    <aside>
      <div class="tabs">
        <button :class="{ active: tab === 'materials' }" @click="tab = 'materials'; editing = null; showSettings = false">Materials</button>
        <button :class="{ active: tab === 'projects' }" @click="tab = 'projects'; editing = null; showSettings = false">Projects</button>
      </div>

      <template v-if="tab === 'materials'">
        <ul>
          <li v-for="m in materials" :key="m.id" :class="{ active: m.id === selectedId }" @click="select(m.id)">
            <img v-if="m.hasPhoto" :src="`/api/materials/${m.id}/photo?v=${photoVersion}`" alt="" class="thumb" />
            <span v-else class="thumb" :style="{ background: m.previewColor }" />
            <span>
              <strong>{{ m.name }}</strong><br />
              <small>{{ m.color }}</small>
            </span>
          </li>
        </ul>
        <p v-if="!materials.length" class="muted">No materials yet.</p>
      </template>

      <template v-else>
        <ul>
          <li v-for="p in projects" :key="p.id" :class="{ active: p.id === projectId }" @click="selectProject(p.id)">
            <img v-if="p.hasPhoto" :src="`/api/projects/${p.id}/photo?v=${photoVersion}`" alt="" class="thumb" />
            <span v-else class="thumb" />
            <span>
              <strong>{{ p.name }}</strong><br />
              <small>{{ PROJECT_STATUS[p.status] }}<template v-if="p.startDate"> · {{ p.startDate }}</template></small>
            </span>
          </li>
        </ul>
        <p v-if="!projects.length" class="muted">No projects yet.</p>
      </template>

      <div class="aside-footer">
        <IconButton icon="settings" label="Settings" :class="{ active: showSettings }" @click="showSettings = true" />
        <IconButton v-if="tab === 'materials'" icon="add" label="New material" text="New" class="primary" @click="select(null); editing = {}" />
        <IconButton v-else icon="add" label="New project" text="New" class="primary" @click="selectProject(null); editing = {}" />
      </div>
    </aside>

    <main>
      <p v-if="error" class="error">{{ error }}</p>

      <!-- ===== Settings (this browser only) ===== -->
      <section v-if="showSettings" class="card form">
        <h2>Settings</h2>
        <fieldset>
          <label>Currency<input v-model.trim="currency" maxlength="10" placeholder="RSD" /></label>
        </fieldset>
        <p class="muted">Saved in this browser only. It's just the label next to prices; amounts are not converted.</p>
      </section>

      <!-- ===== Materials ===== -->
      <template v-else-if="tab === 'materials'">
        <MaterialForm v-if="editing" :key="editing.id ?? 'new'" :initial="editing" @save="saveMaterial" @cancel="editing = null" />

        <template v-else-if="selected">
          <section class="card head">
            <img v-if="selected.hasPhoto" :src="`/api/materials/${selected.id}/photo?v=${photoVersion}`" alt="" class="photo" />
            <div class="grow">
              <h2>
                <a v-if="selected.link" :href="selected.link" target="_blank" rel="noopener" title="Open product page in new tab">{{ selected.name }}</a>
                <template v-else>{{ selected.name }}</template>
              </h2>
              <dl>
                <template v-for="[k, v] in [
                  ['Color', selected.color], ['Composition', formatComposition(selected.composition)],
                  ['Bought', `${selected.lengthCm} cm × ${selected.rollWidthCm} cm roll`],
                  ['Weight', selected.weightGsm && `${selected.weightGsm} g/m²`], ['Certificate', selected.certificate],
                  ['Store', selected.store], ['Purchased', selected.purchaseDate],
                ]" :key="k">
                  <template v-if="v"><dt>{{ k }}</dt><dd>{{ v }}</dd></template>
                </template>
                <template v-if="selected.pricePerMeter != null">
                  <dt>Price</dt>
                  <dd><PriceTag :price="selected.pricePerMeter" :final="selected.finalPricePerMeter" :discount="selected.discountPercent" :unit="` ${currency}/m`" /></dd>
                  <dt>Total</dt>
                  <dd>{{ money(selected.totalPrice) }} {{ currency }}</dd>
                </template>
                <template v-if="selected.needleType || selected.needleSize">
                  <dt>Needle</dt>
                  <dd class="needle">
                    <span class="band" :style="{ background: NEEDLE_TYPES[selected.needleType] ?? 'transparent' }" title="type" />
                    <span class="band" :style="{ background: NEEDLE_SIZES[selected.needleSize]?.[1] ?? 'transparent' }" title="size" />
                    {{ selected.needleType }} {{ selected.needleSize }}<template v-if="selected.needleSize">/{{ NEEDLE_SIZES[selected.needleSize]?.[0] }}</template>
                  </dd>
                </template>
              </dl>
              <div class="care-row">
                <template v-for="(_, key) in CARE" :key="key">
                  <CareIcon v-if="selected[key]" :category="key" :value="selected[key]" />
                </template>
              </div>
            </div>
            <div class="actions col">
              <IconButton icon="edit" label="Edit" @click="editing = { ...selected }" />
              <IconButton icon="content_copy" label="Duplicate" @click="duplicate" />
              <IconButton icon="delete" label="Delete" class="danger" @click="removeMaterial" />
            </div>
          </section>

          <section class="card">
            <h3>Pieces — {{ m2(selected.remainingArea) }} m² left</h3>
            <p v-if="!selected.pieces.length" class="muted">Everything is used up.</p>
            <div class="pieces">
              <button
                v-for="p in selected.pieces" :key="p.id" class="piece-btn" :class="{ active: p.id === pieceId }"
                @click="pieceId = p.id; cut = null"
              >
                <PieceShape :shape="p.shape" :color="selected.previewColor" />
                <small>{{ m2(p.area) }} m²</small>
              </button>
            </div>
          </section>

          <section v-if="piece" class="card">
            <h3>
              Cut from piece #{{ piece.id }}
              <span class="info material-symbols-rounded" role="img" tabindex="0" :title="CUT_HELP" :aria-label="CUT_HELP">help</span>
            </h3>
            <div class="cut-layout">
              <div class="grow"><PieceShape v-model:cut="cut" :shape="piece.shape" :color="selected.previewColor" editable /></div>
              <form class="cut-form" @submit.prevent="saveCut">
                <template v-if="cut">
                  <label>X (cm)<input v-model.number="cut.x" type="number" step="0.1" min="0" /></label>
                  <label>Y (cm)<input v-model.number="cut.y" type="number" step="0.1" min="0" /></label>
                  <label>Width (cm)<input v-model.number="cut.w" type="number" step="0.1" min="0.1" /></label>
                  <label>Length (cm)<input v-model.number="cut.h" type="number" step="0.1" min="0.1" /></label>
                  <label>Project
                    <select v-model="cutProjectId">
                      <option value="">— no project —</option>
                      <option v-for="p in projects" :key="p.id" :value="p.id">{{ p.name }}</option>
                    </select>
                  </label>
                  <label>Label<input v-model="cutLabel" placeholder="e.g. skirt front" /></label>
                  <div class="actions halves">
                    <IconButton icon="content_cut" label="Save cut" text="Cut" type="submit" class="primary" :disabled="!(cut.w > 0 && cut.h > 0)" />
                    <IconButton icon="close" label="Cancel cut" text="Cancel" type="button" @click="cut = null" />
                  </div>
                </template>
                <IconButton v-else icon="keyboard" label="Enter numbers instead" text="Enter manually" type="button" @click="cut = { x: 0, y: 0, w: 0, h: 0 }" />
              </form>
            </div>
          </section>

          <section class="card">
            <h3>Cut log</h3>
            <p v-if="!selected.cuts.length" class="muted">No cuts yet.</p>
            <table v-else>
              <tr><th>Date</th><th>Project</th><th>Label</th><th>Size (cm)</th><th class="num">m²</th><th class="num">Price ({{ currency }})</th><th /></tr>
              <tr v-for="(c, i) in selected.cuts" :key="c.id">
                <td>{{ new Date(c.createdAt).toLocaleString() }}</td>
                <td><a v-if="c.projectId" href="#" @click.prevent="selectProject(c.projectId)">{{ c.projectName }}</a></td>
                <td>{{ c.label }}</td>
                <td>{{ c.w }} × {{ c.h }} at {{ c.x }}, {{ c.y }}</td>
                <td class="num">{{ m2(c.area) }}</td>
                <td class="num">{{ money(c.cost) }}</td>
                <td class="action"><IconButton v-if="i === 0" icon="undo" label="Undo cut" class="plain" @click="undoCut" /></td>
              </tr>
            </table>
          </section>
        </template>

        <p v-else class="muted">Select a material or add a new one.</p>
      </template>

      <!-- ===== Projects ===== -->
      <template v-else>
        <ProjectForm v-if="editing" :key="editing.id ?? 'new'" :initial="editing" @save="saveProject" @cancel="editing = null" />

        <template v-else-if="project">
          <section class="card head">
            <img v-if="project.hasPhoto" :src="`/api/projects/${project.id}/photo?v=${photoVersion}`" alt="" class="photo" />
            <div class="grow">
              <h2>{{ project.name }}</h2>
              <dl>
                <dt>Status</dt><dd>{{ PROJECT_STATUS[project.status] }}</dd>
                <template v-if="project.startDate"><dt>Started</dt><dd>{{ project.startDate }}</dd></template>
              </dl>
              <p v-if="project.notes" class="notes">{{ project.notes }}</p>
            </div>
            <div class="actions col">
              <IconButton icon="edit" label="Edit" @click="editing = { ...project }" />
              <IconButton icon="delete" label="Delete" class="danger" @click="removeProject" />
            </div>
          </section>

          <section class="card">
            <h3>Materials used</h3>
            <p v-if="!project.cuts.length" class="muted">No cuts yet. Cut a piece under Materials and pick this project.</p>
            <table v-else>
              <tr><th>Material</th><th class="num">Pieces</th><th class="num">m²</th><th class="num">Price ({{ currency }})</th></tr>
              <tr v-for="r in project.materials" :key="r.materialId">
                <td><a href="#" @click.prevent="select(r.materialId)">{{ r.name }}</a> <small class="muted">{{ r.color }}</small></td>
                <td class="num">{{ r.pieces }}</td>
                <td class="num">{{ m2(r.area) }}</td>
                <td class="num">{{ money(r.cost) }}</td>
              </tr>
              <tr class="total">
                <td>Total</td>
                <td class="num">{{ project.totals.pieces }}</td>
                <td class="num">{{ m2(project.totals.area) }}</td>
                <td class="num">{{ money(project.totals.cost) }}</td>
              </tr>
            </table>
          </section>

          <section v-if="project.cuts.length || Object.keys(CARE).some((k) => project[k])" class="card">
            <h3>
              Care for the finished product
              <span class="info material-symbols-rounded" role="img" tabindex="0" :title="CARE_HELP" :aria-label="CARE_HELP">help</span>
            </h3>
            <div class="care-list">
              <template v-for="(c, key) in CARE" :key="key">
                <div v-if="project[key] || project.care[key]" class="care-item">
                  <CareIcon :category="key" :value="project[key] || project.care[key]" />
                  <span>{{ c.options[project[key] || project.care[key]] }}</span>
                  <small
                    v-if="project[key] && project[key] !== project.care[key]" class="muted"
                    :title="`Calculated: ${project.care[key] ? c.options[project.care[key]] : 'not set'}`"
                  >(edited)</small>
                </div>
              </template>
            </div>
            <p v-for="(names, key) in project.care.missing" :key="key" class="muted">
              {{ CARE[key].label }} not set for: {{ names.join(', ') }}
            </p>
          </section>

          <section v-if="project.fibers.length" class="card">
            <h3>
              Fiber composition
              <span class="info material-symbols-rounded" role="img" tabindex="0" :title="FIBER_HELP" :aria-label="FIBER_HELP">help</span>
            </h3>
            <table>
              <tr><th>Fiber</th><th class="num">Grams</th><th class="num">%</th></tr>
              <tr v-for="fb in project.fibers" :key="fb.fiber">
                <td>{{ FIBERS[fb.fiber] }}</td>
                <td class="num">{{ fb.grams.toFixed(1) }}</td>
                <td class="num">{{ fb.percent.toFixed(1) }}</td>
              </tr>
            </table>
            <p v-if="project.uncountedCuts" class="muted">
              {{ project.uncountedCuts }} cut(s) not counted: their material has no g/m² or no composition.
            </p>
          </section>

          <section v-if="project.cuts.length" class="card">
            <h3>Cut pieces</h3>
            <table>
              <tr><th>Material</th><th>Label</th><th>Size (cm)</th><th class="num">m²</th><th class="num">Price ({{ currency }})</th></tr>
              <tr v-for="c in project.cuts" :key="c.id">
                <td>{{ c.materialName }}</td>
                <td>{{ c.label }}</td>
                <td>{{ c.w }} × {{ c.h }}</td>
                <td class="num">{{ m2(c.area) }}</td>
                <td class="num">{{ money(c.cost) }}</td>
              </tr>
            </table>
          </section>
        </template>

        <p v-else class="muted">Select a project or add a new one.</p>
      </template>
    </main>
  </div>
</template>
