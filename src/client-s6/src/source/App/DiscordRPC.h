#pragma once
#include <cwchar>

void Discord_Initialize(const char* appId = "1280000000000000000");
void Discord_Shutdown();
void Discord_UpdatePresence(const wchar_t* charName, int level, const char* mapName);
void Discord_SetIdlePresence();
void Discord_Update();
