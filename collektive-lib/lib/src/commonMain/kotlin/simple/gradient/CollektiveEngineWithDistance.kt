package simple.gradient

import it.unibo.collektive.Collektive
import kotlin.Double.Companion.POSITIVE_INFINITY

class CollektiveEngineWithDistance(nodeCount: Int, maxDistance: Double) {
    private val networkManager = NetworkManagerWithDistance(maxDistance)
    val sources = mutableSetOf<Int>()

    private val devices: List<Collektive<Node, Double>>
    private val lastValues: Array<Double> = Array(nodeCount) { POSITIVE_INFINITY }

    init {
        devices = (0 until nodeCount).map { id ->
            val node = Node(id, Position.origin())
            val network = NetworkWithDistance(networkManager, node)
            Collektive(node, network) {
                val value = gradient(localId.id in sources)
                lastValues[localId.id] = value
                value
            }
        }
        with (networkManager) {
            computeTopology()
            pruneBuffers()
        }
    }

    fun updateNodePosition(nodeId: Int, position: Position) {
        devices.first { d -> d.localId.id == nodeId }.localId.position = position
        networkManager.updatePosition(nodeId, position)
    }

    fun setSource(nodeId: Int, isSource: Boolean) {
        if (isSource) sources.add(nodeId) else sources.remove(nodeId)
    }

    fun clearSources() {
        sources.clear()
    }

    fun stepOnce() {
        with (networkManager) {
            computeTopology()
            pruneBuffers()
        }
        devices.reversed().forEach { it.cycle() }
    }

    fun stepMany(rounds: Int) {
        repeat(rounds) { stepOnce() }
    }

    fun getValue(nodeId: Int): Double = lastValues[nodeId]

    fun getValues(): List<Double> = lastValues.toList()

    fun getNeighborhood(nodeId: Int): Set<Int> = networkManager.getNeighbors(nodeId)
}