// dllmain.cpp : Defines the entry point for the DLL application.
#include "framework.h"
#include "open_vector_format.pb.h"
#include "ovf_lut.pb.h"
#include "IReader.h"
#include "FileReader.h"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

