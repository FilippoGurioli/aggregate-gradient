package time

import kotlin.math.ceil

interface SampleSink {
    fun onSample(id: String, tNs: Long, durationNs: Long)
}

class TimeMeasurer(
    private val sink: SampleSink? = null
) {
    private val starts = mutableMapOf<String, Long>()
    private val samples = mutableMapOf<String, MutableList<Long>>()
    private val t0 = nanoTime()

    fun start(id: String) {
        starts[id] = nanoTime()
    }

    fun stop(id: String) {
        val start = starts.remove(id) ?: return
        val end = nanoTime()
        val dur = end - start

        samples.getOrPut(id) { mutableListOf() }.add(dur)
        sink?.onSample(id, end - t0, dur)
    }

    fun getSamples(id: String): List<Long> = samples[id]?.toList().orEmpty()

    fun summary(id: String): Summary? {
        val v = samples[id] ?: return null
        if (v.isEmpty()) return null
        val arr = v.sorted()
        val mean = v.average()
        return Summary(
            n = v.size,
            meanNs = mean,
            p50Ns = percentileSorted(arr, 0.50),
            p95Ns = percentileSorted(arr, 0.95),
            p99Ns = percentileSorted(arr, 0.99),
        )
    }

    fun summaries(): Map<String, Summary> =
        samples.keys.sorted().mapNotNull { id -> summary(id)?.let { id to it } }.toMap()

    data class Summary(
        val n: Int,
        val meanNs: Double,
        val p50Ns: Long,
        val p95Ns: Long,
        val p99Ns: Long
    )

    private fun percentileSorted(sorted: List<Long>, p: Double): Long {
        val idx = ceil(p * sorted.size).toInt().coerceIn(1, sorted.size) - 1
        return sorted[idx]
    }
}
