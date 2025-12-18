package time

import java.io.BufferedWriter
import java.io.File
import java.io.FileWriter
import java.util.concurrent.ConcurrentLinkedQueue
import java.util.concurrent.atomic.AtomicBoolean

class CsvFileSink(
    file: File = File("timings.csv"),
    private val flushEvery: Int = 200
) : SampleSink {
    private val queue = ConcurrentLinkedQueue<Triple<Long, String, Long>>() // t,id,dur
    private val running = AtomicBoolean(true)
    private val writerThread: Thread

    init {
        BufferedWriter(FileWriter(file, false)).use { w ->
            w.write("t_ns,id,duration_ns\n")
        }

        writerThread = Thread {
            BufferedWriter(FileWriter(file, true)).use { w ->
                var count = 0
                while (running.get() || queue.isNotEmpty()) {
                    val item = queue.poll()
                    if (item == null) {
                        Thread.sleep(2)
                        continue
                    }
                    val (t, id, ns) = item
                    w.write("$t,$id,$ns\n")
                    count++
                    if (count % flushEvery == 0) w.flush()
                }
                w.flush()
            }
        }.apply {
            isDaemon = true
            name = "CsvFileSink-Writer"
            start()
        }
    }

    override fun onSample(id: String, tNs: Long, durationNs: Long) {
        queue.add(Triple(tNs, id, durationNs))
    }

    fun close() {
        running.set(false)
        writerThread.join(2_000)
    }
}
