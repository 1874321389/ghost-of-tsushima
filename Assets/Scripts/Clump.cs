using System;
using UnityEngine;

namespace Tutorial404
{
    [Serializable]
    public struct ClumpParameters
    {
        public float pullToCentre;         // 控制草叶向簇中心聚集的程度
        public float pointInSameDirection;         // 控制簇内草叶朝向的一致性
        // 草叶高度的基础值
        public float baseHeight;
        // 草叶高度的随机变化范围
        public float heightRandom;
        // 草叶宽度的基础值
        public float baseWidth;
        // 草叶宽度的随机变化范围
        public float widthRandom;
        // 草叶基础倾斜度（控制偏离垂直方向的程度）
        public float baseTilt;
        // 草叶倾斜度的随机变化范围
        public float tiltRandom;
        // 草叶基础弯曲度（控制整体曲线形状）
        public float baseBend;
        // 草叶弯曲度的随机变化范围
        public float bendRandom;
    }
}