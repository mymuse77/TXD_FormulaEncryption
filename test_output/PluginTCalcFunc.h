// PluginTCalcFunc.h
// 通达信DLL插件接口头文件
// 由TdxDllGenerator自动生成

#ifndef __PLUGIN_TCALC_FUNC
#define __PLUGIN_TCALC_FUNC

#include <windows.h>

#pragma pack(push, 1)

// 函数指针类型(数据个数, 输出, 输入a, 输入b, 输入c)
typedef void(*pPluginFUNC)(int, float*, float*, float*, float*);

typedef struct tagPluginTCalcFuncInfo
{
    unsigned short nFuncMark;  // 函数编号
    pPluginFUNC pCallFunc;     // 函数指针
} PluginTCalcFuncInfo;

// 入口函数声明
extern "C" __declspec(dllexport) BOOL RegisterTdxFunc(PluginTCalcFuncInfo** pFun);

#pragma pack(pop)

#endif // __PLUGIN_TCALC_FUNC
