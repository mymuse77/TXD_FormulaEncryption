#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <iphlpapi.h>

#pragma comment(lib, "iphlpapi.lib")
#pragma comment(lib, "ws2_32.lib")

// 获取MAC地址
bool GetMacAddress(unsigned char* mac)
{
    PIP_ADAPTER_INFO pAdapterInfo = (PIP_ADAPTER_INFO)malloc(sizeof(IP_ADAPTER_INFO));
    ULONG ulOutBufLen = sizeof(IP_ADAPTER_INFO);
    
    if (GetAdaptersInfo(pAdapterInfo, &ulOutBufLen) == ERROR_BUFFER_OVERFLOW) {
        free(pAdapterInfo);
        pAdapterInfo = (PIP_ADAPTER_INFO)malloc(ulOutBufLen);
    }
    
    if (GetAdaptersInfo(pAdapterInfo, &ulOutBufLen) == NO_ERROR) {
        PIP_ADAPTER_INFO pAdapter = pAdapterInfo;
        while (pAdapter) {
            if (pAdapter->Type == MIB_IF_TYPE_ETHERNET && pAdapter->AddressLength == 6) {
                memcpy(mac, pAdapter->Address, 6);
                free(pAdapterInfo);
                return true;
            }
            pAdapter = pAdapter->Next;
        }
    }
    
    free(pAdapterInfo);
    return false;
}

// 生成机器指纹
void GenerateMachineFingerprint(char* fingerprint, int maxLen)
{
    unsigned char mac[6] = {0};
    GetMacAddress(mac);
    
    // 基于MAC地址生成指纹
    unsigned int hash = 0;
    for (int i = 0; i < 6; i++) {
        hash = hash * 31 + mac[i];
    }
    
    // 使用sprintf而不是sprintf_s以保持兼容性
    sprintf(fingerprint, "%08X%08X", hash, hash ^ 0xAAAAAAAA);
    fingerprint[maxLen - 1] = '\0';  // 确保字符串结束
}

int main(int argc, char* argv[])
{
    char fingerprint[64];
    GenerateMachineFingerprint(fingerprint, sizeof(fingerprint));
    
    // 检查是否是命令行模式（用于脚本捕获）
    if (argc > 1) {
        // 仅输出指纹
        printf("%s", fingerprint);
        return 0;
    }
    
    // 图形界面模式
    printf("========================================\n");
    printf("  通达信DLL插件 - 机器指纹生成工具\n");
    printf("========================================\n\n");
    
    printf("您的机器指纹：\n");
    printf("\n");
    printf("  %s\n", fingerprint);
    printf("\n");
    printf("========================================\n");
    printf("请将此指纹发送给开发者以获取授权。\n");
    printf("========================================\n\n");
    
    // 自动复制到剪贴板
    if (OpenClipboard(NULL)) {
        EmptyClipboard();
        size_t len = strlen(fingerprint);
        HGLOBAL hMem = GlobalAlloc(GMEM_MOVEABLE, len + 1);
        if (hMem) {
            memcpy(GlobalLock(hMem), fingerprint, len + 1);
            GlobalUnlock(hMem);
            SetClipboardData(CF_TEXT, hMem);
        }
        CloseClipboard();
        printf("（指纹已自动复制到剪贴板，可直接粘贴）\n\n");
    }
    
    printf("按任意键退出...\n");
    system("pause > nul");
    return 0;
}
