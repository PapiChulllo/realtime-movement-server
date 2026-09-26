# Realtime Movement Server

**A Unity Transport UDP server for a small 2D realtime movement prototype.** Clients report positions; the server stores the latest position per connection ID and broadcasts updates (plus a late-join snapshot) to every connected client.

**Paired repository:** [realtime-movement-client](https://github.com/PapiChulllo/realtime-movement-client)

---

## How it works

1. A client holds W / A / S / D, moves locally, and sends `PlayerMoved,<x>,<y>`.
2. The server maps the sender’s connection to an integer player ID and stores that position.
3. The server broadcasts `PlayerMoved,<playerId>,<x>,<y>` on the reliable sequenced pipeline to all active connections.
4. On connect, the new client also receives every position currently held in server memory.

**Status / limitations:** educational networking sample. Positions are **client-authoritative** — no speed, bounds, timestamp, or rate checks. Disconnect removes connection lookup entries but **does not** clear `playerPositions`, so late joiners can see stale IDs. The Unicode CSV protocol is unversioned and culture-sensitive for floats. No auth, encryption, matchmaking, reconnect, interpolation, or persistence. Capacity constant is `1000` with no load testing. No automated tests; Unity was unavailable for re-verification in this documentation pass.

## Tech stack

| Area | What it uses |
|---|---|
| Engine | **Unity** `2022.3.5f1` |
| Networking | **Unity Transport** `1.3.4` |
| Bind | UDP port **9001**, `NetworkEndPoint.AnyIpv4` |
| Encoding | `Encoding.Unicode` payload + transport `int` byte-length prefix |
| Scene | `Assets/Scenes/SampleScene.unity` |

Pipelines created on both sides:

- **Reliable and in order:** `FragmentationPipelineStage` → `ReliableSequencedPipelineStage` (all current movement messages)
- **Fire and forget:** `FragmentationPipelineStage` only (defined, unused by gameplay)

## What's in the project

| System | Key files |
|---|---|
| Driver bind/listen, pipelines, connection IDs, framing, send/receive | `Assets/NetworkServer.cs` |
| Message parse bridge and connect/disconnect hooks | `Assets/NetworkServerProcessing.cs` |
| Position dictionary, fan-out broadcast, late-join snapshot | `Assets/GameLogic.cs` |
| Editor scene with server components | `Assets/Scenes/SampleScene.unity` |

Three authored C# scripts (~9 KB). No third-party game art packs; this is a networking lab project.

### Code / system highlights

- **`NetworkServer`:** creates the driver and both pipelines, binds port 9001, accepts connections, assigns the lowest free integer ID, frames messages as `int` length + Unicode bytes, and tears down on destroy.
- **`GameLogic.UpdatePlayerPosition`:** writes `playerPositions[playerId]` and sends a reliable `PlayerMoved` update to every ID returned by `GetAllClientIDs()`.
- **`SendAllPlayerPositions`:** on connect, replays the full position map to the joining client.

## Scenes

| Scene | Purpose |
|---|---|
| `Assets/Scenes/SampleScene.unity` | Server Editor scene — enter Play mode so the Console reports listening on port 9001 before any client connects |

## Run (Editor)

1. Open this repo root in Unity Hub with **2022.3.5f1**.
2. Open `Assets/Scenes/SampleScene.unity` and enter Play mode.
3. Start the [client](https://github.com/PapiChulllo/realtime-movement-client) afterward (set its hardcoded IP to `127.0.0.1` for localhost, or this machine’s LAN IPv4). Keep UDP **9001** open on the host firewall.

No verified standalone build is claimed in this documentation.

## Third-party assets

None beyond Unity packages (Transport, 2D feature set, TextMesh Pro, etc. in `Packages/manifest.json`). Authored work is the three networking scripts above.

## About this repository

Public educational / portfolio showcase for low-level Unity Transport server work under **PapiChulllo**. Pair with [realtime-movement-client](https://github.com/PapiChulllo/realtime-movement-client). Documentation is conservative and source-derived; runtime behavior was not re-tested in Unity for this README pass.
