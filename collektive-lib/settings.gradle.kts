plugins {
    id("org.gradle.toolchains.foojay-resolver-convention") version "1.0.0"
}

rootProject.name = "collektive-lib"
include("lib")
include("app")
include("socket-server")