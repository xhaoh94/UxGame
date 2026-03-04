#if UNITY_IOS && Test
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

public class TestChannel : IXCodeProcessor {
    public void Process(BuildContext context) {
        context.Project.AddBuildProperty(context.MainTarget, "OTHER_LDFLAGS", "-Wl -ld_classic");
        
        BuildUtils.CopyAndAddDirectory(context, "../Datas/iosSDKChannel/tanwan_overseas/SDK", "SDK", context.FrameworkTarget);
        string[] fms = { "MiddleModel.h", "MiddleGround.h", "libMiddleControl.a" };
        foreach(var f in fms) {
            string guid = BuildUtils.AddFileSafely(context, "SDK/" + f, "SDK/" + f);
            context.Project.AddFileToBuild(context.FrameworkTarget, guid);
        }
        string[] plists = { "configure.plist", "GoogleService-Info.plist" };
        foreach(var p in plists) {
            string guid = BuildUtils.AddFileSafely(context, "SDK/" + p, "SDK/" + p);
            context.Project.AddFileToBuild(context.MainTarget, guid);
        }

        // Recursive Ext Logic
        string extPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Datas/iosSDKChannel/tanwan_overseas/SDK/Ext");
        if (Directory.Exists(extPath)) {
            foreach (var dir in Directory.GetDirectories(extPath, "*.*", SearchOption.TopDirectoryOnly)) {
                string dirName = Path.GetFileName(dir);
                foreach (var file in Directory.GetDirectories(dir, "*.*", SearchOption.TopDirectoryOnly)) {
                    string fileName = Path.GetFileName(file);
                    string tmpDir = $"SDK/Ext/{dirName}/{fileName}";
                    string guid = BuildUtils.AddFileSafely(context, tmpDir, tmpDir);
                    if (Path.GetExtension(file) == ".bundle") context.Project.AddFileToBuild(context.MainTarget, guid);
                    else context.Project.AddFileToBuild(context.FrameworkTarget, guid);
                }
            }
        }

        BuildUtils.AddSystemLibraries(context, new[] { "libbz2.tbd", "libc++.tbd", "libc++abi.tbd", "libiconv.tbd", "libresolv.9.tbd", "libresolv.tbd", "libsqlite3.tbd", "libxml2.tbd", "libz.tbd" });
        BuildUtils.AddSystemFrameworks(context, new[] { "Accelerate.framework", "AdServices.framework", "AdSupport.framework", "AppTrackingTransparency.framework", "AudioToolBox.framework", "AVFoundation.framework", "CFNetwork.framework", "CoreGraphics.framework", "CoreImage.framework", "CoreLocation.framework", "CoreMedia.framework", "CoreML.framework", "CoreMotion.framework", "CoreTelephony.framework", "DeviceCheck.framework", "iAd.framework", "ImageIO.framework", "JavaScriptCore.framework", "LocalAuthentication.framework", "MapKit.framework", "MediaPlayer.framework", "MessageUI.framework", "MobileCoreServices.framework", "QuartzCore.framework", "SafariServices.framework", "Security.framework", "SystemConfiguration.framework", "WebKit.framework" });

        // Localization
        ApplyLocalization(context);

        // Overwrite Info.plist logic
        string overwritePlist = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Datas/iosSDKChannel/tanwan_overseas/Info.plist");
        if(File.Exists(overwritePlist)) {
            context.InfoPlist.ReadFromFile(overwritePlist); 
        }
        context.InfoPlist.root.SetString("CFBundleDisplayName", "初心者傳説：夢境大冒險");
        FillDefaultDescriptions(context.InfoPlist.root);

        context.Project.SetBuildProperty(context.MainTarget, "PRODUCT_BUNDLE_IDENTIFIER", "com.chuxin.ios");
        BuildUtils.CopyAndAddDirectory(context, "../Datas/iosSDKChannel/tanwan_overseas/Code", "Libraries/Plugins/Ios/", context.FrameworkTarget, false, true);

        // Swift Bridge
        string swiftPath = Path.Combine(context.PathToBuiltProject, "SwiftBridge.swift");
        if(!File.Exists(swiftPath)) File.WriteAllText(swiftPath, "// Unity Auto Generated\nimport Foundation\n");
        string swiftGuid = BuildUtils.AddFileSafely(context, "SwiftBridge.swift", "SwiftBridge.swift");
        context.Project.AddFileToBuild(context.FrameworkTarget, swiftGuid);
        context.Project.SetBuildProperty(context.MainTarget, "SWIFT_VERSION", "5.0");
        context.Project.SetBuildProperty(context.FrameworkTarget, "SWIFT_VERSION", "5.0");
        context.Project.SetBuildProperty(context.FrameworkTarget, "ENABLE_SWIFT", "YES");
    }

