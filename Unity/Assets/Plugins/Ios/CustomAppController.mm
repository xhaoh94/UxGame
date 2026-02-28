#import "UnityAppController.h"

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

- (BOOL)application:(UIApplication *)application
    didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {

    NSLog(@"[CustomAppController] didFinishLaunchingWithOptions 开始");

    // TODO: 在调用 super 之前初始化需要尽早启动的 SDK
    // 示例：
    // [ThirdPartySDK initWithAppKey:@"your-app-key"];

    BOOL result = [super application:application didFinishLaunchingWithOptions:launchOptions];

    // TODO: 在调用 super 之后初始化依赖 Unity 引擎的 SDK
    // 示例：
    // [AnalyticsSDK startWithConfiguration:config];

    NSLog(@"[CustomAppController] didFinishLaunchingWithOptions 完成");
    return result;
}

- (void)applicationDidBecomeActive:(UIApplication *)application {
    [super applicationDidBecomeActive:application];
    NSLog(@"[CustomAppController] applicationDidBecomeActive");

    // TODO: 在此处处理应用激活回调（如有需要）
}

- (void)applicationWillResignActive:(UIApplication *)application {
    [super applicationWillResignActive:application];
    NSLog(@"[CustomAppController] applicationWillResignActive");

    // TODO: 在此处处理应用即将进入后台回调（如有需要）
}

- (void)applicationDidEnterBackground:(UIApplication *)application {
    [super applicationDidEnterBackground:application];
    NSLog(@"[CustomAppController] applicationDidEnterBackground");

    // TODO: 在此处处理应用进入后台回调（如有需要）
}

- (void)applicationWillEnterForeground:(UIApplication *)application {
    [super applicationWillEnterForeground:application];
    NSLog(@"[CustomAppController] applicationWillEnterForeground");

    // TODO: 在此处处理应用即将回到前台回调（如有需要）
}

- (void)applicationWillTerminate:(UIApplication *)application {
    [super applicationWillTerminate:application];
    NSLog(@"[CustomAppController] applicationWillTerminate");

    // TODO: 在此处执行 SDK 清理工作（如有需要）
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
