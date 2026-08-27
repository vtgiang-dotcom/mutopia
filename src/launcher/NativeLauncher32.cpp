#include <windows.h>
#include <stdio.h>

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
    char exePath[MAX_PATH] = "main.exe";
    char dllPath[MAX_PATH];
    GetFullPathNameA("Main.dll", MAX_PATH, dllPath, NULL);

    char cmdLine[512];
    if (lpCmdLine && strlen(lpCmdLine) > 0)
    {
        sprintf_s(cmdLine, "\"%s\" %s", exePath, lpCmdLine);
    }
    else
    {
        sprintf_s(cmdLine, "\"%s\" connect /u127.0.0.1 /p55902", exePath);
    }

    STARTUPINFOA si = { sizeof(si) };
    PROCESS_INFORMATION pi = { 0 };

    if (!CreateProcessA(NULL, cmdLine, NULL, NULL, FALSE, CREATE_SUSPENDED, NULL, NULL, &si, &pi))
    {
        MessageBoxA(NULL, "Không th? kh?i d?ng main.exe", "L?i Launcher S16", MB_OK | MB_ICONERROR);
        return 1;
    }

    if (GetFileAttributesA(dllPath) != INVALID_FILE_ATTRIBUTES)
    {
        size_t dllLen = strlen(dllPath) + 1;
        LPVOID pMem = VirtualAllocEx(pi.hProcess, NULL, dllLen, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (pMem)
        {
            WriteProcessMemory(pi.hProcess, pMem, dllPath, dllLen, NULL);
            LPVOID pLoadLib = (LPVOID)GetProcAddress(GetModuleHandleA("kernel32.dll"), "LoadLibraryA");
            HANDLE hThread = CreateRemoteThread(pi.hProcess, NULL, 0, (LPTHREAD_START_ROUTINE)pLoadLib, pMem, 0, NULL);
            if (hThread)
            {
                WaitForSingleObject(hThread, 3000);
                CloseHandle(hThread);
            }
        }
    }

    ResumeThread(pi.hThread);
    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);

    return 0;
}
