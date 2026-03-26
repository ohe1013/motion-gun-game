from __future__ import annotations

import json

from .models import GesturePacket


def serialize_packet(packet: GesturePacket) -> bytes:
    return json.dumps(packet.to_payload(), separators=(",", ":")).encode("utf-8")
