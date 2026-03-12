// bridge-plugin-example 模块 - 示例插件，演示如何接入第三方SDK
plugins {
    id("com.android.library")
}

android {
    namespace = "com.sdkbridge.plugin.example"
    compileSdk = property("COMPILE_SDK").toString().toInt()

    defaultConfig {
        minSdk = property("MIN_SDK").toString().toInt()
        consumerProguardFiles("consumer-rules.pro")
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_1_8
        targetCompatibility = JavaVersion.VERSION_1_8
    }
}

dependencies {
    // 依赖核心桥接模块
    implementation(project(":bridge-core"))

    // 在这里添加第三方SDK的依赖
    // implementation("com.example.thirdparty:sdk:1.0.0")
}
