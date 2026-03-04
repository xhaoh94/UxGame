#import "BridgeManager.h"
#import <objc/runtime.h>

// Unity 发送消息的 C 函数声明
extern void UnitySendMessage(const char *obj, const char *method, const char *msg);

@interface BridgeManager ()
@property (nonatomic, strong) NSMutableDictionary<NSString *, id<IBridgePlugin>> *plugins;
@end

@implementation BridgeManager

+ (instancetype)sharedInstance {
    static BridgeManager *instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instance = [[BridgeManager alloc] init];
        instance.plugins = [NSMutableDictionary dictionary];
    });
    return instance;
}

- (void)initBridge:(NSString *)receiverName receiverMethod:(NSString *)receiverMethod {
    self.receiverName = receiverName;
    self.receiverMethod = receiverMethod;
    NSLog(@"[iOS BridgeManager] Initialized with receiver: %@_%@", receiverName,receiverMethod);
}

- (BOOL)registerPlugin:(NSString *)pluginClassName jsonParams:(NSString *)jsonParams {
    Class PluginClass = NSClassFromString(pluginClassName);
    if (PluginClass == nil) {
        NSLog(@"[iOS BridgeManager] Error: Cannot find class %@", pluginClassName);
        return NO;
    }
    
    id<IBridgePlugin> plugin = [[PluginClass alloc] init];
    if ([plugin conformsToProtocol:@protocol(IBridgePlugin)]) {
        NSString *name = [plugin pluginName];
        
        NSDictionary *paramsDict = nil;
        if (jsonParams && jsonParams.length > 0) {
            NSData *data = [jsonParams dataUsingEncoding:NSUTF8StringEncoding];
            paramsDict = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
        }
        
        [plugin onInit:paramsDict];
        self.plugins[name] = plugin;
        NSLog(@"[iOS BridgeManager] Registered plugin: %@", name);
        return YES;
    }
    return NO;
}

- (BOOL)hasPlugin:(NSString *)pluginName {
    return self.plugins[pluginName] != nil;
}

- (void)unregisterPlugin:(NSString *)pluginName {
    if (self.plugins[pluginName]) {
        [self.plugins removeObjectForKey:pluginName];
        NSLog(@"[iOS BridgeManager] Unregistered plugin: %@", pluginName);
    }
}
- (NSString *)callPlugin:(NSString *)pluginName method:(NSString *)methodName jsonParams:(NSString *)jsonParams {
    id<IBridgePlugin> plugin = self.plugins[pluginName];
    if (!plugin) {
        return @"{\"code\":-1,\"msg\":\"Plugin not found\"}";
    }
    
    NSDictionary *paramsDict = nil;
    if (jsonParams && jsonParams.length > 0) {
        NSData *data = [jsonParams dataUsingEncoding:NSUTF8StringEncoding];
        paramsDict = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
    }
    
    return [plugin execute:methodName params:paramsDict];
}


- (void)sendEvent:(NSString *)pluginName eventName:(NSString *)eventName data:(NSString *)jsonData {
    NSDictionary *dict = @{
        @"plugin": pluginName ?: @"",
        @"eventName": eventName ?: @"",
        @"data": jsonData ?: @"{}"
    };
    NSData *json = [NSJSONSerialization dataWithJSONObject:dict options:0 error:nil];
    NSString *msg = [[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding];
    
    if (self.receiverName) {
        UnitySendMessage([self.receiverName UTF8String], [self.receiverMethod UTF8String], [msg UTF8String]);
    }
}
- (BOOL)dispatchEvent_application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)options {
    BOOL result = YES;
    for (id<IBridgePlugin> plugin in self.plugins.allValues) {
        if ([plugin respondsToSelector:@selector(application:didFinishLaunchingWithOptions:)]) {
            result = result && [plugin application:application didFinishLaunchingWithOptions:options];
        }
    }
    return result;
}

- (void)dispatchEvent_applicationDidBecomeActive:(UIApplication *)application {
    for (id<IBridgePlugin> plugin in self.plugins.allValues) {
        if ([plugin respondsToSelector:@selector(applicationDidBecomeActive:)]) {
            [plugin applicationDidBecomeActive:application];
        }
    }
}

