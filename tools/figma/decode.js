// Decode a Figma canvas.fig (fig-kiwi) into JSON.
// Usage: node decode.js <canvas.fig> <out.json>
const fs = require('fs');
const zlib = require('zlib');
const kiwi = require('kiwi-schema');

const [, , input, output] = process.argv;
const buf = fs.readFileSync(input);
const header = buf.subarray(0, 8).toString('latin1');
if (header !== 'fig-kiwi') throw new Error('bad header ' + header);
const version = buf.readUInt32LE(8);

const chunks = [];
let off = 12;
while (off + 4 <= buf.length) {
  const len = buf.readUInt32LE(off);
  off += 4;
  chunks.push(buf.subarray(off, off + len));
  off += len;
}

function inflate(c) {
  if (c[0] === 0x28 && c[1] === 0xb5 && c[2] === 0x2f && c[3] === 0xfd) return zlib.zstdDecompressSync(c);
  try { return zlib.inflateRawSync(c); } catch { return zlib.inflateSync(c); }
}

const schemaBytes = inflate(chunks[0]);
const schema = kiwi.decodeBinarySchema(schemaBytes);
const compiled = kiwi.compileSchema(schema);
const data = inflate(chunks[1]);
const message = compiled.decodeMessage(data);

// Byte arrays (blobs, geometry) are bulky and useless for tokens: summarize them.
const json = JSON.stringify(message, (k, v) => (v instanceof Uint8Array ? `<bytes ${v.length}>` : typeof v === "bigint" ? v.toString() : v));
fs.writeFileSync(output, json);
fs.writeFileSync(output.replace(/\.json$/, '.schema.txt'), kiwi.prettyPrintSchema(schema));
console.log({ version, chunks: chunks.map(c => c.length), nodes: message.nodeChanges?.length, blobs: message.blobs?.length, outMB: (json.length / 1e6).toFixed(1) });
