import { test } from 'node:test'
import assert from 'node:assert/strict'
import { fitCut, nearestOnFabric } from './fitCut.js'

// L-shape like the bug report: 40 wide x 100 long, top-left 15 x 30 cut away.
const L = [
  { x: 15, y: 0, w: 25, h: 30 },
  { x: 0, y: 30, w: 40, h: 70 },
]

test('drag inside a plain area is unchanged', () => {
  assert.deepEqual(fitCut(L, { x: 20, y: 5 }, { x: 30, y: 20 }), { x: 20, y: 5, w: 10, h: 15 })
})

test('dragging into the cut-out corner stops at its edge', () => {
  // from inside the top part, dragging left/down into the missing corner -> x stops at 15
  assert.deepEqual(fitCut(L, { x: 30, y: 5 }, { x: 5, y: 20 }), { x: 15, y: 5, w: 15, h: 15 })
})

test('dragging below the corner can use the full width', () => {
  assert.deepEqual(fitCut(L, { x: 30, y: 40 }, { x: 5, y: 60 }), { x: 5, y: 40, w: 25, h: 20 })
})

test('a start in the cut-out corner moves onto the fabric', () => {
  assert.deepEqual(nearestOnFabric(L, { x: 10, y: 10 }), { x: 15, y: 10 })
})
