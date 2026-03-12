// app 模块 - 用于本地测试桥接框架（不参与aar打包）
plugins {
    id("com.android.application")
}

android {
    namespace = "com.sdkbridge.app"
    compileSdk = property("COMPILE_SDK").toString().toInt()

    defaultConfig {
        applicationId = "com.sdkbridge.app"
        minSdk = property("MIN_SDK").toString().toInt()
        targetSdk = property("TARGET_SDK").toString().toInt()
        versionCode = property("BRIDGE_VERSION_CODE").toString().toInt()
        versionName = property("BRIDGE_VERSION_NAME").toString()
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
    implementation(project(":bridge-core"))
    implementation(project(":bridge-plugin-example"))
    implementation("androidx.appcompat:appcompat:1.6.1")
}
