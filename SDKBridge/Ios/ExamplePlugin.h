#import <Foundation/Foundation.h>
#import "BridgeManager.h"

/// <summary>
/// 示例插件 (iOS) - 演示如何编写一个桥接插件
/// 
/// 这个示例展示了完整的插件结构，包括：
/// 1. 插件初始化
/// 2. 同步方法调用
/// 3. 在主线程操作 UI
/// 4. 异步方法调用（通过事件主动推送结果给 Unity）
/// </summary>
@interface ExamplePlugin : NSObject <IBridgePlugin>

@end
