from __future__ import annotations

import socket

from .models import GesturePacket
from .protocol import serialize_packet


class UdpJsonSender:
    def __init__(self, host: str, port: int) -> None:
        self._target = (host, port)
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def send(self, packet: GesturePacket) -> None:
        self._socket.sendto(serialize_packet(packet), self._target)

    def close(self) -> None:
        self._socket.close()
