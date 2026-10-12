// Print a compact tree of a component (set) from the decoded kit, with variable names.
// Usage: node inspect.js <canvas.json> <component or set name | node id> [variant regex] [max depth]
const fs = require('fs');
const [, , input, target, variantFilter, maxDepthArg] = process.argv;
const maxDepth = maxDepthArg ? +maxDepthArg : 6;
const m = JSON.parse(fs.readFileSync(input));
const nodes = m.nodeChanges;
const gid = (g) => g.sessionID + ':' + g.localID;
const byId = new Map(nodes.map((x) => [gid(x.guid), x]));
const children = new Map();
for (const x of nodes) {
  if (!x.parentIndex) continue;
  const p = gid(x.parentIndex.guid);
  if (!children.has(p)) children.set(p, []);
  children.get(p).push(x);
}
for (const list of children.values()) list.sort((a, b) => (a.parentIndex.position < b.parentIndex.position ? -1 : 1));

const h = (v) => Math.round(v * 255).toString(16).padStart(2, '0').toUpperCase();
const hex = (c, op = 1) => '#' + (Math.round((c.a ?? 1) * op * 255) === 255 ? '' : h((c.a ?? 1) * op)) + h(c.r) + h(c.g) + h(c.b);
const varName = (d) => d?.value?.alias ? byId.get(gid(d.value.alias.guid))?.name ?? 'ext-var' : undefined;
const r1 = (v) => Math.round(v * 100) / 100;

function paint(p) {
  if (p.visible === false) return null;
  let s = p.type === 'SOLID' ? hex(p.color, p.opacity ?? 1) : p.type === 'IMAGE' ? 'image' : p.type.replace('GRADIENT_', 'grad-').toLowerCase() + '(' + (p.stops || []).map((st) => hex(st.color)).join(',') + ')';
  const v = varName(p.colorVar);
  if (v) s += `{${v}}`;
  if (p.blendMode && p.blendMode !== 'NORMAL' && p.blendMode !== 'PASS_THROUGH') s += `/${p.blendMode}`;
  return s;
}
function effect(e) {
  if (e.visible === false) return null;
  const parts = [e.type.toLowerCase()];
  if (e.color) parts.push(hex(e.color));
  if (e.offset) parts.push(`${r1(e.offset.x)},${r1(e.offset.y)}`);
  if (e.radius) parts.push('r' + r1(e.radius));
  if (e.spread) parts.push('s' + r1(e.spread));
  return parts.join(' ');
}

function describe(x) {
  const a = [`${x.type === 'SYMBOL' ? 'COMPONENT' : x.type} "${x.name}"`];
  if (x.size) a.push(`${r1(x.size.x)}x${r1(x.size.y)}`);
  if (x.transform && (x.transform.m02 || x.transform.m12)) a.push(`@${r1(x.transform.m02)},${r1(x.transform.m12)}`);
  if (x.visible === false) a.push('HIDDEN');
  if (x.opacity !== undefined && x.opacity !== 1) a.push('opacity ' + r1(x.opacity));
  const rad = x.rectangleTopLeftCornerRadius ?? x.cornerRadius;
  if (rad) a.push('radius ' + r1(rad) + (x.rectangleCornerRadiiIndependent && (x.rectangleTopLeftCornerRadius !== x.rectangleBottomRightCornerRadius) ? ` (${[x.rectangleTopLeftCornerRadius, x.rectangleTopRightCornerRadius, x.rectangleBottomRightCornerRadius, x.rectangleBottomLeftCornerRadius].map(r1).join('/')})` : ''));
  if (x.stackMode && x.stackMode !== 'NONE') {
    const pad = [x.stackVerticalPadding ?? 0, x.stackPaddingRight ?? x.stackHorizontalPadding ?? 0, x.stackPaddingBottom ?? x.stackVerticalPadding ?? 0, x.stackHorizontalPadding ?? 0];
    a.push(`${x.stackMode.toLowerCase()} gap ${r1(x.stackSpacing ?? 0)} pad ${pad.map(r1).join(',')}`);
  }
  const fills = (x.fillPaints || []).map(paint).filter(Boolean);
  if (fills.length && x.type !== 'TEXT') a.push('fill ' + fills.join(' + '));
  const strokes = (x.strokePaints || []).map(paint).filter(Boolean);
  if (strokes.length) a.push(`stroke ${strokes.join(' + ')} w${r1(x.strokeWeight ?? 1)} ${(x.strokeAlign || '').toLowerCase()}`);
  const fx = (x.effects || []).map(effect).filter(Boolean);
  if (fx.length) a.push('fx [' + fx.join('; ') + ']');
  if (x.blendMode && !['NORMAL', 'PASS_THROUGH'].includes(x.blendMode)) a.push('blend ' + x.blendMode);
  if (x.type === 'TEXT') {
    const t = x.textData?.characters ?? '';
    a.push(`"${t.slice(0, 40)}" ${x.fontName?.family} ${x.fontName?.style} ${x.fontSize}` + (x.lineHeight ? `/${r1(x.lineHeight.value)}${x.lineHeight.units === 'PERCENT' ? '%' : ''}` : '') + (fills.length ? ' color ' + fills.join(' + ') : ''));
  }
  if (x.type === 'INSTANCE' && x.symbolData?.symbolID) {
    const s = byId.get(gid(x.symbolData.symbolID));
    const parent = s && s.parentIndex && byId.get(gid(s.parentIndex.guid));
    a.push(`→ ${parent?.isStateGroup ? parent.name + ' / ' : ''}${s?.name ?? '?'}`);
  }
  const vars = (x.variableConsumptionMap?.entries || []).filter((e) => varName(e.variableData)).map((e) => `${e.variableField.toLowerCase().replace(/rectangle_(top|bottom)_(left|right)_corner_radius/, 'radius').replace(/stack_padding_(left|right)/, 'pad_h').replace(/stack_padding_(top|bottom)/, 'pad_v')}=${varName(e.variableData)}`);
  if (vars.length) a.push('vars{' + [...new Set(vars)].join(', ') + '}');
  return a.join(' | ');
}

function walk(x, depth) {
  console.log('  '.repeat(depth) + describe(x));
  if (depth >= maxDepth) return;
  let kids = children.get(gid(x.guid)) || [];
  // Instances carry no children in the file: expand their main component instead.
  if (x.type === 'INSTANCE' && !kids.length && x.symbolData?.symbolID) kids = children.get(gid(x.symbolData.symbolID)) || [];
  for (const c of kids) walk(c, depth + 1);
}

// Several targets can be given, separated by ";".
for (const t of target.split(';')) {
  const root = byId.get(t) || nodes.find((x) => x.name === t && (x.isStateGroup || x.type === 'SYMBOL')) || nodes.find((x) => x.name === t);
  if (!root) { console.log('NOT FOUND: ' + t); continue; }
  if (root.isStateGroup) {
    const re = variantFilter ? new RegExp(variantFilter) : null;
    const variants = (children.get(gid(root.guid)) || []).filter((v) => !re || re.test(v.name));
    console.log(`SET "${root.name}" ${gid(root.guid)}: ${variants.length} variants shown`);
    for (const v of variants) walk(v, 0);
  } else walk(root, 0);
}
