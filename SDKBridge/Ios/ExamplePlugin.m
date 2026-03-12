#import "ExamplePlugin.h"
#import "BridgeManager.h"
#import <UIKit/UIKit.h>
#import <UIKit/UIKit.h>

@interface ExamplePlugin ()
@property (nonatomic, strong) NSString *appKey;
@property (nonatomic, assign) BOOL isInitialized;
@end

@implementation ExamplePlugin

+ (void)load {
	//这里我启动就注册了插件，如果这个插件不需要启动就注册，也可以在unity侧寻找时机去注册
    [[BridgeManager sharedInstance] registerPlugin:NSStringFromClass(self) jsonParams:nil];
}

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {
    NSLog(@"[ExamplePlugin iOS] 拦截到 didFinishLaunchingWithOptions");
    return YES;
}

// 1. 实现 IBridgePlugin 的必选方法
#pragma mark - IBridgePlugin Protocol

- (NSString *)pluginName {
    return @"Example"; // 这个名字必须与 Unity 侧 Call("Example", ...) 的第一个参数一致
}

- (void)onInit:(NSDictionary *)params {
    self.isInitialized = YES;
    self.appKey = params[@"appKey"] ?: @"";
    
    NSLog(@"[ExamplePlugin iOS] 初始化完成. appKey = %@", self.appKey);
}

- (NSString *)execute:(NSString *)methodName params:(NSDictionary *)params {
    NSLog(@"[ExamplePlugin iOS] 执行方法: %@, 参数: %@", methodName, params);
    
    if ([methodName isEqualToString:@"getDeviceInfo"]) {
        return [self handleGetDeviceInfo];
    } 
    else if ([methodName isEqualToString:@"showToast"]) {
        return [self handleShowToast:params];
    } 
    else if ([methodName isEqualToString:@"doAsyncWork"]) {
        return [self handleAsyncWork:params];
    }
    
    // 对于未知方法
    return [self errorResultWithCode:-1 msg:[NSString stringWithFormat:@"未知方法: %@", methodName]];
}

// 2. 内部处理方法
#pragma mark - Method Handlers

/// 同步调用示例：获取设备信息
- (NSString *)handleGetDeviceInfo {
    NSString *brand = @"Apple";
    NSString *model = [[UIDevice currentDevice] model]; // 比如 "iPhone"
    NSString *systemVersion = [[UIDevice currentDevice] systemVersion];
    
    // 返回 JSON 格式的字符串
    NSString *info = [NSString stringWithFormat:@"{\"brand\":\"%@\",\"model\":\"%@\",\"version\":\"%@\"}", brand, model, systemVersion];
    
    return [self successResultWithData:info];
}

/// 同步调用示例：主线程操作 UI（在 iOS 中，我们用 UIAlertController 模拟 Toast）
- (NSString *)handleShowToast:(NSDictionary *)params {
    NSString *message = params[@"message"];
    if (!message || message.length == 0) {
        return [self errorResultWithCode:1 msg:@"缺少 message 参数"];
    }
    
    // UI 操作必须放在主线程执行
    dispatch_async(dispatch_get_main_queue(), ^{
        UIAlertController *alert = [UIAlertController alertControllerWithTitle:nil 
                                                                       message:message 
                                                                preferredStyle:UIAlertControllerStyleAlert];
        
        // 查找当前的根视图控制器
        UIViewController *rootVC = [UIApplication sharedApplication].keyWindow.rootViewController;
        if (rootVC) {
            [rootVC presentViewController:alert animated:YES completion:^{
                // 模拟 Toast 自动消失 (延迟1.5秒)
                dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(1.5 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
                    [alert dismissViewControllerAnimated:YES completion:nil];
                });
            }];
        }
    });
    
    return [self successResultWithData:nil];
}

/// 异步调用示例：模拟耗时操作，并通过事件推送结果
- (NSString *)handleAsyncWork:(NSDictionary *)params {
    NSNumber *delayNumber = params[@"delay"];
    int delayMs = delayNumber ? [delayNumber intValue] : 2000;
    
    NSLog(@"[ExamplePlugin iOS] 发起异步任务，延迟: %d 毫秒", delayMs);
    
    // 模拟在后台线程执行异步任务
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        // 延时
        usleep(delayMs * 1000); 
        
        NSTimeInterval timestamp = [[NSDate date] timeIntervalSince1970] * 1000;
        NSString *eventData = [NSString stringWithFormat:@"{\"result\":\"异步任务完成(iOS)\",\"timestamp\":%f}", timestamp];
        
        // 耗时任务完成后，通过 BridgeManager 的 sendEvent 向 Unity 发送事件
        [[BridgeManager sharedInstance] sendEvent:[self pluginName] 
                                        eventName:@"onAsyncWorkDone" 
                                             data:eventData];
    });
    
    // 立即向 C# 同步返回一个成功，告诉 Unity "任务已接管"
    return [self successResultWithData:@"{\"status\":\"accepted\"}"];
}

// 3. 辅助方法
#pragma mark - Helper

- (NSString *)successResultWithData:(NSString *)data {
    if (data) {
        return [NSString stringWithFormat:@"{\"code\":0,\"msg\":\"success\",\"data\":%@}", data];
    }
    return @"{\"code\":0,\"msg\":\"success\"}";
}

- (NSString *)errorResultWithCode:(int)code msg:(NSString *)msg {
    return [NSString stringWithFormat:@"{\"code\":%d,\"msg\":\"%@\"}", code, msg];
}

@end
