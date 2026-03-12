package com.sdkbridge.core.util;

import android.os.Bundle;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.Iterator;

/**
 * JSON工具类 - 提供JSON与Bundle之间的转换
 */
public class JsonHelper {

    private static final String TAG = "JsonHelper";

    /**
     * JSON字符串转Bundle
     */
    @NonNull
    public static Bundle jsonToBundle(@Nullable String json) {
        Bundle bundle = new Bundle();
        if (json == null || json.isEmpty()) {
            return bundle;
        }
        try {
            JSONObject obj = new JSONObject(json);
            Iterator<String> keys = obj.keys();
            while (keys.hasNext()) {
                String key = keys.next();
                Object value = obj.get(key);
                if (value instanceof String) {
                    bundle.putString(key, (String) value);
                } else if (value instanceof Integer) {
                    bundle.putInt(key, (Integer) value);
                } else if (value instanceof Long) {
                    bundle.putLong(key, (Long) value);
                } else if (value instanceof Double) {
                    bundle.putDouble(key, (Double) value);
                } else if (value instanceof Boolean) {
                    bundle.putBoolean(key, (Boolean) value);
                } else {
                    bundle.putString(key, value.toString());
                }
            }
        } catch (JSONException e) {
            Log.e(TAG, "JSON解析失败: " + json, e);
        }
        return bundle;
    }

    /**
     * 安全获取JSON字段
     */
    @Nullable
    public static String getString(@Nullable String json, @NonNull String key) {
        if (json == null || json.isEmpty()) return null;
        try {
            JSONObject obj = new JSONObject(json);
            return obj.optString(key, null);
        } catch (JSONException e) {
            return null;
        }
    }

    /**
     * 安全获取JSON int字段
     */
    public static int getInt(@Nullable String json, @NonNull String key, int defaultValue) {
        if (json == null || json.isEmpty()) return defaultValue;
        try {
            JSONObject obj = new JSONObject(json);
            return obj.optInt(key, defaultValue);
        } catch (JSONException e) {
            return defaultValue;
        }
    }

    /**
     * 安全获取JSON bool字段
     */
    public static boolean getBoolean(@Nullable String json, @NonNull String key, boolean defaultValue) {
        if (json == null || json.isEmpty()) return defaultValue;
        try {
            JSONObject obj = new JSONObject(json);
            return obj.optBoolean(key, defaultValue);
        } catch (JSONException e) {
            return defaultValue;
        }
    }
}
