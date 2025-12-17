package simple.gradient

import it.unibo.collektive.aggregate.api.DataSharingMethod
import it.unibo.collektive.networking.Mailbox
import it.unibo.collektive.networking.Message
import it.unibo.collektive.networking.NeighborsData
import it.unibo.collektive.networking.OutboundEnvelope
import it.unibo.collektive.path.Path
import kotlin.math.pow
import kotlin.math.sqrt

class NetworkWithDistance(private val networkManager: NetworkManagerWithDistance, private val local: Node) : Mailbox<Node> {
    override val inMemory: Boolean = false

    init {
        networkManager.registerDevice(local)
    }

    override fun deliverableFor(outboundMessage: OutboundEnvelope<Node>) = networkManager.send(local, outboundMessage)

    override fun deliverableReceived(message: Message<Node, *>) {
        error("This network is supposed to be in-memory, no need to deliver messages since it is already in the buffer")
    }

    override fun currentInbound(): NeighborsData<Node> = networkManager.receiveMessageFor(local)
}

class NetworkManagerWithDistance(var maxDistance: Double) {

    /**
     * Latest known state, keyed by stable identity (node id).
     */
    private val nodesById: MutableMap<Int, NodeSnapshot> = mutableMapOf()

    /**
     * Inbound buffer for each receiverId: senderId -> message
     */
    private val messageBuffer: MutableMap<Int, MutableMap<Int, Message<Node, *>>> = mutableMapOf()

    /**
     * Cached adjacency, recomputed on each tick:
     * nodeId -> set of neighborIds within maxDistance (based on latest positions)
     */
    private var adjacencyById: Map<Int, Set<Int>> = emptyMap()

    fun registerDevice(device: Node) {
        nodesById[device.id] = NodeSnapshot(device.id, device.position)
        messageBuffer.getOrPut(device.id) { mutableMapOf() }
        // Optional: computeTopology() here if you want immediate connectivity.
    }

    fun updatePosition(nodeId: Int, newPosition: Position) {
        val current = nodesById[nodeId] ?: error("Node $nodeId not registered")
        nodesById[nodeId] = current.copy(position = newPosition)
    }

    /**
     * Call once per simulation tick, after all updatePosition(...) calls.
     */
    fun computeTopology() {
        val ids = nodesById.keys.toList()
        val snapshots = nodesById.toMap()

        val next = HashMap<Int, MutableSet<Int>>(ids.size)
        ids.forEach { next[it] = mutableSetOf() }

        for (i in 0 until ids.size) {
            val aId = ids[i]
            val a = snapshots[aId] ?: continue
            for (j in i + 1 until ids.size) {
                val bId = ids[j]
                val b = snapshots[bId] ?: continue

                if (Position.distance(a.position, b.position) <= maxDistance) {
                    next[aId]!!.add(bId)
                    next[bId]!!.add(aId)
                }
            }
        }

        adjacencyById = next.mapValues { it.value.toSet() }
    }

    fun getNeighbors(nodeId: Int): Set<Int> {
        return adjacencyById[nodeId].orEmpty()
    }

    /**
     * Sends only to CURRENT cached neighbors (topology of this tick).
     */
    fun send(sender: Node, envelope: OutboundEnvelope<Node>) {
        val senderSnap = nodesById[sender.id] ?: return
        val neighborIds = adjacencyById[senderSnap.id].orEmpty()

        neighborIds.forEach { neighborId ->
            val message = envelope.prepareMessageFor(senderSnap.toNode())
            val inboundForNeighbor = messageBuffer.getOrPut(neighborId) { mutableMapOf() }
            inboundForNeighbor[senderSnap.id] = message
        }
    }

    /**
     * Exposes only messages from CURRENT cached neighbors (topology of this tick).
     */
    fun receiveMessageFor(receiver: Node): NeighborsData<Node> {
        val receiverId = receiver.id
        val neighborIds = adjacencyById[receiverId].orEmpty()
        val inbound = messageBuffer.getOrPut(receiverId) { mutableMapOf() }

        return object : NeighborsData<Node> {
            override val neighbors: Set<Node>
                get() = neighborIds.mapNotNull { id -> nodesById[id]?.toNode() }.toSet()

            @Suppress("UNCHECKED_CAST")
            override fun <Value> dataAt(path: Path, dataSharingMethod: DataSharingMethod<Value>): Map<Node, Value> =
                inbound
                    .asSequence()
                    .filter { (senderId, _) -> senderId in neighborIds } // enforce current topology
                    .mapNotNull { (senderId, msg) ->
                        val senderNode = nodesById[senderId]?.toNode() ?: return@mapNotNull null
                        val raw = msg.sharedData.getOrElse(path) { NoValue } as Value
                        if (raw == NoValue) null else senderNode to raw
                    }
                    .toMap()
        }
    }

    /**
     * Optional but recommended: keeps buffers aligned with dynamic topology,
     * so you don't accumulate stale senders forever.
     *
     * Call after computeTopology() each tick.
     */
    fun pruneBuffers() {
        adjacencyById.forEach { (receiverId, neighborIds) ->
            val inbound = messageBuffer[receiverId] ?: return@forEach
            inbound.keys.retainAll(neighborIds)
        }
    }

    private data class NodeSnapshot(val id: Int, val position: Position) {
        fun toNode(): Node = Node(id, position)
    }

    private object NoValue
}

data class Position(val x: Double, val y: Double, val z: Double) {
    companion object {
        fun origin() = Position(0.0,0.0,0.0)
        fun distance(pos1: Position, pos2: Position): Double = sqrt(
            (pos2.x - pos1.x).pow(2) + (pos2.y - pos1.y).pow(2) + (pos2.z - pos1.z).pow(2)
        )
    }
}

data class Node(val id: Int, var position: Position)
