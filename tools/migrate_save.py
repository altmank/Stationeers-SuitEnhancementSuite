"""Move a Stationeers save from the old uniform slot layout to Suit Enhancement Suite's layout.

Uniforms: the old layout put 4 extra slots first (0-3) and the uniform's own 4 slots after (4-7).
The new layout puts the uniform's own slots first (0-3), extra storage at 4-7, shared power at 8-9.
Items in the old extra slots move to free slots of a locker; the uniform's own items move 4-7 -> 0-3.
Suits and other garments are unchanged.

Run it ONCE per save, with the game closed and a backup made: the new layout reuses indices 0-7, so a
second run would move items again. --apply writes a marker file <save>.ses-migrated and refuses to run
when it exists.

Usage: migrate_save.py <save.save> [--locker ID] [--apply]
"""
import re, sys, zipfile, shutil, os, io, argparse

def blocks(s):
    for m in re.finditer(r"<ThingSaveData\b.*?</ThingSaveData>", s, re.S):
        yield m

def field(b, name):
    m = re.search(r"<%s>([^<]*)</%s>" % (name, name), b)
    return m.group(1) if m else None

def migrate(xml, locker):
    things = {}
    for m in blocks(xml):
        b = m.group(0)
        things[field(b, "ReferenceId")] = (m.start(), m.end(), field(b, "PrefabName"), field(b, "ParentReferenceId"), field(b, "ParentSlotId"))
    if locker not in things:
        raise SystemExit(f"locker {locker} not in save")
    uniforms = {rid for rid, t in things.items() if (t[2] or "").startswith("Uniform")}
    used = {int(t[4]) for t in things.values() if t[3] == locker and t[4] is not None}
    free = [i for i in range(30) if i not in used]
    edits, moves = [], []
    for rid, (st, en, pf, par, slot) in sorted(things.items(), key=lambda kv: kv[1][0]):
        if par in uniforms and slot is not None:
            old = int(slot)
            if old <= 3:
                if not free: raise SystemExit("locker full")
                new_par, new_slot = locker, free.pop(0)
            elif old <= 7:
                new_par, new_slot = par, old - 4
            else:
                raise SystemExit(f"{rid} {pf} in uniform slot {old}: unexpected")
            moves.append((rid, pf, par, old, new_par, new_slot))
            edits.append((st, en, new_par, new_slot))
    out, pos = [], 0
    for st, en, new_par, new_slot in edits:
        b = xml[st:en]
        b = re.sub(r"<ParentReferenceId>[^<]*</ParentReferenceId>", f"<ParentReferenceId>{new_par}</ParentReferenceId>", b, count=1)
        b = re.sub(r"<ParentSlotId>[^<]*</ParentSlotId>", f"<ParentSlotId>{new_slot}</ParentSlotId>", b, count=1)
        out.append(xml[pos:st]); out.append(b); pos = en
    out.append(xml[pos:])
    return "".join(out), moves, len(things)

def validate(xml, n_before, locker):
    things = {}
    slots = {}
    for m in blocks(xml):
        b = m.group(0); rid = field(b, "ReferenceId")
        if rid in things: raise SystemExit(f"duplicate id {rid}")
        things[rid] = b
        par, slot = field(b, "ParentReferenceId"), field(b, "ParentSlotId")
        if par and par != "0" and slot is not None:
            key = (par, slot)
            if key in slots: raise SystemExit(f"two items in {key}: {slots[key]} and {rid}")
            slots[key] = rid
            if par not in things and not re.search(r"<ReferenceId>%s</ReferenceId>" % par, xml):
                raise SystemExit(f"{rid} parent {par} missing")
    if len(things) != n_before: raise SystemExit(f"thing count {len(things)} != {n_before}")
    for (par, slot), rid in slots.items():
        pf = re.search(r"<ReferenceId>%s</ReferenceId>\s*<PrefabName>([^<]*)<" % par, xml)
        if pf and pf.group(1).startswith("Uniform") and int(slot) > 9: raise SystemExit(f"uniform slot {slot} out of range")
        if par == locker and int(slot) > 29: raise SystemExit("locker slot out of range")

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("save"); ap.add_argument("--locker", default="126425"); ap.add_argument("--apply", action="store_true")
    a = ap.parse_args()
    marker = a.save + ".ses-migrated"
    if os.path.exists(marker):
        raise SystemExit(f"already migrated ({marker} exists); refusing to run again")
    with zipfile.ZipFile(a.save) as z:
        infos = z.infolist(); data = {i.filename: z.read(i.filename) for i in infos}
    xml = data["world.xml"].decode("utf-8")
    new, moves, n = migrate(xml, a.locker)
    validate(new, n, a.locker)
    for rid, pf, par, old, np, ns in moves: print(f"  {rid} {pf}: {par}#{old} -> {np}#{ns}")
    print(f"  {len(moves)} moves, {n} things, validation ok")
    if not a.apply or not moves: return
    tmp = a.save + ".tmp"
    with zipfile.ZipFile(tmp, "w") as z:
        for i in infos:
            z.writestr(i, new.encode("utf-8") if i.filename == "world.xml" else data[i.filename], compress_type=i.compress_type)
    with zipfile.ZipFile(tmp) as z:
        assert z.testzip() is None
        assert z.read("world.xml").decode("utf-8") == new
    os.replace(tmp, a.save)
    open(marker, "w").close()
    print("  written")

main()
