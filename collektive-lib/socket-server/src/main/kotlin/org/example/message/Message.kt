package org.example.message

import kotlinx.serialization.Serializable

/*--------------- INPUT TYPES -------------------*/

@Serializable
data class CreateSimulation(val nodeCount: Int, val maxDistance: Double)

@Serializable
data class SetSource(val nodeId: Int)

@Serializable
data class Step(val rid: Int, val stepCount: Int = 1)

@Serializable
data class NewPosition(val nodeId: Int, val x: Double, val y: Double, val z: Double)

/*--------------- OUTPUT TYPES -------------------*/

@Serializable
data class State(val rid: Int, val values: List<NodeState>)

@Serializable
data class NodeState(val value: Double, val neighbors: Set<Int>)
