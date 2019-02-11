#include "pch.h"
#include "main.h"
#include "NvPipe.h"
#include <filesystem>

using namespace std::experimental;

namespace NvPipe
{
#ifdef _WIN32
    filesystem::path getModulePath(HMODULE hModule)
    {
        for (int length = 10;; length *= 2)
        {
            const auto buffer = static_cast<TCHAR*>(_malloca((length + 1) * sizeof(TCHAR)));
            if (!buffer)
                throw std::runtime_error("Failed to get path of module, not enough memory");

            const auto result = GetModuleFileName(hModule, buffer, length);
            const auto error = GetLastError();

            if (result == 0)
            {
                std::stringstream ss;
                ss << "Failed to get path of module, error 0x" << std::hex << error;
                throw std::runtime_error(ss.str());
            }

            if (error == ERROR_INSUFFICIENT_BUFFER)
                continue;

            return filesystem::path(buffer, buffer + result);
        } 
    }
#endif

    Library::Library()
    {
#ifdef _WIN32
        // We expect NvPipe.dll to be found next to this module's DLL.
        auto currentModulePath = getModulePath(currentModuleHandle);
        auto nvPipeModulePath = currentModulePath.parent_path() / "NvPipe.dll";
        module_ = LoadLibrary(nvPipeModulePath.native().c_str());

        if (module_)
        {
#define GET_PROC(name) \
    this->name = reinterpret_cast<NvPipe::name*>(GetProcAddress(module_, "NvPipe_" # name)); \
    this->is_available_ &= (this->name  != nullptr)

            this->is_available_ = true;

            GET_PROC(CreateEncoder);
            GET_PROC(Destroy);
            GET_PROC(GetError);
            GET_PROC(SetBitrate);
            GET_PROC(Encode);
            GET_PROC(EncodeTextureD3D11);

#undef GET_PROC
        }
#endif
    }

    Library::~Library()
    {
#ifdef _WIN32
        if (module_)
        {
            FreeLibrary(module_);
            module_ = nullptr;
        }
#endif
    }
}

