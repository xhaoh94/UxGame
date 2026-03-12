# bridge-core 混淆规则
-keep class com.sdkbridge.core.** { *; }
-keep interface com.sdkbridge.core.plugin.IBridgePlugin { *; }
-keep class * implements com.sdkbridge.core.plugin.IBridgePlugin { *; }
