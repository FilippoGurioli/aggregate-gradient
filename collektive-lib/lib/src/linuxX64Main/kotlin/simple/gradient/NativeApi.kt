package simple.gradient

import arrow.atomic.AtomicInt
import kotlinx.cinterop.*
import kotlin.experimental.ExperimentalNativeApi

private val nextHandle = AtomicInt(1)
private val engines = mutableMapOf<Int, CollektiveEngine>()

@OptIn(ExperimentalNativeApi::class)
@CName("create")
fun create(nodeCount: Int, maxDegree: Int): Int {
    val handle = nextHandle.addAndGet(1)
    val engine = CollektiveEngine(nodeCount, maxDegree)
    engines[handle] = engine
    return handle
}

@OptIn(ExperimentalNativeApi::class)
@CName("destroy")
fun destroy(handle: Int) {
    engines.remove(handle)
}

@OptIn(ExperimentalNativeApi::class)
@CName("set_source")
fun setSource(handle: Int, nodeId: Int, isSource: Boolean) {
    val engine = engines[handle] ?: return
    engine.setSource(nodeId, isSource)
}

@OptIn(ExperimentalNativeApi::class)
@CName("clear_sources")
fun clearSources(handle: Int) {
    val engine = engines[handle] ?: return
    engine.clearSources()
}

@OptIn(ExperimentalNativeApi::class)
@CName("step")
fun step(handle: Int, rounds: Int) {
    val engine = engines[handle] ?: return
    engine.stepMany(rounds)
}

@OptIn(ExperimentalNativeApi::class)
@CName("get_value")
fun getValue(handle: Int, nodeId: Int): Int {
    val engine = engines[handle] ?: return Int.MAX_VALUE
    return engine.getValue(nodeId)
}

@OptIn(ExperimentalNativeApi::class, ExperimentalForeignApi::class)
@CName("get_neighborhood")
fun getNeighborhood(
    handle: Int,
    nodeId: Int,
    outSize: CPointer<IntVar>
): CPointer<IntVar>? {
    val set = engines[handle]?.getNeighborhood(nodeId) ?: emptySet()
    val size = set.size
    outSize.pointed.value = size
    if (size == 0) {
        return null
    }
    val array: CPointer<IntVar> = nativeHeap.allocArray(size)
    var i = 0
    for (v in set) {
        array[i] = v
        i++
    }
    return array
}

@OptIn(ExperimentalNativeApi::class, ExperimentalForeignApi::class)
@CName("free_neighborhood")
fun freeNeighborhood(ptr: CPointer<IntVar>?) {
    if (ptr != null) {
        nativeHeap.free(ptr)
    }
}