- (void)dispatchEvent_applicationWillResignActive:(UIApplication *)application {
    for (id<IBridgePlugin> plugin in self.plugins.allValues) {
        if ([plugin respondsToSelector:@selector(applicationWillResignActive:)]) {
            [plugin applicationWillResignActive:application];
        }
    }
}

- (void)dispatchEvent_applicationDidEnterBackground:(UIApplication *)application {
    for (id<IBridgePlugin> plugin in self.plugins.allValues) {
        if ([plugin respondsToSelector:@selector(applicationDidEnterBackground:)]) {
            [plugin applicationDidEnterBackground:application];
        }
    }
}

- (void)dispatchEvent_applicationWillEnterForeground:(UIApplication *)application {
    for (id<IBridgePlugin> plugin in self.plugins.allValues) {
        if ([plugin respondsToSelector:@selector(applicationWillEnterForeground:)]) {
            [plugin applicationWillEnterForeground:application];
        }
    }
}

- (void)dispatchEvent_applicationWillTerminate:(UIApplication *)application {
    for (id<IBridgePlugin> plugin in self.plugins.allValues) {
        if ([plugin respondsToSelector:@selector(applicationWillTerminate:)]) {
            [plugin applicationWillTerminate:application];
        }
    }
}

- (BOOL)dispatchEvent_application:(UIApplication *)app openURL:(NSURL *)url options:(NSDictionary<UIApplicationOpenURLOptionsKey, id> *)options {
    BOOL result = NO;
    for (id<IBridgePlugin> plugin in self.plugins.allValues) {
        if ([plugin respondsToSelector:@selector(application:openURL:options:)]) {
            result = result || [plugin application:app openURL:url options:options];
        }
    }
    return result;
}

@end

// ==========================================
// C-API (Bridge exported to Unity C# [DllImport])
// ==========================================

#if defined(__cplusplus)
extern "C" {
#endif

// 工具方法：复制字符串给 Unity
char* MakeStringCopy(const char* string) {
    if (string == NULL) return NULL;
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

void OC_Init(const char* receiverName, const char* receiverMethod) {
    NSString *name = [NSString stringWithUTF8String:receiverName ? receiverName : "SDKBridgeReceiver"];
    NSString *methodName = [NSString stringWithUTF8String:receiverMethod ? receiverMethod : "OnSdkCallback"];
    [[BridgeManager sharedInstance] initBridge:name receiverMethod:methodName];
}

bool OC_RegisterPlugin(const char* pluginName, const char* jsonParams) {
    if (!pluginName) return false;
    NSString *pName = [NSString stringWithUTF8String:pluginName];
    NSString *jParams = jsonParams ? [NSString stringWithUTF8String:jsonParams] : nil;
    return [[BridgeManager sharedInstance] registerPlugin:pName jsonParams:jParams];
}

bool OC_HasPlugin(const char* pluginName) {
    if (!pluginName) return false;
    NSString *pName = [NSString stringWithUTF8String:pluginName];
    return [[BridgeManager sharedInstance] hasPlugin:pName];
}

bool OC_UnRegisterPlugin(const char* pluginName) {
    if (!pluginName) return false;
    NSString *pName = [NSString stringWithUTF8String:pluginName];
    [[BridgeManager sharedInstance] unregisterPlugin:pName];
    return true;
}

const char* OC_Call(const char* pluginName, const char* methodName, const char* jsonParams) {
    if (!pluginName || !methodName) return NULL;
    NSString *pName = [NSString stringWithUTF8String:pluginName];
    NSString *mName = [NSString stringWithUTF8String:methodName];
    NSString *jParams = jsonParams ? [NSString stringWithUTF8String:jsonParams] : nil;
    
    NSString *result = [[BridgeManager sharedInstance] callPlugin:pName method:mName jsonParams:jParams];
    return MakeStringCopy([result UTF8String]);
}

void OC_FreeString(char* ptr) {
    if (ptr != NULL) {
        free(ptr);
    }
}

#if defined(__cplusplus)
}
#endif
