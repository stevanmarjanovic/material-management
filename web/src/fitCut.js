// Keeps a dragged cut rectangle on the fabric, which can be any 90° shape (a set of non-overlapping rects).
// The API still validates the cut; this only makes the drag stop at the fabric's edges.

const EPS = 1e-9
const overlap = (a, b) =>
  Math.max(0, Math.min(a.x + a.w, b.x + b.w) - Math.max(a.x, b.x)) * Math.max(0, Math.min(a.y + a.h, b.y + b.h) - Math.max(a.y, b.y))

// A rect is on the fabric when the pieces' rects cover all of its area.
export const onFabric = (shape, r) => r.w > 0 && r.h > 0 && Math.abs(shape.reduce((s, x) => s + overlap(x, r), 0) - r.w * r.h) < EPS

// Nearest point that lies on the fabric (for a drag that starts outside it, e.g. in a cut-out corner).
export function nearestOnFabric(shape, p) {
  let best = null
  for (const r of shape) {
    const q = { x: Math.min(r.x + r.w, Math.max(r.x, p.x)), y: Math.min(r.y + r.h, Math.max(r.y, p.y)) }
    const d = (q.x - p.x) ** 2 + (q.y - p.y) ** 2
    if (!best || d < best.d) best = { ...q, d }
  }
  return { x: best.x, y: best.y }
}

// Rect from `start` towards `p` that stays on the fabric and whose far corner is closest to `p`.
// The far corner may only move to `p` itself or to fabric edges between start and p, so it snaps to edges.
export function fitCut(shape, start, p) {
  const between = (a, b, v) => v >= Math.min(a, b) - EPS && v <= Math.max(a, b) + EPS
  const xs = [p.x, ...shape.flatMap((r) => [r.x, r.x + r.w])].filter((x) => between(start.x, p.x, x))
  const ys = [p.y, ...shape.flatMap((r) => [r.y, r.y + r.h])].filter((y) => between(start.y, p.y, y))
  let best = { x: start.x, y: start.y, w: 0, h: 0 }
  let bestD = Infinity
  for (const x of xs)
    for (const y of ys) {
      const r = { x: Math.min(start.x, x), y: Math.min(start.y, y), w: Math.abs(x - start.x), h: Math.abs(y - start.y) }
      const d = (x - p.x) ** 2 + (y - p.y) ** 2
      if (d < bestD && onFabric(shape, r)) [best, bestD] = [r, d]
    }
  return best
}
