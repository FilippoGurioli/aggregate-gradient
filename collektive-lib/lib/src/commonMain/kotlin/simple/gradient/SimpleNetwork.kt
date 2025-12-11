package simple.gradient

import it.unibo.collektive.aggregate.api.DataSharingMethod
import it.unibo.collektive.networking.Mailbox
import it.unibo.collektive.networking.Message
import it.unibo.collektive.networking.NeighborsData
import it.unibo.collektive.networking.OutboundEnvelope
import it.unibo.collektive.path.Path

class SimpleNetwork(private val networkManager: NetworkManager, private val localId: Int) : Mailbox<Int> {
    override val inMemory: Boolean = false

    init {
        networkManager.registerDevice(localId)
    }

    override fun deliverableFor(outboundMessage: OutboundEnvelope<Int>) = networkManager.send(localId, outboundMessage)

    override fun deliverableReceived(message: Message<Int, *>) {
        error("This network is supposed to be in-memory, no need to deliver messages since it is already in the buffer")
    }

    override fun currentInbound(): NeighborsData<Int> = networkManager.receiveMessageFor(localId)
}

class NetworkManager(private val maxDegree: Int = 3) {
    private var messageBuffer: MutableMap<Int, MutableMap<Int, Message<Int, *>>> = mutableMapOf()
    private val adjacency: MutableMap<Int, MutableSet<Int>> = mutableMapOf()

    fun registerDevice(deviceId: Int) {
        messageBuffer.getOrPut(deviceId) { mutableMapOf() }
        adjacency.getOrPut(deviceId) { mutableSetOf() }
        autoConnect(deviceId)
    }

    /**
     * Try to connect two nodes with an undirected edge.
     * Returns true if the edge was added, false if degree limit is hit.
     */
    fun connect(a: Int, b: Int): Boolean {
        val neighborsA = adjacency.getOrPut(a) { mutableSetOf() }
        val neighborsB = adjacency.getOrPut(b) { mutableSetOf() }
        if (a == b) return false
        if (neighborsA.size >= maxDegree || neighborsB.size >= maxDegree) {
            return false
        }
        neighborsA.add(b)
        neighborsB.add(a)
        return true
    }

    private fun autoConnect(deviceId: Int) {
        val newNeighbors = adjacency.getOrPut(deviceId) { mutableSetOf() }
        if (newNeighbors.size >= maxDegree) return
        for (existingId in adjacency.keys.sorted()) {
            if (existingId == deviceId) continue
            val existingNeighbors = adjacency.getOrPut(existingId) { mutableSetOf() }
            if (existingNeighbors.size < maxDegree && newNeighbors.size < maxDegree) {
                connect(existingId, deviceId)
                break
            }
        }
    }

    /**
     * Send envelope from senderId only to its configured neighbors.
     */
    fun send(senderId: Int, envelope: OutboundEnvelope<Int>) {
        val neighborsIds = adjacency[senderId].orEmpty()   // only explicit neighbors

        neighborsIds.forEach { neighborId ->
            val message = envelope.prepareMessageFor(senderId)
            val neighborMessages = messageBuffer.getOrPut(neighborId) { mutableMapOf() }
            neighborMessages[senderId] = message
        }
    }

    /**
     * Return messages directed to receiverId.
     * Neighbors are exactly those who have sent a message.
     */
    fun receiveMessageFor(receiverId: Int): NeighborsData<Int> = object : NeighborsData<Int> {
        private val neighborDeliverableMessages by lazy { messageBuffer[receiverId] ?: emptyMap() }
        override val neighbors: Set<Int> get() = neighborDeliverableMessages.keys

        @Suppress("UNCHECKED_CAST")
        override fun <Value> dataAt(path: Path, dataSharingMethod: DataSharingMethod<Value>): Map<Int, Value> =
            neighborDeliverableMessages
                .mapValues { it.value.sharedData.getOrElse(path) { NoValue } as Value }
                .filter { it.value != NoValue }
    }

    fun getConnectionsOf(nodeId: Int): Set<Int> = adjacency[nodeId]?.toSet() ?: emptySet()

    private object NoValue
}
