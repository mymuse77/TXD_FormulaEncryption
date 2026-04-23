// MyTest.cpp
// 我的交易策略 - 综合技术指标插件
// 由TdxDllGenerator自动生成

#include "PluginTCalcFunc.h"
#include <windows.h>
#include <cmath>       // For sqrtf, fabsf, etc.
#include <algorithm>   // For std::max, std::min
#include <vector>      // For dynamic temp buffers sized by nDataLen

// 无安全验证 - 使用默认的安全函数
volatile long g_securityPass = 1; // 默认通过

float ObfuscateResult(float value)
{
    return value; // 无安全检查，直接返回原值
}

// 安全初始化 - 无安全功能时默认通过
bool InitializeSecurity()
{
    return true;
}

// 函数回调指针（用于调用通达信内部数据）
pPluginFUNC g_pFuncCallBack = NULL;

// 金叉信号 - 5日均线上穿10日均线
void GoldenCross(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = CLOSE[i];
    int nPeriod_3 = 5;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_3; j--)
        {
            sum += _temp_2[j];
            count++;
        }
        _temp_0[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = CLOSE[i];
    int nPeriod_5 = 10;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_5; j--)
        {
            sum += _temp_4[j];
            count++;
        }
        _temp_1[i] = count > 0 ? sum / count : 0;
    }
    pfOut[0] = 0.0f;
    for (int i = 1; i < nDataLen; i++)
        pfOut[i] = (_temp_0[i - 1] <= _temp_1[i - 1] && _temp_0[i] > _temp_1[i]) ? 1.0f : 0.0f;

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 布林带上轨
void BollingerUpper(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = CLOSE[i];
    int nPeriod_3 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_3; j--)
        {
            sum += _temp_2[j];
            count++;
        }
        _temp_0[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = CLOSE[i];
    int nPeriod_7 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0, sumSq = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_7; j--)
        {
            sum += _temp_6[j];
            sumSq += _temp_6[j] * _temp_6[j];
            count++;
        }
        if (count > 1)
        {
            float mean = sum / count;
            _temp_4[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));
        }
        else _temp_4[i] = 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = 2.0f;
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = _temp_4[i] * _temp_5[i];
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] + _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 布林带下轨
void BollingerLower(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = CLOSE[i];
    int nPeriod_3 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_3; j--)
        {
            sum += _temp_2[j];
            count++;
        }
        _temp_0[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = CLOSE[i];
    int nPeriod_7 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0, sumSq = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_7; j--)
        {
            sum += _temp_6[j];
            sumSq += _temp_6[j] * _temp_6[j];
            count++;
        }
        if (count > 1)
        {
            float mean = sum / count;
            _temp_4[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));
        }
        else _temp_4[i] = 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = 2.0f;
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = _temp_4[i] * _temp_5[i];
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] - _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 价格动量 - N日涨幅百分比
void PriceMomentum(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = CLOSE[i];
    int nRef_7 = 10;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_7;
        _temp_5[i] = (j >= 0) ? _temp_6[j] : _temp_6[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = _temp_4[i] - _temp_5[i];
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = CLOSE[i];
    int nRef_9 = 10;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_9;
        _temp_3[i] = (j >= 0) ? _temp_8[j] : _temp_8[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] / _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] * _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// RSI相对强弱指标(14日) - SMA算法
void RSI14(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);
    std::vector<float> _temp_18(nDataLen);
    std::vector<float> _temp_19(nDataLen);
    std::vector<float> _temp_20(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_11[i] = CLOSE[i];
    int nRef_12 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_12;
        _temp_10[i] = (j >= 0) ? _temp_11[j] : _temp_11[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = _temp_9[i] - _temp_10[i];
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = fmaxf(_temp_7[i], _temp_8[i]);
    int n_5 = 14;
    int m_6 = 1;
    if (nDataLen > 0)
    {
        _temp_2[0] = _temp_4[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_2[i] = (m_6 * _temp_4[i] + (n_5 - m_6) * _temp_2[i - 1]) / n_5;
    }
    for (int i = 0; i < nDataLen; i++) _temp_17[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_19[i] = CLOSE[i];
    int nRef_20 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_20;
        _temp_18[i] = (j >= 0) ? _temp_19[j] : _temp_19[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_16[i] = _temp_17[i] - _temp_18[i];
    for (int i = 0; i < nDataLen; i++) _temp_13[i] = fabsf(_temp_16[i]);
    int n_14 = 14;
    int m_15 = 1;
    if (nDataLen > 0)
    {
        _temp_3[0] = _temp_13[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_3[i] = (m_15 * _temp_13[i] + (n_14 - m_15) * _temp_3[i - 1]) / n_14;
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] / _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] * _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// MACD的DIF线 - EMA12-EMA26
void MACD_DIF(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = CLOSE[i];
    float alpha_3 = 0.1538461596f;
    if (nDataLen > 0)
    {
        _temp_0[0] = _temp_2[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_0[i] = alpha_3 * _temp_2[i] + (1 - alpha_3) * _temp_0[i - 1];
    }
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = CLOSE[i];
    float alpha_5 = 0.0740740746f;
    if (nDataLen > 0)
    {
        _temp_1[0] = _temp_4[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_1[i] = alpha_5 * _temp_4[i] + (1 - alpha_5) * _temp_1[i - 1];
    }
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] - _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// MACD的DEA线 - DIF的9日EMA
void MACD_DEA(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = CLOSE[i];
    float alpha_5 = 0.1538461596f;
    if (nDataLen > 0)
    {
        _temp_2[0] = _temp_4[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_2[i] = alpha_5 * _temp_4[i] + (1 - alpha_5) * _temp_2[i - 1];
    }
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = CLOSE[i];
    float alpha_7 = 0.0740740746f;
    if (nDataLen > 0)
    {
        _temp_3[0] = _temp_6[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_3[i] = alpha_7 * _temp_6[i] + (1 - alpha_7) * _temp_3[i - 1];
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] - _temp_3[i];
    float alpha_1 = 0.2000000030f;
    if (nDataLen > 0)
    {
        pfOut[0] = _temp_0[0];
        for (int i = 1; i < nDataLen; i++)
            pfOut[i] = alpha_1 * _temp_0[i] + (1 - alpha_1) * pfOut[i - 1];
    }

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// MACD柱状线 - (DIF-DEA)*2
void MACD_Hist(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = CLOSE[i];
    float alpha_7 = 0.1538461596f;
    if (nDataLen > 0)
    {
        _temp_4[0] = _temp_6[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_4[i] = alpha_7 * _temp_6[i] + (1 - alpha_7) * _temp_4[i - 1];
    }
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = CLOSE[i];
    float alpha_9 = 0.0740740746f;
    if (nDataLen > 0)
    {
        _temp_5[0] = _temp_8[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_5[i] = alpha_9 * _temp_8[i] + (1 - alpha_9) * _temp_5[i - 1];
    }
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = _temp_4[i] - _temp_5[i];
    for (int i = 0; i < nDataLen; i++) _temp_14[i] = CLOSE[i];
    float alpha_15 = 0.1538461596f;
    if (nDataLen > 0)
    {
        _temp_12[0] = _temp_14[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_12[i] = alpha_15 * _temp_14[i] + (1 - alpha_15) * _temp_12[i - 1];
    }
    for (int i = 0; i < nDataLen; i++) _temp_16[i] = CLOSE[i];
    float alpha_17 = 0.0740740746f;
    if (nDataLen > 0)
    {
        _temp_13[0] = _temp_16[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_13[i] = alpha_17 * _temp_16[i] + (1 - alpha_17) * _temp_13[i - 1];
    }
    for (int i = 0; i < nDataLen; i++) _temp_10[i] = _temp_12[i] - _temp_13[i];
    float alpha_11 = 0.2000000030f;
    if (nDataLen > 0)
    {
        _temp_3[0] = _temp_10[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_3[i] = alpha_11 * _temp_10[i] + (1 - alpha_11) * _temp_3[i - 1];
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] - _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 2.0f;
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] * _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// KDJ指标K值 - RSV的3日SMA
void KDJ_K(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = LOW[i];
    int nPeriod_10 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_9[i];
        for (int j = i; j >= 0 && j > i - nPeriod_10; j--)
            if (_temp_9[j] < minVal) minVal = _temp_9[j];
        _temp_8[i] = minVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = _temp_7[i] - _temp_8[i];
    for (int i = 0; i < nDataLen; i++) _temp_13[i] = HIGH[i];
    int nPeriod_14 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float maxVal = _temp_13[i];
        for (int j = i; j >= 0 && j > i - nPeriod_14; j--)
            if (_temp_13[j] > maxVal) maxVal = _temp_13[j];
        _temp_11[i] = maxVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_15[i] = LOW[i];
    int nPeriod_16 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_15[i];
        for (int j = i; j >= 0 && j > i - nPeriod_16; j--)
            if (_temp_15[j] < minVal) minVal = _temp_15[j];
        _temp_12[i] = minVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = _temp_11[i] - _temp_12[i];
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = _temp_5[i] / _temp_6[i];
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_3[i] * _temp_4[i];
    int n_1 = 3;
    int m_2 = 1;
    if (nDataLen > 0)
    {
        pfOut[0] = _temp_0[0];
        for (int i = 1; i < nDataLen; i++)
            pfOut[i] = (m_2 * _temp_0[i] + (n_1 - m_2) * pfOut[i - 1]) / n_1;
    }

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// KDJ指标D值 - K值的3日SMA
void KDJ_D(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);
    std::vector<float> _temp_18(nDataLen);
    std::vector<float> _temp_19(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_10[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_12[i] = LOW[i];
    int nPeriod_13 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_12[i];
        for (int j = i; j >= 0 && j > i - nPeriod_13; j--)
            if (_temp_12[j] < minVal) minVal = _temp_12[j];
        _temp_11[i] = minVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = _temp_10[i] - _temp_11[i];
    for (int i = 0; i < nDataLen; i++) _temp_16[i] = HIGH[i];
    int nPeriod_17 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float maxVal = _temp_16[i];
        for (int j = i; j >= 0 && j > i - nPeriod_17; j--)
            if (_temp_16[j] > maxVal) maxVal = _temp_16[j];
        _temp_14[i] = maxVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_18[i] = LOW[i];
    int nPeriod_19 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_18[i];
        for (int j = i; j >= 0 && j > i - nPeriod_19; j--)
            if (_temp_18[j] < minVal) minVal = _temp_18[j];
        _temp_15[i] = minVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = _temp_14[i] - _temp_15[i];
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = _temp_8[i] / _temp_9[i];
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = _temp_6[i] * _temp_7[i];
    int n_4 = 3;
    int m_5 = 1;
    if (nDataLen > 0)
    {
        _temp_0[0] = _temp_3[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_0[i] = (m_5 * _temp_3[i] + (n_4 - m_5) * _temp_0[i - 1]) / n_4;
    }
    int n_1 = 3;
    int m_2 = 1;
    if (nDataLen > 0)
    {
        pfOut[0] = _temp_0[0];
        for (int i = 1; i < nDataLen; i++)
            pfOut[i] = (m_2 * _temp_0[i] + (n_1 - m_2) * pfOut[i - 1]) / n_1;
    }

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// KDJ指标J值 - 3*K-2*D
void KDJ_J(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);
    std::vector<float> _temp_18(nDataLen);
    std::vector<float> _temp_19(nDataLen);
    std::vector<float> _temp_20(nDataLen);
    std::vector<float> _temp_21(nDataLen);
    std::vector<float> _temp_22(nDataLen);
    std::vector<float> _temp_23(nDataLen);
    std::vector<float> _temp_24(nDataLen);
    std::vector<float> _temp_25(nDataLen);
    std::vector<float> _temp_26(nDataLen);
    std::vector<float> _temp_27(nDataLen);
    std::vector<float> _temp_28(nDataLen);
    std::vector<float> _temp_29(nDataLen);
    std::vector<float> _temp_30(nDataLen);
    std::vector<float> _temp_31(nDataLen);
    std::vector<float> _temp_32(nDataLen);
    std::vector<float> _temp_33(nDataLen);
    std::vector<float> _temp_34(nDataLen);
    std::vector<float> _temp_35(nDataLen);
    std::vector<float> _temp_36(nDataLen);
    std::vector<float> _temp_37(nDataLen);
    std::vector<float> _temp_38(nDataLen);
    std::vector<float> _temp_39(nDataLen);
    std::vector<float> _temp_40(nDataLen);
    std::vector<float> _temp_41(nDataLen);
    std::vector<float> _temp_42(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = 3.0f;
    for (int i = 0; i < nDataLen; i++) _temp_11[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_13[i] = LOW[i];
    int nPeriod_14 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_13[i];
        for (int j = i; j >= 0 && j > i - nPeriod_14; j--)
            if (_temp_13[j] < minVal) minVal = _temp_13[j];
        _temp_12[i] = minVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = _temp_11[i] - _temp_12[i];
    for (int i = 0; i < nDataLen; i++) _temp_17[i] = HIGH[i];
    int nPeriod_18 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float maxVal = _temp_17[i];
        for (int j = i; j >= 0 && j > i - nPeriod_18; j--)
            if (_temp_17[j] > maxVal) maxVal = _temp_17[j];
        _temp_15[i] = maxVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_19[i] = LOW[i];
    int nPeriod_20 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_19[i];
        for (int j = i; j >= 0 && j > i - nPeriod_20; j--)
            if (_temp_19[j] < minVal) minVal = _temp_19[j];
        _temp_16[i] = minVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_10[i] = _temp_15[i] - _temp_16[i];
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = _temp_9[i] / _temp_10[i];
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = _temp_7[i] * _temp_8[i];
    int n_5 = 3;
    int m_6 = 1;
    if (nDataLen > 0)
    {
        _temp_3[0] = _temp_4[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_3[i] = (m_6 * _temp_4[i] + (n_5 - m_6) * _temp_3[i - 1]) / n_5;
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] * _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_21[i] = 2.0f;
    for (int i = 0; i < nDataLen; i++) _temp_33[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_35[i] = LOW[i];
    int nPeriod_36 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_35[i];
        for (int j = i; j >= 0 && j > i - nPeriod_36; j--)
            if (_temp_35[j] < minVal) minVal = _temp_35[j];
        _temp_34[i] = minVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_31[i] = _temp_33[i] - _temp_34[i];
    for (int i = 0; i < nDataLen; i++) _temp_39[i] = HIGH[i];
    int nPeriod_40 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float maxVal = _temp_39[i];
        for (int j = i; j >= 0 && j > i - nPeriod_40; j--)
            if (_temp_39[j] > maxVal) maxVal = _temp_39[j];
        _temp_37[i] = maxVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_41[i] = LOW[i];
    int nPeriod_42 = 9;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_41[i];
        for (int j = i; j >= 0 && j > i - nPeriod_42; j--)
            if (_temp_41[j] < minVal) minVal = _temp_41[j];
        _temp_38[i] = minVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_32[i] = _temp_37[i] - _temp_38[i];
    for (int i = 0; i < nDataLen; i++) _temp_29[i] = _temp_31[i] / _temp_32[i];
    for (int i = 0; i < nDataLen; i++) _temp_30[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_26[i] = _temp_29[i] * _temp_30[i];
    int n_27 = 3;
    int m_28 = 1;
    if (nDataLen > 0)
    {
        _temp_23[0] = _temp_26[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_23[i] = (m_28 * _temp_26[i] + (n_27 - m_28) * _temp_23[i - 1]) / n_27;
    }
    int n_24 = 3;
    int m_25 = 1;
    if (nDataLen > 0)
    {
        _temp_22[0] = _temp_23[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_22[i] = (m_25 * _temp_23[i] + (n_24 - m_25) * _temp_22[i - 1]) / n_24;
    }
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = _temp_21[i] * _temp_22[i];
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] - _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 真实波幅ATR(14日) - TR的14日MA
void ATR14(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = _temp_6[i] - _temp_7[i];
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_11[i] = CLOSE[i];
    int nRef_12 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_12;
        _temp_10[i] = (j >= 0) ? _temp_11[j] : _temp_11[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = _temp_9[i] - _temp_10[i];
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = fabsf(_temp_8[i]);
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = fmaxf(_temp_4[i], _temp_5[i]);
    for (int i = 0; i < nDataLen; i++) _temp_14[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_16[i] = CLOSE[i];
    int nRef_17 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_17;
        _temp_15[i] = (j >= 0) ? _temp_16[j] : _temp_16[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_13[i] = _temp_14[i] - _temp_15[i];
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = fabsf(_temp_13[i]);
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = fmaxf(_temp_2[i], _temp_3[i]);
    int nPeriod_1 = 14;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_1; j--)
        {
            sum += _temp_0[j];
            count++;
        }
        pfOut[i] = count > 0 ? sum / count : 0;
    }

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 威廉指标WR(14日)
void WilliamR(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = HIGH[i];
    int nPeriod_7 = 14;
    for (int i = 0; i < nDataLen; i++)
    {
        float maxVal = _temp_6[i];
        for (int j = i; j >= 0 && j > i - nPeriod_7; j--)
            if (_temp_6[j] > maxVal) maxVal = _temp_6[j];
        _temp_4[i] = maxVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = _temp_4[i] - _temp_5[i];
    for (int i = 0; i < nDataLen; i++) _temp_10[i] = HIGH[i];
    int nPeriod_11 = 14;
    for (int i = 0; i < nDataLen; i++)
    {
        float maxVal = _temp_10[i];
        for (int j = i; j >= 0 && j > i - nPeriod_11; j--)
            if (_temp_10[j] > maxVal) maxVal = _temp_10[j];
        _temp_8[i] = maxVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_12[i] = LOW[i];
    int nPeriod_13 = 14;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_12[i];
        for (int j = i; j >= 0 && j > i - nPeriod_13; j--)
            if (_temp_12[j] < minVal) minVal = _temp_12[j];
        _temp_9[i] = minVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = _temp_8[i] - _temp_9[i];
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] / _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] * _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// CCI商品路径指标(14日)
void CCI14(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = CLOSE[i];
    int nPeriod_5 = 14;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_5; j--)
        {
            sum += _temp_4[j];
            count++;
        }
        _temp_3[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] - _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = CLOSE[i];
    int nPeriod_9 = 14;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0, sumSq = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_9; j--)
        {
            sum += _temp_8[j];
            sumSq += _temp_8[j] * _temp_8[j];
            count++;
        }
        if (count > 1)
        {
            float mean = sum / count;
            _temp_6[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));
        }
        else _temp_6[i] = 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = 0.015f;
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = _temp_6[i] * _temp_7[i];
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] / _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// DMI指标+DI(14日) - 上升动向指标
void DMI_PDI(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);
    std::vector<float> _temp_18(nDataLen);
    std::vector<float> _temp_19(nDataLen);
    std::vector<float> _temp_20(nDataLen);
    std::vector<float> _temp_21(nDataLen);
    std::vector<float> _temp_22(nDataLen);
    std::vector<float> _temp_23(nDataLen);
    std::vector<float> _temp_24(nDataLen);
    std::vector<float> _temp_25(nDataLen);
    std::vector<float> _temp_26(nDataLen);
    std::vector<float> _temp_27(nDataLen);
    std::vector<float> _temp_28(nDataLen);
    std::vector<float> _temp_29(nDataLen);
    std::vector<float> _temp_30(nDataLen);
    std::vector<float> _temp_31(nDataLen);
    std::vector<float> _temp_32(nDataLen);
    std::vector<float> _temp_33(nDataLen);
    std::vector<float> _temp_34(nDataLen);
    std::vector<float> _temp_35(nDataLen);
    std::vector<float> _temp_36(nDataLen);
    std::vector<float> _temp_37(nDataLen);
    std::vector<float> _temp_38(nDataLen);
    std::vector<float> _temp_39(nDataLen);
    std::vector<float> _temp_40(nDataLen);
    std::vector<float> _temp_41(nDataLen);
    std::vector<float> _temp_42(nDataLen);
    std::vector<float> _temp_43(nDataLen);
    std::vector<float> _temp_44(nDataLen);
    std::vector<float> _temp_45(nDataLen);
    std::vector<float> _temp_46(nDataLen);
    std::vector<float> _temp_47(nDataLen);
    std::vector<float> _temp_48(nDataLen);
    std::vector<float> _temp_49(nDataLen);
    std::vector<float> _temp_50(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_14[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_16[i] = HIGH[i];
    int nRef_17 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_17;
        _temp_15[i] = (j >= 0) ? _temp_16[j] : _temp_16[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_12[i] = _temp_14[i] - _temp_15[i];
    for (int i = 0; i < nDataLen; i++) _temp_20[i] = LOW[i];
    int nRef_21 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_21;
        _temp_18[i] = (j >= 0) ? _temp_20[j] : _temp_20[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_19[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_13[i] = _temp_18[i] - _temp_19[i];
    for (int i = 0; i < nDataLen; i++) _temp_10[i] = _temp_12[i] > _temp_13[i];
    for (int i = 0; i < nDataLen; i++) _temp_24[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_26[i] = HIGH[i];
    int nRef_27 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_27;
        _temp_25[i] = (j >= 0) ? _temp_26[j] : _temp_26[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_22[i] = _temp_24[i] - _temp_25[i];
    for (int i = 0; i < nDataLen; i++) _temp_23[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++) _temp_11[i] = _temp_22[i] > _temp_23[i];
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = _temp_10[i] && _temp_11[i];
    for (int i = 0; i < nDataLen; i++) _temp_28[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_30[i] = HIGH[i];
    int nRef_31 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_31;
        _temp_29[i] = (j >= 0) ? _temp_30[j] : _temp_30[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = _temp_28[i] - _temp_29[i];
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++)
        _temp_4[i] = _temp_7[i] ? _temp_8[i] : _temp_9[i];
    int n_5 = 14;
    int m_6 = 1;
    if (nDataLen > 0)
    {
        _temp_2[0] = _temp_4[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_2[i] = (m_6 * _temp_4[i] + (n_5 - m_6) * _temp_2[i - 1]) / n_5;
    }
    for (int i = 0; i < nDataLen; i++) _temp_39[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_40[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_37[i] = _temp_39[i] - _temp_40[i];
    for (int i = 0; i < nDataLen; i++) _temp_42[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_44[i] = CLOSE[i];
    int nRef_45 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_45;
        _temp_43[i] = (j >= 0) ? _temp_44[j] : _temp_44[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_41[i] = _temp_42[i] - _temp_43[i];
    for (int i = 0; i < nDataLen; i++) _temp_38[i] = fabsf(_temp_41[i]);
    for (int i = 0; i < nDataLen; i++) _temp_35[i] = fmaxf(_temp_37[i], _temp_38[i]);
    for (int i = 0; i < nDataLen; i++) _temp_47[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_49[i] = CLOSE[i];
    int nRef_50 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_50;
        _temp_48[i] = (j >= 0) ? _temp_49[j] : _temp_49[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_46[i] = _temp_47[i] - _temp_48[i];
    for (int i = 0; i < nDataLen; i++) _temp_36[i] = fabsf(_temp_46[i]);
    for (int i = 0; i < nDataLen; i++) _temp_32[i] = fmaxf(_temp_35[i], _temp_36[i]);
    int n_33 = 14;
    int m_34 = 1;
    if (nDataLen > 0)
    {
        _temp_3[0] = _temp_32[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_3[i] = (m_34 * _temp_32[i] + (n_33 - m_34) * _temp_3[i - 1]) / n_33;
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] / _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] * _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// DMI指标-DI(14日) - 下降动向指标
void DMI_MDI(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);
    std::vector<float> _temp_18(nDataLen);
    std::vector<float> _temp_19(nDataLen);
    std::vector<float> _temp_20(nDataLen);
    std::vector<float> _temp_21(nDataLen);
    std::vector<float> _temp_22(nDataLen);
    std::vector<float> _temp_23(nDataLen);
    std::vector<float> _temp_24(nDataLen);
    std::vector<float> _temp_25(nDataLen);
    std::vector<float> _temp_26(nDataLen);
    std::vector<float> _temp_27(nDataLen);
    std::vector<float> _temp_28(nDataLen);
    std::vector<float> _temp_29(nDataLen);
    std::vector<float> _temp_30(nDataLen);
    std::vector<float> _temp_31(nDataLen);
    std::vector<float> _temp_32(nDataLen);
    std::vector<float> _temp_33(nDataLen);
    std::vector<float> _temp_34(nDataLen);
    std::vector<float> _temp_35(nDataLen);
    std::vector<float> _temp_36(nDataLen);
    std::vector<float> _temp_37(nDataLen);
    std::vector<float> _temp_38(nDataLen);
    std::vector<float> _temp_39(nDataLen);
    std::vector<float> _temp_40(nDataLen);
    std::vector<float> _temp_41(nDataLen);
    std::vector<float> _temp_42(nDataLen);
    std::vector<float> _temp_43(nDataLen);
    std::vector<float> _temp_44(nDataLen);
    std::vector<float> _temp_45(nDataLen);
    std::vector<float> _temp_46(nDataLen);
    std::vector<float> _temp_47(nDataLen);
    std::vector<float> _temp_48(nDataLen);
    std::vector<float> _temp_49(nDataLen);
    std::vector<float> _temp_50(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_16[i] = LOW[i];
    int nRef_17 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_17;
        _temp_14[i] = (j >= 0) ? _temp_16[j] : _temp_16[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_15[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_12[i] = _temp_14[i] - _temp_15[i];
    for (int i = 0; i < nDataLen; i++) _temp_18[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_20[i] = HIGH[i];
    int nRef_21 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_21;
        _temp_19[i] = (j >= 0) ? _temp_20[j] : _temp_20[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_13[i] = _temp_18[i] - _temp_19[i];
    for (int i = 0; i < nDataLen; i++) _temp_10[i] = _temp_12[i] > _temp_13[i];
    for (int i = 0; i < nDataLen; i++) _temp_26[i] = LOW[i];
    int nRef_27 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_27;
        _temp_24[i] = (j >= 0) ? _temp_26[j] : _temp_26[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_25[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_22[i] = _temp_24[i] - _temp_25[i];
    for (int i = 0; i < nDataLen; i++) _temp_23[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++) _temp_11[i] = _temp_22[i] > _temp_23[i];
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = _temp_10[i] && _temp_11[i];
    for (int i = 0; i < nDataLen; i++) _temp_30[i] = LOW[i];
    int nRef_31 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_31;
        _temp_28[i] = (j >= 0) ? _temp_30[j] : _temp_30[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_29[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = _temp_28[i] - _temp_29[i];
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++)
        _temp_4[i] = _temp_7[i] ? _temp_8[i] : _temp_9[i];
    int n_5 = 14;
    int m_6 = 1;
    if (nDataLen > 0)
    {
        _temp_2[0] = _temp_4[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_2[i] = (m_6 * _temp_4[i] + (n_5 - m_6) * _temp_2[i - 1]) / n_5;
    }
    for (int i = 0; i < nDataLen; i++) _temp_39[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_40[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_37[i] = _temp_39[i] - _temp_40[i];
    for (int i = 0; i < nDataLen; i++) _temp_42[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_44[i] = CLOSE[i];
    int nRef_45 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_45;
        _temp_43[i] = (j >= 0) ? _temp_44[j] : _temp_44[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_41[i] = _temp_42[i] - _temp_43[i];
    for (int i = 0; i < nDataLen; i++) _temp_38[i] = fabsf(_temp_41[i]);
    for (int i = 0; i < nDataLen; i++) _temp_35[i] = fmaxf(_temp_37[i], _temp_38[i]);
    for (int i = 0; i < nDataLen; i++) _temp_47[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_49[i] = CLOSE[i];
    int nRef_50 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_50;
        _temp_48[i] = (j >= 0) ? _temp_49[j] : _temp_49[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_46[i] = _temp_47[i] - _temp_48[i];
    for (int i = 0; i < nDataLen; i++) _temp_36[i] = fabsf(_temp_46[i]);
    for (int i = 0; i < nDataLen; i++) _temp_32[i] = fmaxf(_temp_35[i], _temp_36[i]);
    int n_33 = 14;
    int m_34 = 1;
    if (nDataLen > 0)
    {
        _temp_3[0] = _temp_32[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_3[i] = (m_34 * _temp_32[i] + (n_33 - m_34) * _temp_3[i - 1]) / n_33;
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] / _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] * _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 变动速率ROC(12日)
void ROC(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = CLOSE[i];
    int nRef_7 = 12;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_7;
        _temp_5[i] = (j >= 0) ? _temp_6[j] : _temp_6[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = _temp_4[i] - _temp_5[i];
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = CLOSE[i];
    int nRef_9 = 12;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_9;
        _temp_3[i] = (j >= 0) ? _temp_8[j] : _temp_8[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] / _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] * _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 能量潮OBV - 基于成交量累计
void OBV(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = CLOSE[i];
    int nRef_8 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_8;
        _temp_6[i] = (j >= 0) ? _temp_7[j] : _temp_7[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = _temp_5[i] > _temp_6[i];
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = VOL[i];
    for (int i = 0; i < nDataLen; i++) _temp_12[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_14[i] = CLOSE[i];
    int nRef_15 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_15;
        _temp_13[i] = (j >= 0) ? _temp_14[j] : _temp_14[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = _temp_12[i] < _temp_13[i];
    for (int i = 0; i < nDataLen; i++) _temp_16[i] = VOL[i];
    for (int i = 0; i < nDataLen; i++) _temp_10[i] = -_temp_16[i];
    for (int i = 0; i < nDataLen; i++) _temp_11[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++)
        _temp_4[i] = _temp_9[i] ? _temp_10[i] : _temp_11[i];
    for (int i = 0; i < nDataLen; i++)
        _temp_0[i] = _temp_2[i] ? _temp_3[i] : _temp_4[i];
    int nPeriod_1 = 0;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_1; j--)
            sum += _temp_0[j];
        pfOut[i] = sum;
    }

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 量比 - 当日成交量/5日均量
void VOL_Ratio(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = VOL[i];
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = VOL[i];
    int nPeriod_3 = 5;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_3; j--)
        {
            sum += _temp_2[j];
            count++;
        }
        _temp_1[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] / _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 近20日金叉次数统计
void MA_Cross_Count(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = CLOSE[i];
    int nPeriod_5 = 5;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_5; j--)
        {
            sum += _temp_4[j];
            count++;
        }
        _temp_2[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = CLOSE[i];
    int nPeriod_7 = 10;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_7; j--)
        {
            sum += _temp_6[j];
            count++;
        }
        _temp_3[i] = count > 0 ? sum / count : 0;
    }
    _temp_0[0] = 0.0f;
    for (int i = 1; i < nDataLen; i++)
        _temp_0[i] = (_temp_2[i - 1] <= _temp_3[i - 1] && _temp_2[i] > _temp_3[i]) ? 1.0f : 0.0f;
    int nPeriod_1 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        int cnt = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_1; j--)
            if (_temp_0[j] != 0.0f) cnt++;
        pfOut[i] = (float)cnt;
    }

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 突破20日新高信号
void Price_Breakout(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = HIGH[i];
    int nPeriod_8 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float maxVal = _temp_7[i];
        for (int j = i; j >= 0 && j > i - nPeriod_8; j--)
            if (_temp_7[j] > maxVal) maxVal = _temp_7[j];
        _temp_5[i] = maxVal;
    }
    int nRef_6 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_6;
        _temp_4[i] = (j >= 0) ? _temp_5[j] : _temp_5[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_3[i] > _temp_4[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 1.0f;
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++)
        pfOut[i] = _temp_0[i] ? _temp_1[i] : _temp_2[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 趋势强度 - 价格偏离20日均线百分比
void Trend_Strength(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = CLOSE[i];
    int nPeriod_7 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_7; j--)
        {
            sum += _temp_6[j];
            count++;
        }
        _temp_5[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = _temp_4[i] - _temp_5[i];
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = CLOSE[i];
    int nPeriod_9 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_9; j--)
        {
            sum += _temp_8[j];
            count++;
        }
        _temp_3[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] / _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] * _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 布林带宽度 - 上下轨距离/中轨
void Bollinger_Width(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);
    std::vector<float> _temp_18(nDataLen);
    std::vector<float> _temp_19(nDataLen);
    std::vector<float> _temp_20(nDataLen);
    std::vector<float> _temp_21(nDataLen);
    std::vector<float> _temp_22(nDataLen);
    std::vector<float> _temp_23(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = CLOSE[i];
    int nPeriod_9 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_9; j--)
        {
            sum += _temp_8[j];
            count++;
        }
        _temp_6[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_12[i] = CLOSE[i];
    int nPeriod_13 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0, sumSq = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_13; j--)
        {
            sum += _temp_12[j];
            sumSq += _temp_12[j] * _temp_12[j];
            count++;
        }
        if (count > 1)
        {
            float mean = sum / count;
            _temp_10[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));
        }
        else _temp_10[i] = 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_11[i] = 2.0f;
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = _temp_10[i] * _temp_11[i];
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = _temp_6[i] + _temp_7[i];
    for (int i = 0; i < nDataLen; i++) _temp_16[i] = CLOSE[i];
    int nPeriod_17 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_17; j--)
        {
            sum += _temp_16[j];
            count++;
        }
        _temp_14[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_20[i] = CLOSE[i];
    int nPeriod_21 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0, sumSq = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_21; j--)
        {
            sum += _temp_20[j];
            sumSq += _temp_20[j] * _temp_20[j];
            count++;
        }
        if (count > 1)
        {
            float mean = sum / count;
            _temp_18[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));
        }
        else _temp_18[i] = 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_19[i] = 2.0f;
    for (int i = 0; i < nDataLen; i++) _temp_15[i] = _temp_18[i] * _temp_19[i];
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = _temp_14[i] - _temp_15[i];
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = _temp_4[i] - _temp_5[i];
    for (int i = 0; i < nDataLen; i++) _temp_22[i] = CLOSE[i];
    int nPeriod_23 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_23; j--)
        {
            sum += _temp_22[j];
            count++;
        }
        _temp_3[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] / _temp_3[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] * _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 历史波动率 - 20日收益率标准差
void Volatility(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_8[i] = CLOSE[i];
    int nRef_9 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_9;
        _temp_7[i] = (j >= 0) ? _temp_8[j] : _temp_8[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = _temp_6[i] - _temp_7[i];
    for (int i = 0; i < nDataLen; i++) _temp_10[i] = CLOSE[i];
    int nRef_11 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_11;
        _temp_5[i] = (j >= 0) ? _temp_10[j] : _temp_10[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = _temp_4[i] / _temp_5[i];
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] * _temp_3[i];
    int nPeriod_1 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0, sumSq = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_1; j--)
        {
            sum += _temp_0[j];
            sumSq += _temp_0[j] * _temp_0[j];
            count++;
        }
        if (count > 1)
        {
            float mean = sum / count;
            pfOut[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));
        }
        else pfOut[i] = 0;
    }

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 均线交叉综合信号(5/10/20/60四线)
void MA_Cross_Signal(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);
    std::vector<float> _temp_18(nDataLen);
    std::vector<float> _temp_19(nDataLen);
    std::vector<float> _temp_20(nDataLen);
    std::vector<float> _temp_21(nDataLen);
    std::vector<float> _temp_22(nDataLen);
    std::vector<float> _temp_23(nDataLen);
    std::vector<float> _temp_24(nDataLen);
    std::vector<float> _temp_25(nDataLen);
    std::vector<float> _temp_26(nDataLen);
    std::vector<float> _temp_27(nDataLen);
    std::vector<float> _temp_28(nDataLen);
    std::vector<float> _temp_29(nDataLen);
    std::vector<float> _temp_30(nDataLen);
    std::vector<float> _temp_31(nDataLen);
    std::vector<float> _temp_32(nDataLen);
    std::vector<float> _temp_33(nDataLen);
    std::vector<float> _temp_34(nDataLen);
    std::vector<float> _temp_35(nDataLen);
    std::vector<float> _temp_36(nDataLen);
    std::vector<float> _temp_37(nDataLen);
    std::vector<float> _temp_38(nDataLen);
    std::vector<float> _temp_39(nDataLen);
    std::vector<float> _temp_40(nDataLen);
    std::vector<float> _temp_41(nDataLen);
    std::vector<float> _temp_42(nDataLen);
    std::vector<float> _temp_43(nDataLen);
    std::vector<float> _temp_44(nDataLen);
    std::vector<float> _temp_45(nDataLen);
    std::vector<float> _temp_46(nDataLen);
    std::vector<float> _temp_47(nDataLen);
    std::vector<float> _temp_48(nDataLen);
    std::vector<float> _temp_49(nDataLen);
    std::vector<float> _temp_50(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = CLOSE[i];
    int nPeriod_10 = 5;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_10; j--)
        {
            sum += _temp_9[j];
            count++;
        }
        _temp_7[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_11[i] = CLOSE[i];
    int nPeriod_12 = 10;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_12; j--)
        {
            sum += _temp_11[j];
            count++;
        }
        _temp_8[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = _temp_7[i] > _temp_8[i];
    for (int i = 0; i < nDataLen; i++) _temp_15[i] = CLOSE[i];
    int nPeriod_16 = 10;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_16; j--)
        {
            sum += _temp_15[j];
            count++;
        }
        _temp_13[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_17[i] = CLOSE[i];
    int nPeriod_18 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_18; j--)
        {
            sum += _temp_17[j];
            count++;
        }
        _temp_14[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = _temp_13[i] > _temp_14[i];
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = _temp_5[i] && _temp_6[i];
    for (int i = 0; i < nDataLen; i++) _temp_21[i] = CLOSE[i];
    int nPeriod_22 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_22; j--)
        {
            sum += _temp_21[j];
            count++;
        }
        _temp_19[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_23[i] = CLOSE[i];
    int nPeriod_24 = 60;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_24; j--)
        {
            sum += _temp_23[j];
            count++;
        }
        _temp_20[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = _temp_19[i] > _temp_20[i];
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_3[i] && _temp_4[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 1.0f;
    for (int i = 0; i < nDataLen; i++) _temp_34[i] = CLOSE[i];
    int nPeriod_35 = 5;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_35; j--)
        {
            sum += _temp_34[j];
            count++;
        }
        _temp_32[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_36[i] = CLOSE[i];
    int nPeriod_37 = 10;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_37; j--)
        {
            sum += _temp_36[j];
            count++;
        }
        _temp_33[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_30[i] = _temp_32[i] < _temp_33[i];
    for (int i = 0; i < nDataLen; i++) _temp_40[i] = CLOSE[i];
    int nPeriod_41 = 10;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_41; j--)
        {
            sum += _temp_40[j];
            count++;
        }
        _temp_38[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_42[i] = CLOSE[i];
    int nPeriod_43 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_43; j--)
        {
            sum += _temp_42[j];
            count++;
        }
        _temp_39[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_31[i] = _temp_38[i] < _temp_39[i];
    for (int i = 0; i < nDataLen; i++) _temp_28[i] = _temp_30[i] && _temp_31[i];
    for (int i = 0; i < nDataLen; i++) _temp_46[i] = CLOSE[i];
    int nPeriod_47 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_47; j--)
        {
            sum += _temp_46[j];
            count++;
        }
        _temp_44[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_48[i] = CLOSE[i];
    int nPeriod_49 = 60;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_49; j--)
        {
            sum += _temp_48[j];
            count++;
        }
        _temp_45[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_29[i] = _temp_44[i] < _temp_45[i];
    for (int i = 0; i < nDataLen; i++) _temp_25[i] = _temp_28[i] && _temp_29[i];
    for (int i = 0; i < nDataLen; i++) _temp_50[i] = 1.0f;
    for (int i = 0; i < nDataLen; i++) _temp_26[i] = -_temp_50[i];
    for (int i = 0; i < nDataLen; i++) _temp_27[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++)
        _temp_2[i] = _temp_25[i] ? _temp_26[i] : _temp_27[i];
    for (int i = 0; i < nDataLen; i++)
        pfOut[i] = _temp_0[i] ? _temp_1[i] : _temp_2[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 底部信号 - 价格创新低但RSI未创新低
void Bottom_Divergence(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);
    std::vector<float> _temp_18(nDataLen);
    std::vector<float> _temp_19(nDataLen);
    std::vector<float> _temp_20(nDataLen);
    std::vector<float> _temp_21(nDataLen);
    std::vector<float> _temp_22(nDataLen);
    std::vector<float> _temp_23(nDataLen);
    std::vector<float> _temp_24(nDataLen);
    std::vector<float> _temp_25(nDataLen);
    std::vector<float> _temp_26(nDataLen);
    std::vector<float> _temp_27(nDataLen);
    std::vector<float> _temp_28(nDataLen);
    std::vector<float> _temp_29(nDataLen);
    std::vector<float> _temp_30(nDataLen);
    std::vector<float> _temp_31(nDataLen);
    std::vector<float> _temp_32(nDataLen);
    std::vector<float> _temp_33(nDataLen);
    std::vector<float> _temp_34(nDataLen);
    std::vector<float> _temp_35(nDataLen);
    std::vector<float> _temp_36(nDataLen);
    std::vector<float> _temp_37(nDataLen);
    std::vector<float> _temp_38(nDataLen);
    std::vector<float> _temp_39(nDataLen);
    std::vector<float> _temp_40(nDataLen);
    std::vector<float> _temp_41(nDataLen);
    std::vector<float> _temp_42(nDataLen);
    std::vector<float> _temp_43(nDataLen);
    std::vector<float> _temp_44(nDataLen);
    std::vector<float> _temp_45(nDataLen);
    std::vector<float> _temp_46(nDataLen);
    std::vector<float> _temp_47(nDataLen);
    std::vector<float> _temp_48(nDataLen);
    std::vector<float> _temp_49(nDataLen);
    std::vector<float> _temp_50(nDataLen);
    std::vector<float> _temp_51(nDataLen);
    std::vector<float> _temp_52(nDataLen);
    std::vector<float> _temp_53(nDataLen);
    std::vector<float> _temp_54(nDataLen);
    std::vector<float> _temp_55(nDataLen);
    std::vector<float> _temp_56(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = CLOSE[i];
    int nPeriod_10 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_9[i];
        for (int j = i; j >= 0 && j > i - nPeriod_10; j--)
            if (_temp_9[j] < minVal) minVal = _temp_9[j];
        _temp_7[i] = minVal;
    }
    int nRef_8 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_8;
        _temp_6[i] = (j >= 0) ? _temp_7[j] : _temp_7[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = _temp_5[i] < _temp_6[i];
    for (int i = 0; i < nDataLen; i++) _temp_22[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_24[i] = CLOSE[i];
    int nRef_25 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_25;
        _temp_23[i] = (j >= 0) ? _temp_24[j] : _temp_24[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_20[i] = _temp_22[i] - _temp_23[i];
    for (int i = 0; i < nDataLen; i++) _temp_21[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++) _temp_17[i] = fmaxf(_temp_20[i], _temp_21[i]);
    int n_18 = 14;
    int m_19 = 1;
    if (nDataLen > 0)
    {
        _temp_15[0] = _temp_17[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_15[i] = (m_19 * _temp_17[i] + (n_18 - m_19) * _temp_15[i - 1]) / n_18;
    }
    for (int i = 0; i < nDataLen; i++) _temp_30[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_32[i] = CLOSE[i];
    int nRef_33 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_33;
        _temp_31[i] = (j >= 0) ? _temp_32[j] : _temp_32[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_29[i] = _temp_30[i] - _temp_31[i];
    for (int i = 0; i < nDataLen; i++) _temp_26[i] = fabsf(_temp_29[i]);
    int n_27 = 14;
    int m_28 = 1;
    if (nDataLen > 0)
    {
        _temp_16[0] = _temp_26[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_16[i] = (m_28 * _temp_26[i] + (n_27 - m_28) * _temp_16[i - 1]) / n_27;
    }
    for (int i = 0; i < nDataLen; i++) _temp_13[i] = _temp_15[i] / _temp_16[i];
    for (int i = 0; i < nDataLen; i++) _temp_14[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_11[i] = _temp_13[i] * _temp_14[i];
    for (int i = 0; i < nDataLen; i++) _temp_45[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_47[i] = CLOSE[i];
    int nRef_48 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_48;
        _temp_46[i] = (j >= 0) ? _temp_47[j] : _temp_47[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_43[i] = _temp_45[i] - _temp_46[i];
    for (int i = 0; i < nDataLen; i++) _temp_44[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++) _temp_40[i] = fmaxf(_temp_43[i], _temp_44[i]);
    int n_41 = 14;
    int m_42 = 1;
    if (nDataLen > 0)
    {
        _temp_38[0] = _temp_40[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_38[i] = (m_42 * _temp_40[i] + (n_41 - m_42) * _temp_38[i - 1]) / n_41;
    }
    for (int i = 0; i < nDataLen; i++) _temp_53[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_55[i] = CLOSE[i];
    int nRef_56 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_56;
        _temp_54[i] = (j >= 0) ? _temp_55[j] : _temp_55[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_52[i] = _temp_53[i] - _temp_54[i];
    for (int i = 0; i < nDataLen; i++) _temp_49[i] = fabsf(_temp_52[i]);
    int n_50 = 14;
    int m_51 = 1;
    if (nDataLen > 0)
    {
        _temp_39[0] = _temp_49[0];
        for (int i = 1; i < nDataLen; i++)
            _temp_39[i] = (m_51 * _temp_49[i] + (n_50 - m_51) * _temp_39[i - 1]) / n_50;
    }
    for (int i = 0; i < nDataLen; i++) _temp_36[i] = _temp_38[i] / _temp_39[i];
    for (int i = 0; i < nDataLen; i++) _temp_37[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_34[i] = _temp_36[i] * _temp_37[i];
    int nRef_35 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_35;
        _temp_12[i] = (j >= 0) ? _temp_34[j] : _temp_34[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = _temp_11[i] > _temp_12[i];
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_3[i] && _temp_4[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 1.0f;
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++)
        pfOut[i] = _temp_0[i] ? _temp_1[i] : _temp_2[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 连续上涨天数
void Consecutive_Rise(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = CLOSE[i];
    int nRef_5 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_5;
        _temp_3[i] = (j >= 0) ? _temp_4[j] : _temp_4[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_2[i] > _temp_3[i];
    int nPeriod_1 = 5;
    for (int i = 0; i < nDataLen; i++)
    {
        int cnt = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_1; j--)
            if (_temp_0[j] != 0.0f) cnt++;
        pfOut[i] = (float)cnt;
    }

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 向上跳空缺口信号
void Gap_Up(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = HIGH[i];
    int nRef_6 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_6;
        _temp_4[i] = (j >= 0) ? _temp_5[j] : _temp_5[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_3[i] > _temp_4[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 1.0f;
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++)
        pfOut[i] = _temp_0[i] ? _temp_1[i] : _temp_2[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// Money Flow Index 14
void MFI14(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);
    std::vector<float> _temp_18(nDataLen);
    std::vector<float> _temp_19(nDataLen);
    std::vector<float> _temp_20(nDataLen);
    std::vector<float> _temp_21(nDataLen);
    std::vector<float> _temp_22(nDataLen);
    std::vector<float> _temp_23(nDataLen);
    std::vector<float> _temp_24(nDataLen);
    std::vector<float> _temp_25(nDataLen);
    std::vector<float> _temp_26(nDataLen);
    std::vector<float> _temp_27(nDataLen);
    std::vector<float> _temp_28(nDataLen);
    std::vector<float> _temp_29(nDataLen);
    std::vector<float> _temp_30(nDataLen);
    std::vector<float> _temp_31(nDataLen);
    std::vector<float> _temp_32(nDataLen);
    std::vector<float> _temp_33(nDataLen);
    std::vector<float> _temp_34(nDataLen);
    std::vector<float> _temp_35(nDataLen);
    std::vector<float> _temp_36(nDataLen);
    std::vector<float> _temp_37(nDataLen);
    std::vector<float> _temp_38(nDataLen);
    std::vector<float> _temp_39(nDataLen);
    std::vector<float> _temp_40(nDataLen);
    std::vector<float> _temp_41(nDataLen);
    std::vector<float> _temp_42(nDataLen);
    std::vector<float> _temp_43(nDataLen);
    std::vector<float> _temp_44(nDataLen);
    std::vector<float> _temp_45(nDataLen);
    std::vector<float> _temp_46(nDataLen);
    std::vector<float> _temp_47(nDataLen);
    std::vector<float> _temp_48(nDataLen);
    std::vector<float> _temp_49(nDataLen);
    std::vector<float> _temp_50(nDataLen);
    std::vector<float> _temp_51(nDataLen);
    std::vector<float> _temp_52(nDataLen);
    std::vector<float> _temp_53(nDataLen);
    std::vector<float> _temp_54(nDataLen);
    std::vector<float> _temp_55(nDataLen);
    std::vector<float> _temp_56(nDataLen);
    std::vector<float> _temp_57(nDataLen);
    std::vector<float> _temp_58(nDataLen);
    std::vector<float> _temp_59(nDataLen);
    std::vector<float> _temp_60(nDataLen);
    std::vector<float> _temp_61(nDataLen);
    std::vector<float> _temp_62(nDataLen);
    std::vector<float> _temp_63(nDataLen);
    std::vector<float> _temp_64(nDataLen);
    std::vector<float> _temp_65(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_4[i] = 1.0f;
    for (int i = 0; i < nDataLen; i++) _temp_19[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_20[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_17[i] = _temp_19[i] + _temp_20[i];
    for (int i = 0; i < nDataLen; i++) _temp_18[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_15[i] = _temp_17[i] + _temp_18[i];
    for (int i = 0; i < nDataLen; i++) _temp_16[i] = 3.0f;
    for (int i = 0; i < nDataLen; i++) _temp_13[i] = _temp_15[i] / _temp_16[i];
    for (int i = 0; i < nDataLen; i++) _temp_27[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_28[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_25[i] = _temp_27[i] + _temp_28[i];
    for (int i = 0; i < nDataLen; i++) _temp_26[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_23[i] = _temp_25[i] + _temp_26[i];
    for (int i = 0; i < nDataLen; i++) _temp_24[i] = 3.0f;
    for (int i = 0; i < nDataLen; i++) _temp_21[i] = _temp_23[i] / _temp_24[i];
    int nRef_22 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_22;
        _temp_14[i] = (j >= 0) ? _temp_21[j] : _temp_21[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_10[i] = _temp_13[i] > _temp_14[i];
    for (int i = 0; i < nDataLen; i++) _temp_35[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_36[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_33[i] = _temp_35[i] + _temp_36[i];
    for (int i = 0; i < nDataLen; i++) _temp_34[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_31[i] = _temp_33[i] + _temp_34[i];
    for (int i = 0; i < nDataLen; i++) _temp_32[i] = 3.0f;
    for (int i = 0; i < nDataLen; i++) _temp_29[i] = _temp_31[i] / _temp_32[i];
    for (int i = 0; i < nDataLen; i++) _temp_30[i] = VOL[i];
    for (int i = 0; i < nDataLen; i++) _temp_11[i] = _temp_29[i] * _temp_30[i];
    for (int i = 0; i < nDataLen; i++) _temp_12[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++)
        _temp_8[i] = _temp_10[i] ? _temp_11[i] : _temp_12[i];
    int nPeriod_9 = 14;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_9; j--)
            sum += _temp_8[j];
        _temp_6[i] = sum;
    }
    for (int i = 0; i < nDataLen; i++) _temp_48[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_49[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_46[i] = _temp_48[i] + _temp_49[i];
    for (int i = 0; i < nDataLen; i++) _temp_47[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_44[i] = _temp_46[i] + _temp_47[i];
    for (int i = 0; i < nDataLen; i++) _temp_45[i] = 3.0f;
    for (int i = 0; i < nDataLen; i++) _temp_42[i] = _temp_44[i] / _temp_45[i];
    for (int i = 0; i < nDataLen; i++) _temp_56[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_57[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_54[i] = _temp_56[i] + _temp_57[i];
    for (int i = 0; i < nDataLen; i++) _temp_55[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_52[i] = _temp_54[i] + _temp_55[i];
    for (int i = 0; i < nDataLen; i++) _temp_53[i] = 3.0f;
    for (int i = 0; i < nDataLen; i++) _temp_50[i] = _temp_52[i] / _temp_53[i];
    int nRef_51 = 1;
    for (int i = 0; i < nDataLen; i++)
    {
        int j = i - nRef_51;
        _temp_43[i] = (j >= 0) ? _temp_50[j] : _temp_50[0];
    }
    for (int i = 0; i < nDataLen; i++) _temp_39[i] = _temp_42[i] < _temp_43[i];
    for (int i = 0; i < nDataLen; i++) _temp_64[i] = HIGH[i];
    for (int i = 0; i < nDataLen; i++) _temp_65[i] = LOW[i];
    for (int i = 0; i < nDataLen; i++) _temp_62[i] = _temp_64[i] + _temp_65[i];
    for (int i = 0; i < nDataLen; i++) _temp_63[i] = CLOSE[i];
    for (int i = 0; i < nDataLen; i++) _temp_60[i] = _temp_62[i] + _temp_63[i];
    for (int i = 0; i < nDataLen; i++) _temp_61[i] = 3.0f;
    for (int i = 0; i < nDataLen; i++) _temp_58[i] = _temp_60[i] / _temp_61[i];
    for (int i = 0; i < nDataLen; i++) _temp_59[i] = VOL[i];
    for (int i = 0; i < nDataLen; i++) _temp_40[i] = _temp_58[i] * _temp_59[i];
    for (int i = 0; i < nDataLen; i++) _temp_41[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++)
        _temp_37[i] = _temp_39[i] ? _temp_40[i] : _temp_41[i];
    int nPeriod_38 = 14;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_38; j--)
            sum += _temp_37[j];
        _temp_7[i] = sum;
    }
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = _temp_6[i] / _temp_7[i];
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = _temp_4[i] + _temp_5[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = _temp_2[i] / _temp_3[i];
    for (int i = 0; i < nDataLen; i++) pfOut[i] = _temp_0[i] - _temp_1[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 布林带收窄信号 - 带宽低于近20日最低值
void Bollinger_Squeeze(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    if (nDataLen <= 0 || pfOut == nullptr || pfIn1 == nullptr)
        return;

    // 数据序列映射
    float* CLOSE = pfIn1;
    float* C = pfIn1;
    float* OPEN = pfIn2;
    float* O = pfIn2;
    float* HIGH = pfIn3;
    float* H = pfIn3;
    float* LOW = pfIn3;   // 与 HIGH 共享 pfIn3 或需要额外处理
    float* L = pfIn3;
    float* VOL = pfIn2;   // 参数数组，实际数据需要额外输入
    float* V = pfIn2;

    // 临时变量声明
    std::vector<float> _temp_0(nDataLen);
    std::vector<float> _temp_1(nDataLen);
    std::vector<float> _temp_2(nDataLen);
    std::vector<float> _temp_3(nDataLen);
    std::vector<float> _temp_4(nDataLen);
    std::vector<float> _temp_5(nDataLen);
    std::vector<float> _temp_6(nDataLen);
    std::vector<float> _temp_7(nDataLen);
    std::vector<float> _temp_8(nDataLen);
    std::vector<float> _temp_9(nDataLen);
    std::vector<float> _temp_10(nDataLen);
    std::vector<float> _temp_11(nDataLen);
    std::vector<float> _temp_12(nDataLen);
    std::vector<float> _temp_13(nDataLen);
    std::vector<float> _temp_14(nDataLen);
    std::vector<float> _temp_15(nDataLen);
    std::vector<float> _temp_16(nDataLen);
    std::vector<float> _temp_17(nDataLen);
    std::vector<float> _temp_18(nDataLen);
    std::vector<float> _temp_19(nDataLen);
    std::vector<float> _temp_20(nDataLen);
    std::vector<float> _temp_21(nDataLen);
    std::vector<float> _temp_22(nDataLen);
    std::vector<float> _temp_23(nDataLen);
    std::vector<float> _temp_24(nDataLen);
    std::vector<float> _temp_25(nDataLen);
    std::vector<float> _temp_26(nDataLen);
    std::vector<float> _temp_27(nDataLen);
    std::vector<float> _temp_28(nDataLen);
    std::vector<float> _temp_29(nDataLen);
    std::vector<float> _temp_30(nDataLen);
    std::vector<float> _temp_31(nDataLen);
    std::vector<float> _temp_32(nDataLen);
    std::vector<float> _temp_33(nDataLen);
    std::vector<float> _temp_34(nDataLen);
    std::vector<float> _temp_35(nDataLen);
    std::vector<float> _temp_36(nDataLen);
    std::vector<float> _temp_37(nDataLen);
    std::vector<float> _temp_38(nDataLen);
    std::vector<float> _temp_39(nDataLen);
    std::vector<float> _temp_40(nDataLen);
    std::vector<float> _temp_41(nDataLen);
    std::vector<float> _temp_42(nDataLen);
    std::vector<float> _temp_43(nDataLen);
    std::vector<float> _temp_44(nDataLen);
    std::vector<float> _temp_45(nDataLen);
    std::vector<float> _temp_46(nDataLen);
    std::vector<float> _temp_47(nDataLen);
    std::vector<float> _temp_48(nDataLen);
    std::vector<float> _temp_49(nDataLen);
    std::vector<float> _temp_50(nDataLen);
    std::vector<float> _temp_51(nDataLen);
    std::vector<float> _temp_52(nDataLen);
    std::vector<float> _temp_53(nDataLen);
    std::vector<float> _temp_54(nDataLen);

    // 计算输出
    for (int i = 0; i < nDataLen; i++) _temp_13[i] = CLOSE[i];
    int nPeriod_14 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_14; j--)
        {
            sum += _temp_13[j];
            count++;
        }
        _temp_11[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_17[i] = CLOSE[i];
    int nPeriod_18 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0, sumSq = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_18; j--)
        {
            sum += _temp_17[j];
            sumSq += _temp_17[j] * _temp_17[j];
            count++;
        }
        if (count > 1)
        {
            float mean = sum / count;
            _temp_15[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));
        }
        else _temp_15[i] = 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_16[i] = 2.0f;
    for (int i = 0; i < nDataLen; i++) _temp_12[i] = _temp_15[i] * _temp_16[i];
    for (int i = 0; i < nDataLen; i++) _temp_9[i] = _temp_11[i] + _temp_12[i];
    for (int i = 0; i < nDataLen; i++) _temp_21[i] = CLOSE[i];
    int nPeriod_22 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_22; j--)
        {
            sum += _temp_21[j];
            count++;
        }
        _temp_19[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_25[i] = CLOSE[i];
    int nPeriod_26 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0, sumSq = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_26; j--)
        {
            sum += _temp_25[j];
            sumSq += _temp_25[j] * _temp_25[j];
            count++;
        }
        if (count > 1)
        {
            float mean = sum / count;
            _temp_23[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));
        }
        else _temp_23[i] = 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_24[i] = 2.0f;
    for (int i = 0; i < nDataLen; i++) _temp_20[i] = _temp_23[i] * _temp_24[i];
    for (int i = 0; i < nDataLen; i++) _temp_10[i] = _temp_19[i] - _temp_20[i];
    for (int i = 0; i < nDataLen; i++) _temp_7[i] = _temp_9[i] - _temp_10[i];
    for (int i = 0; i < nDataLen; i++) _temp_27[i] = CLOSE[i];
    int nPeriod_28 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_28; j--)
        {
            sum += _temp_27[j];
            count++;
        }
        _temp_8[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_5[i] = _temp_7[i] / _temp_8[i];
    for (int i = 0; i < nDataLen; i++) _temp_6[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_3[i] = _temp_5[i] * _temp_6[i];
    for (int i = 0; i < nDataLen; i++) _temp_39[i] = CLOSE[i];
    int nPeriod_40 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_40; j--)
        {
            sum += _temp_39[j];
            count++;
        }
        _temp_37[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_43[i] = CLOSE[i];
    int nPeriod_44 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0, sumSq = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_44; j--)
        {
            sum += _temp_43[j];
            sumSq += _temp_43[j] * _temp_43[j];
            count++;
        }
        if (count > 1)
        {
            float mean = sum / count;
            _temp_41[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));
        }
        else _temp_41[i] = 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_42[i] = 2.0f;
    for (int i = 0; i < nDataLen; i++) _temp_38[i] = _temp_41[i] * _temp_42[i];
    for (int i = 0; i < nDataLen; i++) _temp_35[i] = _temp_37[i] + _temp_38[i];
    for (int i = 0; i < nDataLen; i++) _temp_47[i] = CLOSE[i];
    int nPeriod_48 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_48; j--)
        {
            sum += _temp_47[j];
            count++;
        }
        _temp_45[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_51[i] = CLOSE[i];
    int nPeriod_52 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0, sumSq = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_52; j--)
        {
            sum += _temp_51[j];
            sumSq += _temp_51[j] * _temp_51[j];
            count++;
        }
        if (count > 1)
        {
            float mean = sum / count;
            _temp_49[i] = sqrtf((sumSq - count * mean * mean) / (count - 1));
        }
        else _temp_49[i] = 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_50[i] = 2.0f;
    for (int i = 0; i < nDataLen; i++) _temp_46[i] = _temp_49[i] * _temp_50[i];
    for (int i = 0; i < nDataLen; i++) _temp_36[i] = _temp_45[i] - _temp_46[i];
    for (int i = 0; i < nDataLen; i++) _temp_33[i] = _temp_35[i] - _temp_36[i];
    for (int i = 0; i < nDataLen; i++) _temp_53[i] = CLOSE[i];
    int nPeriod_54 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod_54; j--)
        {
            sum += _temp_53[j];
            count++;
        }
        _temp_34[i] = count > 0 ? sum / count : 0;
    }
    for (int i = 0; i < nDataLen; i++) _temp_31[i] = _temp_33[i] / _temp_34[i];
    for (int i = 0; i < nDataLen; i++) _temp_32[i] = 100.0f;
    for (int i = 0; i < nDataLen; i++) _temp_29[i] = _temp_31[i] * _temp_32[i];
    int nPeriod_30 = 20;
    for (int i = 0; i < nDataLen; i++)
    {
        float minVal = _temp_29[i];
        for (int j = i; j >= 0 && j > i - nPeriod_30; j--)
            if (_temp_29[j] < minVal) minVal = _temp_29[j];
        _temp_4[i] = minVal;
    }
    for (int i = 0; i < nDataLen; i++) _temp_0[i] = _temp_3[i] < _temp_4[i];
    for (int i = 0; i < nDataLen; i++) _temp_1[i] = 1.0f;
    for (int i = 0; i < nDataLen; i++) _temp_2[i] = 0.0f;
    for (int i = 0; i < nDataLen; i++)
        pfOut[i] = _temp_0[i] ? _temp_1[i] : _temp_2[i];

    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }

}

// 带参数的自定义均线(参数传入周期)
void CustomMA(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    int nPeriod = (int)pfIn2[0];
    if (nPeriod <= 0) nPeriod = 20;

    for (int i = 0; i < nDataLen; i++)
    {
        float sum = 0;
        int count = 0;
        for (int j = i; j >= 0 && j > i - nPeriod; j--)
        {
            sum += pfIn1[j];
            count++;
        }
        pfOut[i] = (count > 0) ? sum / count : 0;
    }
    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }
}

// 带参数的自定义EMA(参数传入周期)
void CustomEMA(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    int nPeriod = (int)pfIn2[0];
    if (nPeriod <= 0) nPeriod = 12;
    float alpha = 2.0f / (nPeriod + 1);

    if (nDataLen > 0)
    {
        pfOut[0] = pfIn1[0];
        for (int i = 1; i < nDataLen; i++)
        {
            pfOut[i] = alpha * pfIn1[i] + (1 - alpha) * pfOut[i - 1];
        }
    }
    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }
}

// 带参数的自定义RSI(参数传入周期)
void CustomRSI(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    int nPeriod = (int)pfIn2[0];
    if (nPeriod <= 0) nPeriod = 14;

    for (int i = nPeriod; i < nDataLen; i++)
    {
        float gain = 0, loss = 0;
        for (int j = i - nPeriod + 1; j <= i; j++)
        {
            float change = pfIn1[j] - pfIn1[j - 1];
            if (change > 0) gain += change;
            else loss -= change;
        }
        float avgGain = gain / nPeriod;
        float avgLoss = loss / nPeriod;
        if (avgLoss == 0) pfOut[i] = 100;
        else pfOut[i] = 100 - (100 / (1 + avgGain / avgLoss));
    }
    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }
}

// 标准MACD指标(DIF线)
void MACD_Std(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    // MACD算法实现 - 使用输出数组作为临时存储
    int fastPeriod = 12, slowPeriod = 26;
    float alphaFast = 2.0f / (fastPeriod + 1);
    float alphaSlow = 2.0f / (slowPeriod + 1);
    float emaFast = 0, emaSlow = 0;

    for (int i = 0; i < nDataLen; i++)
    {
        if (i == 0)
        {
            emaFast = pfIn1[i];
            emaSlow = pfIn1[i];
        }
        else
        {
            emaFast = alphaFast * pfIn1[i] + (1 - alphaFast) * emaFast;
            emaSlow = alphaSlow * pfIn1[i] + (1 - alphaSlow) * emaSlow;
        }
        pfOut[i] = emaFast - emaSlow;  // DIF线
    }
    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }
}

// 成交量加权价格 - 自定义C++算法
void VolumeWeightedPrice(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    // 成交量加权价格算法
    // pfIn1 = CLOSE, pfIn2 = VOL(作为参数传入)
    float sumPV = 0, sumV = 0;
    for (int i = 0; i < nDataLen; i++)
    {
        float vol = (i < nDataLen) ? pfIn2[i] : 1.0f;
        if (vol <= 0) vol = 1.0f;
        sumPV += pfIn1[i] * vol;
        sumV += vol;
        pfOut[i] = (sumV > 0) ? sumPV / sumV : pfIn1[i];
    }
    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }
    // 安全腐蚀: 验证失败时输出错误结果
    for (int i = 0; i < nDataLen; i++)
    {
        pfOut[i] = ObfuscateResult(pfOut[i]);
    }
}

// 低吸连板前主图信号 - 完整通达信公式
void 低吸连板前(int nDataLen, float* pfOut, float* pfIn1, float* pfIn2, float* pfIn3)
{
    // 代码生成错误: 未知的标识符: SH
    for (int i = 0; i < nDataLen; i++) pfOut[i] = 0;
}

// 函数注册表
PluginTCalcFuncInfo g_CalcFuncSets[] = {
    {1, GoldenCross},  // 金叉信号 - 5日均线上穿10日均线
    {2, BollingerUpper},  // 布林带上轨
    {3, BollingerLower},  // 布林带下轨
    {4, PriceMomentum},  // 价格动量 - N日涨幅百分比
    {5, RSI14},  // RSI相对强弱指标(14日) - SMA算法
    {6, MACD_DIF},  // MACD的DIF线 - EMA12-EMA26
    {7, MACD_DEA},  // MACD的DEA线 - DIF的9日EMA
    {8, MACD_Hist},  // MACD柱状线 - (DIF-DEA)*2
    {9, KDJ_K},  // KDJ指标K值 - RSV的3日SMA
    {10, KDJ_D},  // KDJ指标D值 - K值的3日SMA
    {11, KDJ_J},  // KDJ指标J值 - 3*K-2*D
    {12, ATR14},  // 真实波幅ATR(14日) - TR的14日MA
    {13, WilliamR},  // 威廉指标WR(14日)
    {14, CCI14},  // CCI商品路径指标(14日)
    {15, DMI_PDI},  // DMI指标+DI(14日) - 上升动向指标
    {16, DMI_MDI},  // DMI指标-DI(14日) - 下降动向指标
    {17, ROC},  // 变动速率ROC(12日)
    {18, OBV},  // 能量潮OBV - 基于成交量累计
    {19, VOL_Ratio},  // 量比 - 当日成交量/5日均量
    {20, MA_Cross_Count},  // 近20日金叉次数统计
    {21, Price_Breakout},  // 突破20日新高信号
    {22, Trend_Strength},  // 趋势强度 - 价格偏离20日均线百分比
    {23, Bollinger_Width},  // 布林带宽度 - 上下轨距离/中轨
    {24, Volatility},  // 历史波动率 - 20日收益率标准差
    {25, MA_Cross_Signal},  // 均线交叉综合信号(5/10/20/60四线)
    {26, Bottom_Divergence},  // 底部信号 - 价格创新低但RSI未创新低
    {27, Consecutive_Rise},  // 连续上涨天数
    {28, Gap_Up},  // 向上跳空缺口信号
    {29, MFI14},  // Money Flow Index 14
    {30, Bollinger_Squeeze},  // 布林带收窄信号 - 带宽低于近20日最低值
    {31, CustomMA},  // 带参数的自定义均线(参数传入周期)
    {32, CustomEMA},  // 带参数的自定义EMA(参数传入周期)
    {33, CustomRSI},  // 带参数的自定义RSI(参数传入周期)
    {34, MACD_Std},  // 标准MACD指标(DIF线)
    {35, VolumeWeightedPrice},  // 成交量加权价格 - 自定义C++算法
    {36, 低吸连板前},  // 低吸连板前主图信号 - 完整通达信公式
    {0, NULL}       // 结束标记
};

// DLL主入口函数
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        // 执行安全初始化检查（防DLL替换和调试）
        if (!InitializeSecurity())
            return FALSE; // 安全检查失败，阻止DLL加载
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

// DLL注册函数 - 通达信调用此函数获取函数表
extern "C" __declspec(dllexport) BOOL RegisterTdxFunc(PluginTCalcFuncInfo** pFun)
{
    *pFun = g_CalcFuncSets;
    return TRUE;
}
