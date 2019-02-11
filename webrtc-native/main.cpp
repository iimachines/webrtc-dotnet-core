#include "pch.h"
#include "main.h"

#if _WIN32

HMODULE currentModuleHandle;

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    if (ul_reason_for_call == DLL_PROCESS_ATTACH)
    {
        currentModuleHandle = hModule;
    }

    return true;
}
#else
#   error "This library only works on Windows for now"
#endif