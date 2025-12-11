package org.example

import simple.gradient.CollektiveEngine
import simple.gradient.jvmCreate
import simple.gradient.jvmDestroy
import simple.gradient.jvmGetValue
import simple.gradient.jvmSetSource
import simple.gradient.jvmStep

fun main() {
    val engine = CollektiveEngine(10)

    engine.setSource(0, true)

    println("Connections of source (0): ${engine.getNeighborhood(0)}")
}

fun main2() {
    val engine = CollektiveEngine(10)

    engine.setSource(0, true)

    repeat(10) { round ->
        println("Round $round")
        engine.stepOnce()
        (0 until 10).forEach { id -> println("$id -> ${engine.getValue(id)}")
        }
        println()
    }
}

fun main3() {
    val handle = jvmCreate(3, 3)
    jvmSetSource(handle, 0,true)
    for (r in 0 until 1)
    {
        println("Round $r")
        jvmStep(handle, 1)
        for (id in 0 until 3)
        {
            val value = jvmGetValue(handle, id)
            println("  Device $id -> $value")
        }
        println()
    }

    jvmDestroy(handle)
}
