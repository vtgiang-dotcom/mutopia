#include "stdafx.h"
#include "DiscordRPC.h"
#include <windows.h>
#include <cstdio>
#include <cstring>
#include <cwchar>
#include <ctime>

static HANDLE g_hDiscordPipe = INVALID_HANDLE_VALUE;
static char g_szClientId[64] = "1280000000000000000";
static bool g_bInitialized = false;

static bool ConnectDiscordPipe()
{
    if (g_hDiscordPipe != INVALID_HANDLE_VALUE)
    {
        return true;
    }

    wchar_t pipeName[64];
    for (int i = 0; i < 10; ++i)
    {
        swprintf_s(pipeName, L"\\\\.\\pipe\\discord-ipc-%d", i);
        g_hDiscordPipe = CreateFileW(
            pipeName,
            GENERIC_READ | GENERIC_WRITE,
            0,
            NULL,
            OPEN_EXISTING,
            0,
            NULL
        );

        if (g_hDiscordPipe != INVALID_HANDLE_VALUE)
        {
            // Send Handshake (Opcode 0)
            char handshakePayload[256];
            snprintf(handshakePayload, sizeof(handshakePayload), "{\"v\":1,\"client_id\":\"%s\"}", g_szClientId);

            DWORD len = (DWORD)strlen(handshakePayload);
            DWORD header[2] = { 0, len }; // Opcode 0 = Handshake
            DWORD bytesWritten = 0;

            WriteFile(g_hDiscordPipe, header, 8, &bytesWritten, NULL);
            WriteFile(g_hDiscordPipe, handshakePayload, len, &bytesWritten, NULL);
            return true;
        }
    }

    return false;
}

void Discord_Initialize(const char* appId)
{
    if (appId && strlen(appId) > 0)
    {
        strncpy_s(g_szClientId, sizeof(g_szClientId), appId, _TRUNCATE);
    }
    g_bInitialized = true;
    ConnectDiscordPipe();
}

void Discord_Shutdown()
{
    if (g_hDiscordPipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(g_hDiscordPipe);
        g_hDiscordPipe = INVALID_HANDLE_VALUE;
    }
    g_bInitialized = false;
}

static void SendActivityJson(const char* jsonFrame)
{
    if (!ConnectDiscordPipe())
    {
        return;
    }

    DWORD len = (DWORD)strlen(jsonFrame);
    DWORD header[2] = { 1, len }; // Opcode 1 = Frame
    DWORD bytesWritten = 0;

    if (!WriteFile(g_hDiscordPipe, header, 8, &bytesWritten, NULL) ||
        !WriteFile(g_hDiscordPipe, jsonFrame, len, &bytesWritten, NULL))
    {
        CloseHandle(g_hDiscordPipe);
        g_hDiscordPipe = INVALID_HANDLE_VALUE;
    }
}

void Discord_UpdatePresence(const wchar_t* charName, int level, const char* mapName)
{
    if (!g_bInitialized)
    {
        return;
    }

    char nameUtf8[64] = "Player";
    if (charName && charName[0] != L'\0')
    {
        WideCharToMultiByte(CP_UTF8, 0, charName, -1, nameUtf8, sizeof(nameUtf8), NULL, NULL);
    }

    const char* map = (mapName && mapName[0] != '\0') ? mapName : "Lorencia";

    char framePayload[512];
    snprintf(framePayload, sizeof(framePayload),
        "{\"cmd\":\"SET_ACTIVITY\",\"args\":{\"pid\":%lu,\"activity\":{"
        "\"state\":\"%s\","
        "\"details\":\"Level %d | %s\","
        "\"assets\":{\"large_image\":\"mu_logo\",\"large_text\":\"MU Online S6E3\"}"
        "}},\"nonce\":\"%I64d\"}",
        GetCurrentProcessId(),
        nameUtf8,
        level,
        map,
        (long long)time(NULL)
    );

    SendActivityJson(framePayload);
}

void Discord_SetIdlePresence()
{
    if (!g_bInitialized)
    {
        return;
    }

    char framePayload[512];
    snprintf(framePayload, sizeof(framePayload),
        "{\"cmd\":\"SET_ACTIVITY\",\"args\":{\"pid\":%lu,\"activity\":{"
        "\"state\":\"Login Screen\","
        "\"details\":\"MU Online S6E3\","
        "\"assets\":{\"large_image\":\"mu_logo\",\"large_text\":\"MU Online S6E3\"}"
        "}},\"nonce\":\"%I64d\"}",
        GetCurrentProcessId(),
        (long long)time(NULL)
    );

    SendActivityJson(framePayload);
}

void Discord_Update()
{
    // Non-blocking pipe drain if needed
    if (g_hDiscordPipe != INVALID_HANDLE_VALUE)
    {
        DWORD avail = 0;
        if (PeekNamedPipe(g_hDiscordPipe, NULL, 0, NULL, &avail, NULL) && avail > 0)
        {
            char buf[512];
            DWORD readBytes = 0;
            ReadFile(g_hDiscordPipe, buf, sizeof(buf) < avail ? sizeof(buf) : avail, &readBytes, NULL);
        }
    }
}
