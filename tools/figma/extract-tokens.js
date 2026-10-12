// Extract variables (all modes, aliases resolved) and styles from decoded canvas.json.
// Usage: node extract-tokens.js <canvas.json> <outDir>
const fs = require('fs');
const path = require('path');
const [, , input, outDir] = process.argv;
fs.mkdirSync(outDir, { recursive: true });
const m = JSON.parse(fs.readFileSync(input));
const nodes = m.nodeChanges;
const gid = (g) => g.sessionID + ':' + g.localID;
const byId = new Map(nodes.map((x) => [gid(x.guid), x]));

const h = (v) => Math.round(v * 255).toString(16).padStart(2, '0').toUpperCase();
const hex = (c, op = 1) => '#' + (Math.round((c.a ?? 1) * op * 255) === 255 ? '' : h((c.a ?? 1) * op)) + h(c.r) + h(c.g) + h(c.b);

const sets = nodes.filter((x) => x.type === 'VARIABLE_SET');
const modeName = new Map();
for (const s of sets) for (const md of s.variableSetModes || []) modeName.set(gid(md.id), md.name);

function raw(v) {
  const d = v.value || {};
  if (d.colorValue) return hex(d.colorValue);
  if (d.floatValue !== undefined) return d.floatValue;
  if (d.boolValue !== undefined) return d.boolValue;
  if (d.textValue !== undefined) return d.textValue;
  if (d.alias) return { alias: gid(d.alias.guid) };
  return d;
}

function resolve(varNode, modeId, depth = 0) {
  const entries = varNode.variableDataValues?.entries || [];
  let e = entries.find((x) => gid(x.modeID) === modeId);
  if (!e) e = entries.find((x) => modeName.get(gid(x.modeID)) === modeName.get(modeId)) || entries[0];
  if (!e) return null;
  const r = raw(e.variableData);
  if (r && r.alias) {
    const t = byId.get(r.alias);
    if (!t || depth > 10) return { unresolvedAlias: r.alias };
    return resolve(t, modeId, depth + 1);
  }
  return r;
}

const out = {};
for (const s of sets) {
  const setId = gid(s.guid);
  const modes = (s.variableSetModes || []).map((md) => [gid(md.id), md.name]);
  const vars = nodes.filter((x) => x.type === 'VARIABLE' && x.variableSetID?.guid && gid(x.variableSetID.guid) === setId);
  out[`${s.name} (${setId})`] = {
    modes: modes.map((x) => x[1]),
    variables: Object.fromEntries(vars.map((v) => {
      const row = {};
      for (const [mid, mname] of modes) {
        const entries = v.variableDataValues?.entries || [];
        const e = entries.find((x) => gid(x.modeID) === mid);
        const rv = e ? raw(e.variableData) : null;
        const resolved = resolve(v, mid);
        row[mname] = rv && rv.alias ? { alias: byId.get(rv.alias)?.name, value: resolved } : resolved;
      }
      return [v.name, row];
    })),
  };
}
fs.writeFileSync(path.join(outDir, 'variables.json'), JSON.stringify(out, null, 1));

// Styles: nodes carrying a styleType.
const styles = nodes.filter((x) => x.styleType && x.styleType !== 'NONE');
const paintOut = (p) => p.type === 'SOLID' ? { c: hex(p.color, p.opacity ?? 1), bm: p.blendMode !== 'NORMAL' ? p.blendMode : undefined, v: p.colorVar ? 'var' : undefined } : { t: p.type, o: p.opacity, bm: p.blendMode, stops: p.stops?.map((s) => [+s.position.toFixed(3), hex(s.color)]) };
const effOut = (e) => ({ t: e.type, c: e.color ? hex(e.color) : undefined, o: e.offset ? [e.offset.x, e.offset.y] : undefined, r: e.radius, s: e.spread || undefined, bm: e.blendMode !== 'NORMAL' ? e.blendMode : undefined, vis: e.visible === false ? false : undefined });
const st = styles.map((s) => {
  const r = { name: s.name, type: s.styleType, id: gid(s.guid) };
  if (s.styleType === 'FILL') r.fills = (s.fillPaints || []).map(paintOut);
  if (s.styleType === 'EFFECT') r.effects = (s.effects || []).map(effOut);
  if (s.styleType === 'TEXT') Object.assign(r, { font: s.fontName && `${s.fontName.family} ${s.fontName.style}`, size: s.fontSize, lh: s.lineHeight && `${+s.lineHeight.value.toFixed(2)}${s.lineHeight.units === 'PIXELS' ? 'px' : s.lineHeight.units === 'PERCENT' ? '%' : ' ' + s.lineHeight.units}`, ls: s.letterSpacing && `${+s.letterSpacing.value.toFixed(3)}${s.letterSpacing.units === 'PIXELS' ? 'px' : '%'}` });
  return r;
});
fs.writeFileSync(path.join(outDir, 'styles.json'), JSON.stringify(st, null, 1));

// Page -> top-level symbols / component sets inventory.
const children = new Map();
for (const x of nodes) if (x.parentIndex) { const p = gid(x.parentIndex.guid); (children.get(p) || children.set(p, []).get(p)).push(x); }
const pageOf = (x) => { let c = x; while (c && c.type !== 'CANVAS') c = c.parentIndex && byId.get(gid(c.parentIndex.guid)); return c?.name; };
const inv = {};
for (const x of nodes) {
  if (x.type !== 'SYMBOL') continue;
  const parent = x.parentIndex && byId.get(gid(x.parentIndex.guid));
  const isVariant = parent && parent.isStateGroup;
  const key = pageOf(x) || '?';
  const owner = isVariant ? parent : x;
  const name = owner.name;
  ((inv[key] ||= {})[name] ||= { id: gid(owner.guid), variants: 0 }).variants += isVariant ? 1 : 0;
}
fs.writeFileSync(path.join(outDir, 'components.json'), JSON.stringify(inv, null, 1));
console.log('sets', sets.length, 'styles', st.length, 'pages w/ components', Object.keys(inv).length);
