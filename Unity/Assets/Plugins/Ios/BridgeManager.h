#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

/// <summary>
/// iOS 端插件接口协议
/// </summary>
@protocol IBridgePlugin <NSObject>

@required
- (NSString *)pluginName;
- (void)onInit:(NSDictionary *)params;
- (NSString *)execute:(NSString *)methodName params:(NSDictionary *)params;

@optional
- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions;
- (void)applicationDidBecomeActive:(UIApplication *)application;
- (void)applicationWillResignActive:(UIApplication *)application;
- (void)applicationDidEnterBackground:(UIApplication *)application;
- (void)applicationWillEnterForeground:(UIApplication *)application;
- (void)applicationWillTerminate:(UIApplication *)application;
- (BOOL)application:(UIApplication *)app openURL:(NSURL *)url options:(NSDictionary<UIApplicationOpenURLOptionsKey, id> *)options;

@end

/// <summary>
/// iOS 端桥接管理器
/// </summary>
@interface BridgeManager : NSObject

@property (nonatomic, strong) NSString *receiverName;
@property (nonatomic, strong) NSString *receiverMethod;

+ (instancetype)sharedInstance;

- (void)initBridge:(NSString *)receiverName receiverMethod:(NSString *)receiverMethod;
- (BOOL)registerPlugin:(NSString *)pluginName jsonParams:(NSString *)jsonParams;
- (BOOL)hasPlugin:(NSString *)pluginName;
- (void)unregisterPlugin:(NSString *)pluginName;
- (NSString *)callPlugin:(NSString *)pluginName method:(NSString *)methodName jsonParams:(NSString *)jsonParams;

- (BOOL)dispatchEvent_application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)options;
- (void)dispatchEvent_applicationDidBecomeActive:(UIApplication *)application;
- (void)dispatchEvent_applicationWillResignActive:(UIApplication *)application;
- (void)dispatchEvent_applicationDidEnterBackground:(UIApplication *)application;
- (void)dispatchEvent_applicationWillEnterForeground:(UIApplication *)application;
- (void)dispatchEvent_applicationWillTerminate:(UIApplication *)application;
- (BOOL)dispatchEvent_application:(UIApplication *)app openURL:(NSURL *)url options:(NSDictionary<UIApplicationOpenURLOptionsKey, id> *)options;
// 向Unity发送消息
- (void)sendEvent:(NSString *)pluginName eventName:(NSString *)eventName data:(NSString *)jsonData;

@end
