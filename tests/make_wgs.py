"""Writes a fake FH3 PC (wgs) save folder from an XBsync-style folder, for the tests.

    python3 tests/make_wgs.py <xbox folder> <wgs folder>

Layout as read by github.com/Z1ni/XGP-save-extractor: containers.index + <GUID>/container.N + <GUID>/<file GUID>.
"""
import os
import shutil
import struct
import sys
import uuid


def s(text):
    return struct.pack("<i", len(text)) + text.encode("utf-16-le")


def main(xbox, wgs):
    user = os.path.join(wgs, "000900000ABCDEF1_0000000000000000000000006E8F2F63")
    os.makedirs(os.path.join(wgs, "t"))  # temp folder the tool must ignore
    os.makedirs(user)
    names = sorted(os.listdir(xbox))
    index = struct.pack("<ii", 14, len(names) + 1) + s("") + s("Microsoft.OpusPG_8wekyb3d8bbwe!App")
    index += bytes(8 + 4) + s("id") + bytes(8)
    for n, name in enumerate(names + ["NotSynced"]):
        folder = uuid.uuid4()
        index += s(name) + s(name) + s('"0x8D0"') + bytes([n % 250 + 1]) + bytes(4) + folder.bytes_le + bytes(8 + 16)
        if name == "NotSynced":
            continue  # listed in the index, never downloaded
        out = os.path.join(user, folder.hex.upper())
        os.makedirs(out)
        files = sorted(os.listdir(os.path.join(xbox, name)))
        container = struct.pack("<ii", 4, len(files))
        for i, f in enumerate(files):
            blob, other = uuid.uuid4(), uuid.uuid4()
            # Alternate between "both GUIDs equal" and "only the second GUID exists".
            container += f.encode("utf-16-le").ljust(128, b"\0") + (blob.bytes_le * 2 if i % 2 else other.bytes_le + blob.bytes_le)
            shutil.copyfile(os.path.join(xbox, name, f), os.path.join(out, blob.hex.upper()))
        open(os.path.join(out, f"container.{n % 250 + 1}"), "wb").write(container)
    open(os.path.join(user, "containers.index"), "wb").write(index)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
