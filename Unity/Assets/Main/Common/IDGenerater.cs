using System;
using UnityEngine;

namespace Ux
{
    public static class IDGenerater
    {
        private static ushort value;
        public static long GenerateId()
        {            
            return ++value;
        }
        public static long GenerateId(int a, int b)
        {
            long low = Math.Min(a, b) & 0xFFFFFFFFL;
            long high = Math.Max(a, b) & 0xFFFFFFFFL;
            return (high << 32) | low;
        }
        
        //通过哈希扩散，有概率冲突，但很低
        public static long GenerateId(int a, int b, int c)
        {
            // 使用 ulong 内部计算
            const ulong P1 = 0x9E3779B97F4A7C15;
            const ulong P2 = 0xBF58476D1CE4E5B9;
            const ulong P3 = 0x94D049BB133111EB;
            const ulong Seed = 0x6A09E667F3BCC909;

            ulong hash = Seed;
            hash += (ulong)(uint)a * P1;
            hash ^= (ulong)(uint)b;
            hash *= P2;
            hash ^= (ulong)(uint)c;
            hash *= P3;

            // 扩散函数
            hash ^= hash >> 33;
            hash *= 0xff51afd7ed558ccd;
            hash ^= hash >> 33;
            hash *= 0xc4ceb9fe1a85ec53;
            hash ^= hash >> 31;

            // 转换为 long (保持位模式)
            return (long)hash;
        }

        public static Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}