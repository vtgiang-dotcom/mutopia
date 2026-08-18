#ifdef WIN32

#include <Dbghelp.h>
#include <tchar.h>

MiniDump::MiniDump()
{

}

MiniDump::~MiniDump()
{

}

void MiniDump::Start(std::string const& name)
{
	this->name = name + ".dmp";

	SetUnhandledExceptionFilter(unhandled_handler);
}

typedef BOOL (WINAPI *MINIDUMPWRITEDUMP)(HANDLE hProcess, DWORD dwPid, HANDLE hFile, MINIDUMP_TYPE DumpType,CONST PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam,CONST PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam,CONST PMINIDUMP_CALLBACK_INFORMATION CallbackParam);

void create_minidump(struct _EXCEPTION_POINTERS* apExceptionInfo)
{
    HMODULE mhLib = ::LoadLibrary(_T("dbghelp.dll"));
    MINIDUMPWRITEDUMP pDump = (MINIDUMPWRITEDUMP)::GetProcAddress(mhLib, "MiniDumpWriteDump");

    HANDLE  hFile = ::CreateFile(_T(sMiniDump->GetName().c_str()), GENERIC_WRITE, FILE_SHARE_WRITE, NULL, CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, NULL);

    _MINIDUMP_EXCEPTION_INFORMATION ExInfo;
    ExInfo.ThreadId = ::GetCurrentThreadId();
    ExInfo.ExceptionPointers = apExceptionInfo;
    ExInfo.ClientPointers = FALSE;

    pDump(GetCurrentProcess(), GetCurrentProcessId(), hFile, MiniDumpNormal, &ExInfo, NULL, NULL);
    ::CloseHandle(hFile);
}

#pragma comment(lib, "dbghelp.lib")

void print_stack_trace(struct _EXCEPTION_POINTERS* apExceptionInfo)
{
    HANDLE process = GetCurrentProcess();
    HANDLE thread = GetCurrentThread();
    SymInitialize(process, NULL, TRUE);

    CONTEXT context = *apExceptionInfo->ContextRecord;
    STACKFRAME64 stackFrame;
    memset(&stackFrame, 0, sizeof(stackFrame));

#if defined(_M_IX86)
    DWORD machineType = IMAGE_FILE_MACHINE_I386;
    stackFrame.AddrPC.Offset = context.Eip;
    stackFrame.AddrPC.Mode = AddrModeFlat;
    stackFrame.AddrFrame.Offset = context.Ebp;
    stackFrame.AddrFrame.Mode = AddrModeFlat;
    stackFrame.AddrStack.Offset = context.Esp;
    stackFrame.AddrStack.Mode = AddrModeFlat;
#elif defined(_M_X64)
    DWORD machineType = IMAGE_FILE_MACHINE_AMD64;
    stackFrame.AddrPC.Offset = context.Rip;
    stackFrame.AddrPC.Mode = AddrModeFlat;
    stackFrame.AddrFrame.Offset = context.Rsp;
    stackFrame.AddrFrame.Mode = AddrModeFlat;
    stackFrame.AddrStack.Offset = context.Rsp;
    stackFrame.AddrStack.Mode = AddrModeFlat;
#endif

    sLog->outError(LOG_DEFAULT, "=== CRASH CALL STACK ===");
    for (int frame = 0; frame < 30; ++frame)
    {
        if (!StackWalk64(machineType, process, thread, &stackFrame, &context, NULL, SymFunctionTableAccess64, SymGetModuleBase64, NULL))
            break;

        if (stackFrame.AddrPC.Offset == 0)
            break;

        char buffer[sizeof(SYMBOL_INFO) + MAX_SYM_NAME * sizeof(TCHAR)];
        PSYMBOL_INFO symbol = (PSYMBOL_INFO)buffer;
        symbol->SizeOfStruct = sizeof(SYMBOL_INFO);
        symbol->MaxNameLen = MAX_SYM_NAME;

        DWORD64 displacement = 0;
        IMAGEHLP_LINE64 line;
        line.SizeOfStruct = sizeof(IMAGEHLP_LINE64);
        DWORD lineDisplacement = 0;

        if (SymFromAddr(process, stackFrame.AddrPC.Offset, &displacement, symbol))
        {
            if (SymGetLineFromAddr64(process, stackFrame.AddrPC.Offset, &lineDisplacement, &line))
            {
                sLog->outError(LOG_DEFAULT, "  [%d] %s - %s:line %u", frame, symbol->Name, line.FileName, line.LineNumber);
            }
            else
            {
                sLog->outError(LOG_DEFAULT, "  [%d] %s + 0x%I64X (Addr: 0x%I64X)", frame, symbol->Name, displacement, stackFrame.AddrPC.Offset);
            }
        }
        else
        {
            sLog->outError(LOG_DEFAULT, "  [%d] (Addr: 0x%I64X)", frame, stackFrame.AddrPC.Offset);
        }
    }
    sLog->outError(LOG_DEFAULT, "=========================");
    SymCleanup(process);
}

LONG WINAPI unhandled_handler(struct _EXCEPTION_POINTERS* apExceptionInfo)
{
    create_minidump(apExceptionInfo);
    if (apExceptionInfo && apExceptionInfo->ExceptionRecord)
    {
        sLog->outError(LOG_DEFAULT, "CRASH DETECTED! ExceptionCode=0x%08X, Address=0x%p",
            apExceptionInfo->ExceptionRecord->ExceptionCode,
            apExceptionInfo->ExceptionRecord->ExceptionAddress);
        print_stack_trace(apExceptionInfo);
    }
    return EXCEPTION_CONTINUE_SEARCH;
}

#endif