    private void ApplyLocalization(BuildContext context) {
        var lanDic = new Dictionary<string, Dictionary<string, string>> {
            { "en", new Dictionary<string, string> { { "NSCameraUsageDescription","If not allowed, the camera function cannot be used." }, { "NSLocationWhenInUseUsageDescription","If not allowed, the location function cannot be used." }, { "NSMicrophoneUsageDescription","If not allowed, the microphone function cannot be used." }, { "NSPhotoLibraryUsageDescription","DO you allow this app to access your photo album?" }, { "NSUserTrackingUsageDescription","Obtaining identfiers for better activity recommendations" } } },
            { "English", new Dictionary<string, string> { { "NSCameraUsageDescription","If not allowed, the camera function cannot be used." }, { "NSLocationWhenInUseUsageDescription","If not allowed, the location function cannot be used." }, { "NSMicrophoneUsageDescription","If not allowed, the microphone function cannot be used." }, { "NSPhotoLibraryUsageDescription","DO you allow this app to access your photo album?" }, { "NSUserTrackingUsageDescription","Obtaining identfiers for better activity recommendations" } } },
            { "zh-HK", new Dictionary<string, string> { { "NSCameraUsageDescription","如果禁止將無法使用相機功能" }, { "NSLocationWhenInUseUsageDescription","如果禁止將無法使用地理位置功能" }, { "NSMicrophoneUsageDescription","如果禁止將無法使用麥克風功能" }, { "NSPhotoLibraryUsageDescription","是否允許此App訪問你的相冊" }, { "NSUserTrackingUsageDescription","獲取標識符爲了更好的推薦活動" } } },
            { "zh-Hant", new Dictionary<string, string> { { "NSCameraUsageDescription","如果禁止將無法使用相機功能" }, { "NSLocationWhenInUseUsageDescription","如果禁止將無法使用地理位置功能" }, { "NSMicrophoneUsageDescription","如果禁止將無法使用麥克風功能" }, { "NSPhotoLibraryUsageDescription","是否允許此App訪問你的相冊" }, { "NSUserTrackingUsageDescription","獲取標識符爲了更好的推薦活動" } } },
            { "zh-Hans", new Dictionary<string, string> { { "NSCameraUsageDescription","如果禁止将无法使用相机功能" }, { "NSLocationWhenInUseUsageDescription","如果禁止将无法使用地理位置功能" }, { "NSMicrophoneUsageDescription","如果禁止将无法使用麦克风功能" }, { "NSPhotoLibraryUsageDescription","是否允许此App访问你的相册？" }, { "NSUserTrackingUsageDescription","获取标识符为了更好的推荐活动" } } }
        };

        foreach(var kv in lanDic) {
            string lprojPath = Path.Combine(context.PathToBuiltProject, $"{kv.Key}.lproj");
            Directory.CreateDirectory(lprojPath);
            
            StringBuilder sb = new StringBuilder();
            foreach(var s in kv.Value) sb.AppendLine($"\"{s.Key}\" = \"{s.Value.Replace("\"", "\\\"")}\";");
            
            string filePath = Path.Combine(lprojPath, "InfoPlist.strings");
            File.WriteAllText(filePath, sb.ToString(), Encoding.Unicode);
            
            string guid = BuildUtils.AddFileSafely(context, Path.Combine($"{kv.Key}.lproj", "InfoPlist.strings"), Path.Combine($"{kv.Key}.lproj", "InfoPlist.strings"));
            context.Project.AddFileToBuild(context.MainTarget, guid);
        }
    }
    
    private void FillDefaultDescriptions(PlistElementDict root) {
        string[] keys = { "NSCameraUsageDescription", "NSLocationWhenInUseUsageDescription", "NSMicrophoneUsageDescription", "NSPhotoLibraryUsageDescription", "NSUserTrackingUsageDescription" };
        foreach(var k in keys) if(!root.values.ContainsKey(k)) root.SetString(k, "Description needed");
    }
}
#endif
