// MoveCommandData.cpp: implementation of the CMoveCommandData class.
//////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "MoveCommandData.h"



using namespace SEASON3B;

#pragma pack(push, 1)
typedef struct
{
    int		index;
    char	szMainMapName[32];
    char	szSubMapName[32];
    int		iReqLevel;
    int		m_iReqMaxLevel;
    int		iReqZen;
    int		iGateNum;
} MOVEREQINFO_FILE;
#pragma pack(pop)

CMoveCommandData::CMoveCommandData()
{
}

CMoveCommandData::~CMoveCommandData()
{
    Release();
}

CMoveCommandData* CMoveCommandData::GetInstance()
{
    static CMoveCommandData s_Instance;
    return &s_Instance;
}

bool CMoveCommandData::Create(const std::wstring& filename)
{
    FILE* fp = _wfopen(filename.c_str(), L"rb");
    if (fp == NULL) return false;

    int count = 0;
    fread(&count, sizeof(int), 1, fp);

    for (int i = 0; i < count; i++)
    {
        auto* pMoveInfoData = new MOVEINFODATA;
        MOVEREQINFO_FILE moveReqInfo{};
        fread(&moveReqInfo, sizeof moveReqInfo, 1, fp);

        BuxConvert((BYTE*)&moveReqInfo, sizeof moveReqInfo);
        pMoveInfoData->_ReqInfo.index = moveReqInfo.index;
        pMoveInfoData->_ReqInfo.iGateNum = moveReqInfo.iGateNum;
        pMoveInfoData->_ReqInfo.iReqLevel = moveReqInfo.iReqLevel;
        pMoveInfoData->_ReqInfo.iReqZen = moveReqInfo.iReqZen;
        pMoveInfoData->_ReqInfo.m_iReqMaxLevel = moveReqInfo.m_iReqMaxLevel;
        CMultiLanguage::ConvertFromUtf8(pMoveInfoData->_ReqInfo.szMainMapName, moveReqInfo.szMainMapName, sizeof moveReqInfo.szMainMapName);
        CMultiLanguage::ConvertFromUtf8(pMoveInfoData->_ReqInfo.szSubMapName, moveReqInfo.szSubMapName, sizeof moveReqInfo.szSubMapName);

        m_listMoveInfoData.push_back(pMoveInfoData);
    }
    fclose(fp);

    // Append 16 Season 6 High-Level Farm Maps for Move Window (M Key)
    struct S6MapEntry { int index; int gateNum; int reqLevel; const wchar_t* name; };
    static const S6MapEntry s6Maps[] = {
        { 50, 418, 400, L"Acheron 1" },
        { 51, 424, 400, L"Acheron 2" },
        { 52, 428, 500, L"Deventer 1" },
        { 53, 434, 500, L"Deventer 2" },
        { 54, 438, 600, L"Urk Mountain 1" },
        { 55, 444, 600, L"Urk Mountain 2" },
        { 56, 448, 700, L"Nars" },
        { 57, 454, 800, L"Ferea" },
        { 58, 460, 900, L"Nixies Lake" },
        { 59, 466, 950, L"Deep Dungeon 1" },
        { 60, 470, 980, L"Deep Dungeon 2" },
        { 61, 474, 1000, L"Deep Dungeon 3" },
        { 62, 480, 1050, L"Swamp of Darkness" },
        { 63, 486, 1100, L"Kubera Mine" },
        { 64, 492, 1150, L"Atlans Abyss" },
        { 65, 498, 1200, L"Swamp of Doom" },
    };

    for (const auto& entry : s6Maps)
    {
        auto* pMoveInfoData = new MOVEINFODATA;
        pMoveInfoData->_ReqInfo.index = entry.index;
        pMoveInfoData->_ReqInfo.iGateNum = entry.gateNum;
        pMoveInfoData->_ReqInfo.iReqLevel = entry.reqLevel;
        pMoveInfoData->_ReqInfo.iReqZen = 50000;
        pMoveInfoData->_ReqInfo.m_iReqMaxLevel = 1600;
        wcscpy_s(pMoveInfoData->_ReqInfo.szMainMapName, entry.name);
        wcscpy_s(pMoveInfoData->_ReqInfo.szSubMapName, entry.name);
        m_listMoveInfoData.push_back(pMoveInfoData);
    }

    return true;
}

void CMoveCommandData::Release()
{
    auto li = m_listMoveInfoData.begin();
    for (; li != m_listMoveInfoData.end(); li++)
        delete (*li);
    m_listMoveInfoData.clear();
}

bool CMoveCommandData::OpenMoveReqScript(const std::wstring& filename)
{
    return CMoveCommandData::GetInstance()->Create(filename);
}

int CMoveCommandData::GetNumMoveMap()
{
    if (m_listMoveInfoData.size() > 0)
        return m_listMoveInfoData.size();

    return -1;
}

const CMoveCommandData::MOVEINFODATA* CMoveCommandData::GetMoveCommandDataByIndex(int iIndex)
{
    auto li = m_listMoveInfoData.begin();
    for (; li != m_listMoveInfoData.end(); li++)
    {
        if ((*li)->_ReqInfo.index == iIndex)
        {
            return (*li);
        }
    }
    return 0;
}

const std::list<CMoveCommandData::MOVEINFODATA*>& CMoveCommandData::GetMoveCommandDatalist()
{
    return m_listMoveInfoData;
}