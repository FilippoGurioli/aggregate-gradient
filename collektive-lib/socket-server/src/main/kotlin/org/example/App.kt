package org.example

import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.decodeFromJsonElement
import java.io.BufferedReader
import java.io.BufferedWriter
import java.io.InputStreamReader
import java.io.OutputStreamWriter
import java.net.ServerSocket
import java.net.Socket
import java.util.concurrent.Executors
import org.example.message.CreateSimulation
import org.example.message.Neighbor
import org.example.message.NeighborReq
import org.example.message.NewPosition
import org.example.message.SetSource
import org.example.message.State
import org.example.message.Step
import simple.gradient.CollektiveEngineWithDistance
import simple.gradient.Position
import java.text.ParseException

val json = Json {
    ignoreUnknownKeys = true
    encodeDefaults = true
}

private val engines = mutableMapOf<String, CollektiveEngineWithDistance>()

fun main(args: Array<String>) {
    val host = "127.0.0.1"
    val port = (args.firstOrNull()?.toIntOrNull()) ?: 9090
    val pool = Executors.newCachedThreadPool()
    ServerSocket(port).use { server ->
        println("Socket server listening on $host:$port")
        while (true) {
            val client = server.accept()
            pool.submit { App.handleClient(client) }
        }
    }
}

object App {
    internal fun handleClient(socket: Socket) {
        socket.use { s ->
            val remote = "${s.inetAddress.hostAddress}:${s.port}"
            println("Client connected: $remote")
            val reader = BufferedReader(InputStreamReader(s.getInputStream(), Charsets.UTF_8))
            val writer = BufferedWriter(OutputStreamWriter(s.getOutputStream(), Charsets.UTF_8))
            while (true) {
                val line = reader.readLine() ?: break
                dispatch(remote, line, writer)
            }
            println("Client disconnected: $remote")
        }
    }

    private fun dispatch(remote: String, line: String, writer: BufferedWriter) {
        val trimmed = line.trim()
        if (trimmed.isEmpty()) return
        val obj: JsonObject = json.parseToJsonElement(trimmed).jsonObject
        val op = obj["op"]?.jsonPrimitive?.content
        val data = obj["data"] ?: return
        when (op) {
            "createSim" -> {
                val req = json.decodeFromJsonElement<CreateSimulation>(data)
                engines[remote] = CollektiveEngineWithDistance(req.nodeCount, req.maxDistance)
            }
            "setSource" -> {
                val req = json.decodeFromJsonElement<SetSource>(data)
                engines[remote]?.setSource(req.nodeId, true)
            }
            "step" -> {
                val req = json.decodeFromJsonElement<Step>(data)
                engines[remote]?.stepMany(req.stepCount)
                send(writer, State(engines[remote]?.getValues() ?: return))
            }
            "newPosition" -> {
                val req = json.decodeFromJsonElement<NewPosition>(data)
                engines[remote]?.updateNodePosition(req.nodeId, Position(req.x, req.y, req.z))
            }
            "neighbor" -> {
                val req = json.decodeFromJsonElement<NeighborReq>(data)
                val neighbor = Neighbor(engines[remote]?.getNeighborhood(req.nodeId) ?: return)
                send(writer, neighbor)
            }
            else -> {
                throw ParseException("operation $op not supported", 0)
            }
        }
    }

    private fun send(writer: BufferedWriter, response: Any) {
        writer.write(json.encodeToString(response))
        writer.newLine()
        writer.flush()
    }
}
