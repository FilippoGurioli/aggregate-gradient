package org.example.message

import kotlinx.serialization.Serializable

/*--------------- INPUT TYPES -------------------*/

@Serializable
data class CreateSimulation(val nodeCount: Int, val maxDistance: Double)

@Serializable
data class SetSource(val nodeId: Int)

@Serializable
data class Step(val stepCount: Int = 1)

@Serializable
data class NewPosition(val nodeId: Int, val x: Double, val y: Double, val z: Double)

@Serializable
data class NeighborReq(val nodeId: Int)

/*--------------- OUTPUT TYPES -------------------*/

@Serializable
data class State(val values: List<Double>)

@Serializable
data class Neighbor(val neighbors: Set<Int>)
