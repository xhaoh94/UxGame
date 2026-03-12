package com.sdkbridge.app;

import android.content.Intent;
import android.os.Bundle;
import android.widget.Button;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.AppCompatActivity;

import com.sdkbridge.core.BridgeManager;
import com.sdkbridge.core.UnityBridge;
import com.sdkbridge.plugin.example.ExamplePlugin;

/**
 * 测试Activity - 用于在独立App中测试桥接框架（脱离Unity环境）
 */
public class TestActivity extends AppCompatActivity {

    private TextView tvResult;

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_test);

        tvResult = findViewById(R.id.tv_result);

        Button btnInit = findViewById(R.id.btn_init);
        Button btnToast = findViewById(R.id.btn_toast);
        Button btnDeviceInfo = findViewById(R.id.btn_device_info);
        Button btnAsync = findViewById(R.id.btn_async);

        // 初始化桥接框架
        btnInit.setOnClickListener(v -> {
            BridgeManager.getInstance().init(this);
            BridgeManager.getInstance().registerPlugin(new ExamplePlugin());
            showResult("桥接框架初始化完成，ExamplePlugin 已注册");
        });

        // 测试 showToast
        btnToast.setOnClickListener(v -> {
            String result = UnityBridge.call("Example", "showToast",
                    "{\"message\":\"来自桥接框架的Toast！\"}");
            showResult("showToast 返回: " + result);
        });

        // 测试 getDeviceInfo
        btnDeviceInfo.setOnClickListener(v -> {
            String result = UnityBridge.call("Example", "getDeviceInfo", null);
            showResult("getDeviceInfo 返回:\n" + result);
        });

        // 测试异步调用
        btnAsync.setOnClickListener(v -> {
            String result = UnityBridge.call("Example", "doAsyncWork",
                    "{\"delay\":1000}");
            showResult("doAsyncWork 同步返回: " + result + "\n（异步结果将通过事件推送发送到Unity）");
        });
    }

    private void showResult(String text) {
        tvResult.setText(text);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, @Nullable Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        UnityBridge.onActivityResult(requestCode, resultCode, data);
    }

    @Override
    protected void onResume() {
        super.onResume();
        UnityBridge.onResume();
    }

    @Override
    protected void onPause() {
        super.onPause();
        UnityBridge.onPause();
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        UnityBridge.onDestroy();
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        UnityBridge.onNewIntent(intent);
    }

    @Override
    public void onRequestPermissionsResult(int requestCode,
                                           @NonNull String[] permissions,
                                           @NonNull int[] grantResults) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        UnityBridge.onRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
