// bridge-core 模块 - 核心桥接层，打包为 aar 供 Unity 工程引用
plugins {
    id("com.android.library")
}

android {
    namespace = "com.sdkbridge.core"
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
    compileOnly(files("libs/unity-classes.jar")) // Unity 的 classes.jar，编译时依赖
    api("androidx.annotation:annotation:1.7.1")
}
