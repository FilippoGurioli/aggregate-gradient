package simple.gradient

import arrow.atomic.AtomicInt
import kotlinx.cinterop.*
import kotlin.Double.Companion.POSITIVE_INFINITY
import kotlin.experimental.ExperimentalNativeApi

private val nextHandle = AtomicInt(1)
private val engines = mutableMapOf<Int, CollektiveEngineWithDistance>()

@OptIn(ExperimentalNativeApi::class)
@CName("create_with_distance")
fun createWithDistance(nodeCount: Int, maxDistance: Double): Int {
    val handle = nextHandle.addAndGet(1)
    val engine = CollektiveEngineWithDistance(nodeCount, maxDistance)
    engines[handle] = engine
    return handle
}

@OptIn(ExperimentalNativeApi::class)
@CName("destroy_with_distance")
fun destroyWithDistance(handle: Int) {
    engines.remove(handle)
}

@OptIn(ExperimentalNativeApi::class)
@CName("set_source_with_distance")
fun setSourceWithDistance(handle: Int, nodeId: Int, isSource: Boolean) {
    val engine = engines[handle] ?: return
    engine.setSource(nodeId, isSource)
}

@OptIn(ExperimentalNativeApi::class)
@CName("clear_sources_with_distance")
fun clearSourcesWithDistance(handle: Int) {
    val engine = engines[handle] ?: return
    engine.clearSources()
}

@OptIn(ExperimentalNativeApi::class)
@CName("step_with_distance")
fun stepWithDistance(handle: Int, rounds: Int) {
    val engine = engines[handle] ?: return
    engine.stepMany(rounds)
}

@OptIn(ExperimentalNativeApi::class)
@CName("get_value_with_distance")
fun getValueWithDistance(handle: Int, nodeId: Int): Double {
    val engine = engines[handle] ?: return POSITIVE_INFINITY
    return engine.getValue(nodeId)
}

@OptIn(ExperimentalNativeApi::class, ExperimentalForeignApi::class)
@CName("get_neighborhood_with_distance")
fun getNeighborhoodWithDistance(
    handle: Int,
    nodeId: Int,
    outSize: CPointer<IntVar>
): CPointer<IntVar>? {
    val engine = engines[handle]
    if (engine == null) {
        outSize.pointed.value = 0
        return null
    }

    val neighbors: List<Int> =
        engine.getNeighborhood(nodeId).toList()

    outSize.pointed.value = neighbors.size
    if (neighbors.isEmpty()) return null

    val ptr = nativeHeap.allocArray<IntVar>(neighbors.size)
    for (i in neighbors.indices) {
        ptr[i] = neighbors[i]
    }
    return ptr
}

@OptIn(ExperimentalNativeApi::class, ExperimentalForeignApi::class)
@CName("free_neighborhood_with_distance")
fun freeNeighborhoodWithDistance(ptr: CPointer<IntVar>?) {
    if (ptr != null) {
        nativeHeap.free(ptr)
    }
}

@OptIn(ExperimentalNativeApi::class, ExperimentalForeignApi::class)
@CName("update_position")
fun updatePosition(handle: Int, nodeId: Int, x: Double, y: Double, z: Double) {
    engines[handle]?.updateNodePosition(nodeId, Position(x,y,z))
}
