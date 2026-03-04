#import "UnityAppController.h"
#import "BridgeManager.h"

/// 自定义 AppController
///
/// 通过 IMPL_APP_CONTROLLER_SUBCLASS 宏注册为 Unity 的 AppController 子类，
/// 在应用启动及生命周期事件中提供原生 SDK 初始化入口。
///
/// 使用方法：
///   在对应的生命周期方法中按需添加各 SDK 的初始化 / 回调代码。
@interface CustomAppController : UnityAppController
@end

@implementation CustomAppController

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {
    NSLog(@"[CustomAppController] didFinishLaunchingWithOptions 开始");
    BOOL pluginResult = [[BridgeManager sharedInstance] dispatchEvent_application:application didFinishLaunchingWithOptions:launchOptions];
    BOOL result = [super application:application didFinishLaunchingWithOptions:launchOptions];
    if (pluginResult) { result = YES; }
    NSLog(@"[CustomAppController] didFinishLaunchingWithOptions 完成");
    return result;
}

- (void)applicationDidBecomeActive:(UIApplication *)application {
    [super applicationDidBecomeActive:application];
    [[BridgeManager sharedInstance] dispatchEvent_applicationDidBecomeActive:application];
}

- (void)applicationWillResignActive:(UIApplication *)application {
    [super applicationWillResignActive:application];
    [[BridgeManager sharedInstance] dispatchEvent_applicationWillResignActive:application];
}

- (void)applicationDidEnterBackground:(UIApplication *)application {
    [super applicationDidEnterBackground:application];
    [[BridgeManager sharedInstance] dispatchEvent_applicationDidEnterBackground:application];
}

- (void)applicationWillEnterForeground:(UIApplication *)application {
    [super applicationWillEnterForeground:application];
    [[BridgeManager sharedInstance] dispatchEvent_applicationWillEnterForeground:application];
}

- (void)applicationWillTerminate:(UIApplication *)application {
    [super applicationWillTerminate:application];
    [[BridgeManager sharedInstance] dispatchEvent_applicationWillTerminate:application];
}

- (BOOL)application:(UIApplication *)app openURL:(NSURL *)url options:(NSDictionary<UIApplicationOpenURLOptionsKey, id> *)options {
    BOOL result = [super application:app openURL:url options:options];
    BOOL pluginResult = [[BridgeManager sharedInstance] dispatchEvent_application:app openURL:url options:options];
    return result || pluginResult;
}

- (void)application:(UIApplication *)application
    didRegisterForRemoteNotificationsWithDeviceToken:(NSData *)deviceToken {
    [super application:application didRegisterForRemoteNotificationsWithDeviceToken:deviceToken];
    NSLog(@"[CustomAppController] didRegisterForRemoteNotificationsWithDeviceToken");

    // TODO: 在此处将 deviceToken 传递给推送 SDK（如有需要）
}

- (void)application:(UIApplication *)application
    didFailToRegisterForRemoteNotificationsWithError:(NSError *)error {
    [super application:application didFailToRegisterForRemoteNotificationsWithError:error];
    NSLog(@"[CustomAppController] didFailToRegisterForRemoteNotificationsWithError: %@", error);

    // TODO: 在此处处理推送注册失败（如有需要）
}

@end

IMPL_APP_CONTROLLER_SUBCLASS(CustomAppController)
