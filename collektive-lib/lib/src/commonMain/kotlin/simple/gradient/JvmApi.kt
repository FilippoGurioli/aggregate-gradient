package simple.gradient

import kotlin.concurrent.atomics.AtomicInt
import kotlin.concurrent.atomics.ExperimentalAtomicApi

@OptIn(ExperimentalAtomicApi::class)
private val nextHandle = AtomicInt(1)
private val engines = mutableMapOf<Int, CollektiveEngine>()

@OptIn(ExperimentalAtomicApi::class)
fun jvmCreate(nodeCount: Int, maxDegree: Int): Int {
    println("new engine creation requested")
    val handle = nextHandle.addAndFetch(1)
    val engine = CollektiveEngine(nodeCount, maxDegree)
    engines[handle] = engine
    println("total engines: $engines")
    println("new engine created")
    return handle
}

fun jvmDestroy(handle: Int) {
    println("engine destruction requested")
    println("total size: " + engines.size)
    engines.remove(handle)
    println("engine destroyed")
}

fun jvmSetSource(handle: Int, nodeId: Int, isSource: Boolean) {
    println("request to set as source the node $nodeId for the engine $handle")
    println("engines size: " + engines.size)
    println("engine nodes: " + engines[handle]?.nodeCount)
    val engine = engines[handle] ?: return
    engine.setSource(nodeId, isSource)
    println("now sources are: " + engine.sources)
}

fun jvmClearSources(handle: Int) {
    val engine = engines[handle] ?: return
    engine.clearSources()
}

fun jvmStep(handle: Int, rounds: Int) {
    val engine = engines[handle] ?: return
    engine.stepMany(rounds)
}

fun jvmGetValue(handle: Int, nodeId: Int): Int {
    println("request value for node $nodeId in engine $handle")
    println("engines: $engines")
    println("value: ${engines[handle]?.getValue(nodeId)}")
    val engine = engines[handle] ?: return Int.MAX_VALUE
    return engine.getValue(nodeId)
}

fun jvmGetNeighborhood(handle: Int, nodeId: Int): Set<Int>? = engines[handle]?.getNeighborhood(nodeId)
